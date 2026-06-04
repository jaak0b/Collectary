using System.Net;
using System.Text;

namespace Collectary.Infrastructure.Tests.Infrastructure;

/// <summary>
/// Records outgoing requests and returns canned responses matched by method + URL substring.
/// Lets the cloud REST stores (Graph, Drive) be tested with no real network.
/// </summary>
public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Rule> _rules = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    private sealed record Rule(
        HttpMethod Method,
        string PathContains,
        Func<HttpRequestMessage, HttpResponseMessage> Respond);

    public StubHttpMessageHandler OnJson(HttpMethod method, string pathContains, string json,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add(new Rule(method, pathContains, _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
        return this;
    }

    public StubHttpMessageHandler OnBytes(HttpMethod method, string pathContains, byte[] body)
    {
        _rules.Add(new Rule(method, pathContains, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(body),
        }));
        return this;
    }

    public StubHttpMessageHandler OnStatus(HttpMethod method, string pathContains, HttpStatusCode status)
    {
        _rules.Add(new Rule(method, pathContains, _ => new HttpResponseMessage(status)));
        return this;
    }

    /// <summary>Full control over the response (e.g. to set headers like Location).</summary>
    public StubHttpMessageHandler On(HttpMethod method, string pathContains, Func<HttpResponseMessage> respond)
    {
        _rules.Add(new Rule(method, pathContains, _ => respond()));
        return this;
    }

    public int CountRequests(HttpMethod method, string pathContains) =>
        Requests.Count(r => r.Method == method
            && (r.RequestUri?.ToString().Contains(pathContains, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        var url = request.RequestUri?.ToString() ?? string.Empty;
        foreach (var rule in _rules)
            if (rule.Method == request.Method
                && url.Contains(rule.PathContains, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(rule.Respond(request));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No stub rule for {request.Method} {url}"),
        });
    }
}

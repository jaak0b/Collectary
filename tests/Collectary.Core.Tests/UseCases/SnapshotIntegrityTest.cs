using Collectary.Core.UseCases;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class SnapshotIntegrityTest
{
    private readonly SnapshotIntegrity _sut = new();

    [Test]
    public void Wrap_ThenTryUnwrap_RoundTripsThePayload()
    {
        var wrapped = _sut.Wrap("{\"hello\":1}");

        var ok = _sut.TryUnwrap(wrapped, out var json);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(json, Is.EqualTo("{\"hello\":1}"));
        });
    }

    [Test]
    public void TryUnwrap_WhenBodyTamperedAfterWrapping_ReturnsFalse()
    {
        var wrapped = _sut.Wrap("{\"name\":\"Genuine\"}");
        var tampered = wrapped.Replace("Genuine", "Tampered");

        var ok = _sut.TryUnwrap(tampered, out _);

        Assert.That(ok, Is.False, "a checksum mismatch must be reported so the corrupt snapshot is skipped");
    }

    [Test]
    public void TryUnwrap_WhenHeaderHasNoNewline_ReturnsFalse()
    {
        var ok = _sut.TryUnwrap("sha256:deadbeef", out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryUnwrap_WhenContentHasNoChecksumHeader_ReturnsFalse()
    {
        var ok = _sut.TryUnwrap("{\"legacy\":true}", out _);

        Assert.That(ok, Is.False, "every snapshot we write carries the checksum header; a missing one means corrupt or foreign content and must be skipped, not silently trusted");
    }

    [Test]
    public void TryUnwrap_WhenUnprefixedContentContainsAnEarlyNewline_ReturnsFalseWithoutMisparsing()
    {
        var ok = _sut.TryUnwrap("ab\ncd", out _);

        Assert.That(ok, Is.False, "the missing-header check must reject outright, never fall through and try to parse a header out of unprefixed content");
    }
}

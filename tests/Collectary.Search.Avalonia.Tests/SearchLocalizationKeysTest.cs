using System.Reflection;
using Collectary.Search;
using Collectary.Search.Avalonia;
using Collectary.Search.Avalonia.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

[TestFixture]
public class SearchLocalizationKeysTest
{
    [Test]
    public void All_ListsEveryConstantExactlyOnce()
    {
        var constants = typeof(SearchLocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.That(constants, Is.Not.Empty);
        Assert.That(new SearchLocalizationKeys().All, Is.EquivalentTo(constants));
        Assert.That(new SearchLocalizationKeys().All, Is.Unique);
    }

    [Test]
    public void All_CoversEveryKeyTheViewModelsRequest()
    {
        var requested = new HashSet<string>();
        var provider = new RecordingLocalization(requested);
        var chip = new FilterChipViewModel("Status", ["open"], QueryOperatorKind.Contains, provider, () => { });
        _ = chip.DisplayText;
        _ = chip.OperatorHint;
        _ = chip.ValueSearchPlaceholder;
        _ = chip.ValuePlaceholder;
        _ = chip.ClearLabel;
        _ = chip.RemoveLabel;

        Assert.That(requested, Is.SubsetOf(new SearchLocalizationKeys().All),
            "every key a view-model requests must be part of the published contract");
    }

    private sealed class RecordingLocalization : ILocalizationProvider
    {
        private readonly HashSet<string> _requested;

        public RecordingLocalization(HashSet<string> requested) => _requested = requested;

        public string Get(string key)
        {
            _requested.Add(key);
            return key;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class CountryFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly CountryFieldDefinition _definition;
    private readonly CountryFieldValue _value;

    public IReadOnlyList<CountryOption> Countries { get; }

    [ObservableProperty]
    public partial CountryOption? SelectedCountry { get; set; }

    public CountryFieldEditorViewModel(
        CountryFieldDefinition definition,
        CountryFieldValue value,
        ICountryCatalog catalog)
    {
        _definition = definition;
        _value = value;
        Countries = catalog.Countries;
        SelectedCountry = catalog.Find(value.Code);
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data)
    {
        if (Countries.Count > 0)
            SelectedCountry = data.PickOne(Countries);
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Code = SelectedCountry?.Code;
        return _value;
    }
}

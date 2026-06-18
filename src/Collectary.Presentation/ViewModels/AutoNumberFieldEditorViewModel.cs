using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class AutoNumberFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly AutoNumberFieldDefinition _definition;
    private readonly AutoNumberFieldValue _value;
    private readonly ItemEditingContext _context;
    private readonly IAutoNumberService _autoNumbers;
    private HashSet<int> _used = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDuplicate))]
    [NotifyPropertyChangedFor(nameof(HasNotice))]
    [NotifyPropertyChangedFor(nameof(NoticeIsError))]
    [NotifyPropertyChangedFor(nameof(NoticeText))]
    public partial int? Number { get; set; }

    public bool IsEditable => _definition.Editable;

    public AutoNumberFieldEditorViewModel(
        AutoNumberFieldDefinition definition,
        AutoNumberFieldValue value,
        ItemEditingContext context,
        IAutoNumberService autoNumbers)
    {
        _definition = definition;
        _value = value;
        _context = context;
        _autoNumbers = autoNumbers;
        Number = value.Value;
        Ready = InitializeAsync();
    }

    public override Task Ready { get; }

    private bool IsNewItem => _context.EditingItemId is null;

    private async Task InitializeAsync()
    {
        try
        {
            _used = (await _autoNumbers.UsedNumbersAsync(_definition.Id, _context.EditingItemId)).ToHashSet();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load the used auto-numbers");
        }
        RaiseNoticeChanged();
    }

    private void RaiseNoticeChanged()
    {
        OnPropertyChanged(nameof(HasDuplicate));
        OnPropertyChanged(nameof(HasNotice));
        OnPropertyChanged(nameof(NoticeIsError));
        OnPropertyChanged(nameof(NoticeText));
    }

    public bool HasDuplicate => Number is { } n && _used.Contains(n);

    private bool EditableDuplicate => IsEditable && HasDuplicate;

    public bool HasNotice => EditableDuplicate && _definition.OnDuplicate != DuplicateHandling.Allow;

    public bool NoticeIsError => EditableDuplicate && _definition.OnDuplicate == DuplicateHandling.Error;

    public string NoticeText => LocalizationService.Instance["AutoNumber_DuplicateNotice"];

    /// <summary>Hint shown in the empty box of a new item, explaining that leaving it blank assigns the next number on save.</summary>
    public string? Watermark => IsNewItem ? LocalizationService.Instance["AutoNumber_GeneratesOnSave"] : null;

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        if (Number is null && IsNewItem)
            Number = _definition.NextNumber(_used);
        _value.Value = Number;
        return _value;
    }

    public override string? Validate()
    {
        if (EditableDuplicate && _definition.OnDuplicate == DuplicateHandling.Error)
            return LocalizationService.Instance["AutoNumber_DuplicateNotice"];
        return null;
    }
}

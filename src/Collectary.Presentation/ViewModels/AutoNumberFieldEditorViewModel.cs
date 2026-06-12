using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class AutoNumberFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly AutoNumberFieldDefinition _definition;
    private readonly AutoNumberFieldValue _value;
    private readonly ItemEditingContext _context;
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
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;
        Number = value.Value;
        Ready = InitializeAsync();
    }

    public override Task Ready { get; }

    private async Task InitializeAsync()
    {
        try
        {
            _used = (await _context.LoadUsedNumbersAsync(_definition.Id)).ToHashSet();
            if (_value.IsEmpty)
                Number = _definition.NextNumber(_used);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to compute the next auto-number");
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

    /// <summary>A read-only field that never got a number (the lookup failed) — the user can't type one, so this is an error they must be told about.</summary>
    private bool CouldNotAssign => !IsEditable && Number is null;

    private bool EditableDuplicate => IsEditable && HasDuplicate;

    public bool HasNotice => CouldNotAssign || (EditableDuplicate && _definition.OnDuplicate != DuplicateHandling.Allow);

    public bool NoticeIsError => CouldNotAssign || (EditableDuplicate && _definition.OnDuplicate == DuplicateHandling.Error);

    public string NoticeText => LocalizationService.Instance[CouldNotAssign ? "AutoNumber_CouldNotAssign" : "AutoNumber_DuplicateNotice"];

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Number;
        return _value;
    }

    public override string? Validate()
    {
        if (CouldNotAssign) return LocalizationService.Instance["AutoNumber_CouldNotAssign"];
        if (EditableDuplicate && _definition.OnDuplicate == DuplicateHandling.Error)
            return LocalizationService.Instance["AutoNumber_DuplicateNotice"];
        return null;
    }
}

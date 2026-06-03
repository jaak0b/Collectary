using System.Collections.ObjectModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public class FieldGroupLayout
{
    public ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; } = new();
    public ObservableCollection<ViewModelBase> LayoutRegions { get; } = new();
    public bool HasTabRegion { get; }

    private readonly ItemEditingContext _context;
    private readonly ILookup<Guid?, FieldGroup> _groupsByParent;
    private readonly Dictionary<Guid, FieldGroupViewModel> _groupVmById;

    public FieldGroupLayout(
        IEnumerable<FieldEditorViewModelBase> editors,
        IReadOnlyList<FieldGroup> groups,
        IReadOnlyDictionary<Guid, Guid?> groupByFieldId,
        ItemEditingContext context)
    {
        _context = context;
        _groupsByParent = groups.ToLookup(g => g.ParentGroupId);
        _groupVmById = groups.ToDictionary(g => g.Id, g => new FieldGroupViewModel(g));

        foreach (var editor in editors)
        {
            var groupId = groupByFieldId.GetValueOrDefault(editor.Definition.Id);
            if (groupId is Guid gid && _groupVmById.TryGetValue(gid, out var groupVm))
                groupVm.Editors.Add(editor);
            else
                UngroupedEditors.Add(editor);
        }

        foreach (var region in BuildRegions(null))
            LayoutRegions.Add(region);

        HasTabRegion = LayoutRegions.OfType<TabRegionViewModel>().Any();
    }

    private IEnumerable<ViewModelBase> BuildRegions(Guid? scope)
    {
        var regions = new List<ViewModelBase>();
        TabRegionViewModel? tabRegion = null;

        foreach (var group in _groupsByParent[scope].OrderBy(g => g.DisplayOrder))
        {
            var groupVm = _groupVmById[group.Id];
            foreach (var child in BuildRegions(group.Id))
                groupVm.ChildRegions.Add(child);

            if (!groupVm.HasVisibleContent) continue;

            if (group.DisplayMode == GroupDisplayMode.Card)
            {
                regions.Add(groupVm);
                continue;
            }

            if (tabRegion is null)
            {
                tabRegion = new TabRegionViewModel(_context);
                regions.Add(tabRegion);
            }
            tabRegion.TabGroups.Add(groupVm);
        }

        return regions;
    }
}

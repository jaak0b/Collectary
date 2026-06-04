using System.Collections.ObjectModel;

namespace Collectary.Presentation.ViewModels;

public class EditorNodeTreeBuilder
{
    public ObservableCollection<IEditorNode> Build(
        IReadOnlyList<FieldGroupRowViewModel> groups,
        IReadOnlyList<FieldDefinitionRowViewModel> fields)
    {
        var groupsByParent = groups.ToLookup(g => g.ParentGroupId);
        var fieldsByGroup = fields.ToLookup(f => f.AssignedGroupId);

        foreach (var group in groups)
        {
            var children = groupsByParent[group.Id].Cast<IEditorNode>()
                .Concat(fieldsByGroup[group.Id])
                .OrderBy(n => n.DisplayOrder)
                .ToList();
            group.ChildNodes.Clear();
            foreach (var child in children) group.ChildNodes.Add(child);
        }

        var roots = groupsByParent[(Guid?)null].Cast<IEditorNode>()
            .Concat(fieldsByGroup[(Guid?)null])
            .OrderBy(n => n.DisplayOrder);

        return new ObservableCollection<IEditorNode>(roots);
    }

    public FlattenedNodes Flatten(IEnumerable<IEditorNode> topLevel)
    {
        var result = new FlattenedNodes();
        Walk(topLevel, null, result);
        return result;
    }

    private static void Walk(IEnumerable<IEditorNode> nodes, Guid? scope, FlattenedNodes result)
    {
        var index = 0;
        foreach (var node in nodes)
        {
            node.DisplayOrder = index++;
            switch (node)
            {
                case FieldGroupRowViewModel group:
                    group.ParentGroupId = scope;
                    result.Groups.Add(group);
                    Walk(group.ChildNodes, group.Id, result);
                    break;
                case FieldDefinitionRowViewModel field:
                    field.AssignedGroupId = scope;
                    result.Fields.Add(field);
                    break;
            }
        }
    }
}

public class FlattenedNodes
{
    public List<FieldGroupRowViewModel> Groups { get; } = new();
    public List<FieldDefinitionRowViewModel> Fields { get; } = new();
}

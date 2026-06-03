namespace Collectary.UI.Layout;

public record FieldSlot(int FieldIndex, int ColStart, int Span);

public record FieldRow(IReadOnlyList<FieldSlot> Slots);

public static class FieldLayoutEngine
{
    public static int ComputeEffectiveCols(int desired, double availableWidth, double minColWidth) =>
        Math.Min(desired, Math.Max(1, (int)(availableWidth / minColWidth)));

    public static IReadOnlyList<FieldRow> PackRows(
        IEnumerable<(int index, int span)> fields, int effectiveCols)
    {
        var rows = new List<FieldRow>();
        var slots = new List<FieldSlot>();
        int used = 0;

        foreach (var (idx, rawSpan) in fields)
        {
            int span = Math.Clamp(rawSpan, 1, effectiveCols);

            if (used + span > effectiveCols && slots.Count > 0)
            {
                rows.Add(new FieldRow(slots.ToList()));
                slots.Clear();
                used = 0;
            }

            slots.Add(new FieldSlot(idx, used, span));
            used += span;

            if (used >= effectiveCols)
            {
                rows.Add(new FieldRow(slots.ToList()));
                slots.Clear();
                used = 0;
            }
        }

        if (slots.Count > 0)
            rows.Add(new FieldRow(slots.ToList()));

        return rows;
    }
}

#if DEBUG
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;

namespace Collectary.Presentation.Services;

public sealed class DemoDataSeeder
{
    private readonly IPresetRepository _presets;
    private readonly IItemRepository _items;

    public DemoDataSeeder(IPresetRepository presets, IItemRepository items)
    {
        _presets = presets;
        _items = items;
    }

    public async Task SeedAsync()
    {
        await SeedBooksAsync();
        await SeedBoardGamesAsync();
        AppLogger.Log.Information("Demo data seeded (Books + Board Games)");
    }

    private async Task SeedBooksAsync()
    {
        var title = new DisplayNameFieldDefinition { Label = "Title", IsRequired = true, ShowInList = true };
        var author = new TextFieldDefinition { Label = "Author", ShowInList = true };
        var genre = Choice("Genre", "Science fiction", "Fantasy", "Mystery", "Non-fiction");
        var rating = new RatingFieldDefinition { Label = "Rating", MaxStars = 5, ShowInList = true };
        var finished = new DateFieldDefinition { Label = "Finished reading" };
        var owned = new BoolFieldDefinition { Label = "On my shelf" };

        var fields = new FieldDefinition[] { title, author, genre, rating, finished, owned };
        var preset = Compose("Books", columns: 1, fields);
        await _presets.AddAsync(preset);

        var item = new Item { PresetId = preset.Id, DisplayName = "Project Hail Mary" };
        item.Values.Add(new TextFieldValue { FieldDefinitionId = author.Id, Value = "Andy Weir" });
        item.Values.Add(new SingleChoiceFieldValue { FieldDefinitionId = genre.Id, Selected = "Science fiction" });
        item.Values.Add(new RatingFieldValue { FieldDefinitionId = rating.Id, Stars = 5 });
        item.Values.Add(new DateFieldValue { FieldDefinitionId = finished.Id, Value = new DateTime(2025, 3, 14) });
        item.Values.Add(new BoolFieldValue { FieldDefinitionId = owned.Id, Value = true });
        await _items.AddAsync(item);
    }

    private async Task SeedBoardGamesAsync()
    {
        var title = new DisplayNameFieldDefinition { Label = "Title", IsRequired = true, ShowInList = true };
        var publisher = new TextFieldDefinition { Label = "Publisher", ShowInList = true };
        var designer = new TextFieldDefinition { Label = "Designer" };
        var category = MultiChoice("Categories", "Strategy", "Co-op", "Campaign", "Dungeon crawl", "Fantasy");
        var rating = new RatingFieldDefinition { Label = "Rating", MaxStars = 5, ShowInList = true };
        var price = new CurrencyFieldDefinition { Label = "Price paid", CurrencySymbol = "€", ShowInList = true };
        var acquired = new DateFieldDefinition { Label = "Acquired" };
        var condition = Choice("Condition", "Sealed", "Like new", "Good", "Well played");
        var tags = new TagsFieldDefinition { Label = "Tags" };

        var minPlayers = new IntegerFieldDefinition { Label = "Min players" };
        var maxPlayers = new IntegerFieldDefinition { Label = "Max players" };
        var playTime = new DurationFieldDefinition { Label = "Play time" };
        var complexity = new RatingFieldDefinition { Label = "Complexity", MaxStars = 5 };
        var soloMode = new BoolFieldDefinition { Label = "Solo mode" };
        var gameplay = Tab("Gameplay", columns: 2, minPlayers, maxPlayers, playTime, complexity, soloMode);

        var review = new RichTextFieldDefinition { Label = "Review" };
        var notes = Tab("Notes", columns: 1, review);

        var logDate = new DateFieldDefinition { Label = "Date" };
        var logPlayers = new IntegerFieldDefinition { Label = "Players" };
        var logWinner = new TextFieldDefinition { Label = "Winner" };
        var logComment = new TextFieldDefinition { Label = "How it went" };
        var playLog = List("Play log", columns: 3, logDate, logPlayers, logWinner, logComment);

        var fields = new FieldDefinition[]
        {
            title, publisher, designer, category, rating, price, acquired, condition, tags,
            playLog, minPlayers, maxPlayers, playTime, complexity, soloMode, review,
        };
        var preset = Compose("Board games", columns: 2, fields, gameplay, notes);
        await _presets.AddAsync(preset);

        var item = new Item { PresetId = preset.Id, DisplayName = "Gloomhaven" };
        item.Values.Add(new TextFieldValue { FieldDefinitionId = publisher.Id, Value = "Cephalofair Games" });
        item.Values.Add(new TextFieldValue { FieldDefinitionId = designer.Id, Value = "Isaac Childres" });
        item.Values.Add(new MultiChoiceFieldValue
        {
            FieldDefinitionId = category.Id,
            Selected = new List<string> { "Strategy", "Co-op", "Campaign", "Dungeon crawl" },
        });
        item.Values.Add(new RatingFieldValue { FieldDefinitionId = rating.Id, Stars = 5 });
        item.Values.Add(new CurrencyFieldValue { FieldDefinitionId = price.Id, Value = 129.99m });
        item.Values.Add(new DateFieldValue { FieldDefinitionId = acquired.Id, Value = new DateTime(2024, 11, 20) });
        item.Values.Add(new SingleChoiceFieldValue { FieldDefinitionId = condition.Id, Selected = "Like new" });
        item.Values.Add(new TagsFieldValue
        {
            FieldDefinitionId = tags.Id,
            Tags = new List<string> { "heavy", "legacy", "group night" },
        });
        item.Values.Add(new IntegerFieldValue { FieldDefinitionId = minPlayers.Id, Value = 1 });
        item.Values.Add(new IntegerFieldValue { FieldDefinitionId = maxPlayers.Id, Value = 4 });
        item.Values.Add(new DurationFieldValue { FieldDefinitionId = playTime.Id, TotalMinutes = 120 });
        item.Values.Add(new RatingFieldValue { FieldDefinitionId = complexity.Id, Stars = 4 });
        item.Values.Add(new BoolFieldValue { FieldDefinitionId = soloMode.Id, Value = true });
        item.Values.Add(new RichTextFieldValue
        {
            FieldDefinitionId = review.Id,
            Value = "Easily the centrepiece of our shelf. The legacy campaign kept four of us hooked for "
                + "months — branching scenarios, unlockable classes, and that satisfying sticker-the-map "
                + "payoff at the end of every session. Setup is heavy, but it earns it.",
        });
        item.Values.Add(new ListFieldValue
        {
            FieldDefinitionId = playLog.Id,
            Entries =
            {
                LogEntry(0, logDate, new DateTime(2025, 1, 12), logPlayers, 4, logWinner, "Maja", logComment, "Scenario 14 — barely survived the last room"),
                LogEntry(1, logDate, new DateTime(2025, 2, 3), logPlayers, 3, logWinner, "Tom", logComment, "First run with the new class, great fun"),
                LogEntry(2, logDate, new DateTime(2025, 2, 20), logPlayers, 2, logWinner, "Solo", logComment, "Quick solo run, lost on the final boss"),
            },
        });
        await _items.AddAsync(item);
    }

    private ListEntry LogEntry(
        int order,
        DateFieldDefinition dateField, DateTime date,
        IntegerFieldDefinition playersField, int players,
        TextFieldDefinition winnerField, string winner,
        TextFieldDefinition commentField, string comment)
    {
        var entry = new ListEntry { DisplayOrder = order };
        entry.SubValues.Add(new DateFieldValue { FieldDefinitionId = dateField.Id, Value = date });
        entry.SubValues.Add(new IntegerFieldValue { FieldDefinitionId = playersField.Id, Value = players });
        entry.SubValues.Add(new TextFieldValue { FieldDefinitionId = winnerField.Id, Value = winner });
        entry.SubValues.Add(new TextFieldValue { FieldDefinitionId = commentField.Id, Value = comment });
        return entry;
    }

    private SingleChoiceFieldDefinition Choice(string label, params string[] options)
    {
        var def = new SingleChoiceFieldDefinition { Label = label };
        for (var i = 0; i < options.Length; i++)
            def.Choices.Add(new ChoiceOption { Value = options[i], DisplayOrder = i });
        return def;
    }

    private MultiChoiceFieldDefinition MultiChoice(string label, params string[] options)
    {
        var def = new MultiChoiceFieldDefinition { Label = label };
        for (var i = 0; i < options.Length; i++)
            def.Choices.Add(new ChoiceOption { Value = options[i], DisplayOrder = i });
        return def;
    }

    private ListFieldDefinition List(string label, int columns, params FieldDefinition[] subFields)
    {
        var def = new ListFieldDefinition { Label = label, ColumnCount = columns, InlineStyle = ListInlineStyle.Card };
        for (var i = 0; i < subFields.Length; i++)
        {
            subFields[i].DisplayOrder = i;
            subFields[i].ParentListFieldDefinitionId = def.Id;
            def.SubFields.Add(subFields[i]);
        }
        return def;
    }

    private FieldGroup Tab(string name, int columns, params FieldDefinition[] fields)
    {
        var group = new FieldGroup { Name = name, ColumnCount = columns, DisplayMode = GroupDisplayMode.Tab };
        foreach (var field in fields)
            field.GroupId = group.Id;
        return group;
    }

    private Preset Compose(string name, int columns, IReadOnlyList<FieldDefinition> fields, params FieldGroup[] groups)
    {
        var ordered = fields.ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].DisplayOrder = i;

        var preset = new Preset { Name = name, ColumnCount = columns, Fields = ordered };
        for (var i = 0; i < groups.Length; i++)
        {
            groups[i].PresetId = preset.Id;
            groups[i].DisplayOrder = i;
        }
        preset.Groups = groups.ToList();
        return preset;
    }
}
#endif

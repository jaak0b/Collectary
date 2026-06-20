using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TagsFieldEditorViewModelTest
{
    [Test]
    public void LoadsExistingTags()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue { Tags = { "a", "b" } });
        Assert.That(sut.Tags, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void AddTag_TrimsAndAdds()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue()) { NewTag = "  hello  " };
        sut.AddTagCommand.Execute(null);
        Assert.That(sut.Tags, Is.EqualTo(new[] { "hello" }));
        Assert.That(sut.NewTag, Is.Null);
    }

    [Test]
    public void AddTag_IgnoresBlank()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue()) { NewTag = "   " };
        sut.AddTagCommand.Execute(null);
        Assert.That(sut.Tags, Is.Empty);
    }

    [Test]
    public void AddTag_DeduplicatesCaseInsensitively()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue { Tags = { "Sci-Fi" } })
        {
            NewTag = "sci-fi"
        };
        sut.AddTagCommand.Execute(null);
        Assert.That(sut.Tags, Has.Count.EqualTo(1));
    }

    [Test]
    public void RemoveTag_Removes()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue { Tags = { "a", "b" } });
        sut.RemoveTagCommand.Execute("a");
        Assert.That(sut.Tags, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void RemoveLastTag_RemovesMostRecent()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue { Tags = { "a", "b", "c" } });
        sut.RemoveLastTagCommand.Execute(null);
        Assert.That(sut.Tags, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void RemoveLastTag_NoOpWhenEmpty()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue());
        Assert.DoesNotThrow(() => sut.RemoveLastTagCommand.Execute(null));
        Assert.That(sut.Tags, Is.Empty);
    }

    [Test]
    public void PersistsCurrentTags()
    {
        var sut = new TagsFieldEditorViewModel(new TagsFieldDefinition(), new TagsFieldValue { Tags = { "a" } })
        {
            NewTag = "b"
        };
        sut.AddTagCommand.Execute(null);
        Assert.That(((TagsFieldValue)sut.GetCurrentValue()).Tags, Is.EqualTo(new[] { "a", "b" }));
    }
}

using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TagsFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoTags()
    {
        Assert.That(new TagsFieldValue().IsEmpty, Is.True);
        Assert.That(new TagsFieldValue { Tags = { "x" } }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_JoinsTags()
    {
        Assert.That(new TagsFieldValue { Tags = { "a", "b", "c" } }.ToString(), Is.EqualTo("a, b, c"));
    }

    [Test]
    public void ToString_TruncatesPast80Chars()
    {
        var many = new TagsFieldValue { Tags = Enumerable.Range(0, 50).Select(i => $"tag{i}").ToList() };
        var s = many.ToString();
        Assert.That(s, Does.EndWith("…"));
        Assert.That(s, Has.Length.EqualTo(81));
    }

    [Test]
    public void CopyFrom_ClonesTagsIndependently()
    {
        var source = new TagsFieldValue { Tags = { "a", "b" } };
        var target = new TagsFieldValue();
        target.CopyFrom(source);

        Assert.That(target.Tags, Is.EqualTo(new[] { "a", "b" }));
        source.Tags.Add("c");
        Assert.That(target.Tags, Has.Count.EqualTo(2));
    }

    [Test]
    public void ToString_DoesNotTruncate_WhenExactly80Chars()
    {
        var tag = new string('x', 80);
        var sut = new TagsFieldValue { Tags = [tag] };

        var result = sut.ToString();

        Assert.That(result, Is.EqualTo(tag));
        Assert.That(result, Does.Not.EndWith("…"));
    }

    [Test]
    public void ToString_Truncates_WhenExactly81Chars()
    {
        var tag = new string('x', 81);
        var sut = new TagsFieldValue { Tags = [tag] };

        var result = sut.ToString();

        Assert.That(result, Does.EndWith("…"));
        Assert.That(result.Length, Is.EqualTo(81));
    }
}

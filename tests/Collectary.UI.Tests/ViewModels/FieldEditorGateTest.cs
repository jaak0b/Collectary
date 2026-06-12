using FakeItEasy;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldEditorGateTest
{
    [Test]
    public async Task AwaitReadyAndValidate_WaitsForEveryEditorReadyBeforeValidating()
    {
        var tcs = new TaskCompletionSource();
        var editor = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => editor.Ready).Returns(tcs.Task);
        A.CallTo(() => editor.Validate()).Returns("too late");

        var task = new FieldEditorGate().AwaitReadyAndValidateAsync(new[] { editor });

        Assert.That(task.IsCompleted, Is.False, "must not validate until the editor is ready");
        tcs.SetResult();
        Assert.That(await task, Is.EqualTo("too late"));
    }

    [Test]
    public async Task AwaitReadyAndValidate_ReturnsFirstNonEmptyError()
    {
        var ok = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => ok.Validate()).Returns("   ");
        var bad = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => bad.Validate()).Returns("bad");

        var error = await new FieldEditorGate().AwaitReadyAndValidateAsync(new[] { ok, bad });

        Assert.That(error, Is.EqualTo("bad"));
    }

    [Test]
    public async Task AwaitReadyAndValidate_AllValid_ReturnsNull()
    {
        var editor = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => editor.Validate()).Returns(null);

        Assert.That(await new FieldEditorGate().AwaitReadyAndValidateAsync(new[] { editor }), Is.Null);
    }
}

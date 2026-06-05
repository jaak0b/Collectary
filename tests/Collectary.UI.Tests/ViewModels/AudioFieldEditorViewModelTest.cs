using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AudioFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        Func<Task<(string, int)?>>? record = null,
        Func<string, Task>? play = null,
        Func<string, Task>? delete = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (record is not null) ctx.RecordAudioAsync = record;
        if (play is not null) ctx.PlayAudioAsync = play;
        if (delete is not null) ctx.DeleteFileAsync = delete;
        return ctx;
    }

    [Test]
    public void LoadsExistingAudio()
    {
        var sut = new AudioFieldEditorViewModel(new AudioFieldDefinition(),
            new AudioFieldValue { AudioKey = "k", DurationSeconds = 9 }, MakeContext());
        Assert.That(sut.HasAudio, Is.True);
        Assert.That(sut.DurationSeconds, Is.EqualTo(9));
    }

    [Test]
    public async Task Record_StoresKeyAndDuration()
    {
        var sut = new AudioFieldEditorViewModel(new AudioFieldDefinition(), new AudioFieldValue(),
            MakeContext(record: () => Task.FromResult<(string, int)?>(("audio-7", 14))));

        await sut.RecordCommand.ExecuteAsync(null);

        Assert.That(sut.AudioKey, Is.EqualTo("audio-7"));
        var v = (AudioFieldValue)sut.GetCurrentValue();
        Assert.That(v.AudioKey, Is.EqualTo("audio-7"));
        Assert.That(v.DurationSeconds, Is.EqualTo(14));
    }

    [Test]
    public async Task Record_WhenCancelled_LeavesAudioUnset()
    {
        var sut = new AudioFieldEditorViewModel(new AudioFieldDefinition(), new AudioFieldValue(),
            MakeContext(record: () => Task.FromResult<(string, int)?>(null)));

        await sut.RecordCommand.ExecuteAsync(null);

        Assert.That(sut.HasAudio, Is.False);
    }

    [Test]
    public async Task Play_InvokesContextWithKey()
    {
        string? played = null;
        var sut = new AudioFieldEditorViewModel(new AudioFieldDefinition(),
            new AudioFieldValue { AudioKey = "k" }, MakeContext(play: k => { played = k; return Task.CompletedTask; }));

        await sut.PlayCommand.ExecuteAsync(null);

        Assert.That(played, Is.EqualTo("k"));
    }

    [Test]
    public async Task Delete_ClearsAudioAndDeletesBlob()
    {
        var deleted = new List<string>();
        var sut = new AudioFieldEditorViewModel(new AudioFieldDefinition(),
            new AudioFieldValue { AudioKey = "k", DurationSeconds = 3 },
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.DeleteCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.EqualTo(new[] { "k" }));
        Assert.That(sut.HasAudio, Is.False);
        Assert.That(((AudioFieldValue)sut.GetCurrentValue()).AudioKey, Is.Null);
    }
}

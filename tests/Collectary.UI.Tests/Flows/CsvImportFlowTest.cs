using System.Text;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.UseCases.Import;
using Collectary.Infrastructure.Import;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.Import;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class CsvImportFlowTest : FlowTestBase
{
    [Test]
    public async Task CsvFile_ImportedIntoNewCollection()
    {
        var csv = "Title,Pages\nDune,412\nHobbit,310";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var data = new CsvWorkbookReader().Read(stream);

        var vm = new ExcelImportViewModel(
            data,
            new GridShaper(),
            new CultureDetector(),
            new FieldTypeInference(),
            new SpreadsheetImportService(ItemUseCase, PresetUseCase),
            PresetUseCase,
            A.Fake<IDialogService>(),
            Array.Empty<Preset>(),
            onFinished: null,
            onClose: () => { });
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Books";

        while (vm.Step != ImportStep.Map)
            await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null); // Map -> import

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Result));
        Assert.That(vm.Summary!.Imported, Is.EqualTo(2));
        var books = (await PresetUseCase.GetAllPresetsAsync()).Single(p => p.Name == "Books");
        var items = await ItemRepo.GetByPresetAsync(books.Id);
        Assert.That(items.Select(i => i.DisplayName), Is.EquivalentTo(new[] { "Dune", "Hobbit" }));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculators;

namespace MyFireNumber.ViewModels;

public partial class CalculatorDetailViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;

    public CalculatorDetailViewModel(ICalculatorCatalog catalog)
    {
        this.catalog = catalog;
    }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    public void Load(string calculatorId)
    {
        var definition = catalog.GetRequired(calculatorId);
        Title = definition.Title;
        Summary = definition.Summary;
    }
}
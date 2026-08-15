using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

/// <summary>
/// Hosts the shared FIRE Number form for the Standard, Lean, and Fat FIRE variants.
/// The variant is chosen from the navigation query so the three routes share one
/// page and one inflated visual tree.
/// </summary>
public partial class FireNumberPage : CalculatorPageBase
{
    private readonly IServiceProvider services;

    public FireNumberPage(IServiceProvider services, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        this.services = services;
        InitializeComponent();
    }

    protected override ICalculatorViewModel SelectViewModel(IDictionary<string, object> query)
    {
        var calculatorId = query.TryGetValue("calculatorId", out var value) && value is string id
            ? Uri.UnescapeDataString(id)
            : "standard-fire";

        return calculatorId switch
        {
            "lean-fire" => services.GetRequiredService<LeanFireViewModel>(),
            "fat-fire" => services.GetRequiredService<FatFireViewModel>(),
            _ => services.GetRequiredService<StandardFireViewModel>()
        };
    }
}

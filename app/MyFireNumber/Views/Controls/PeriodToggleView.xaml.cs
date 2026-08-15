namespace MyFireNumber.Views.Controls;

/// <summary>
/// Switches whether a calculator's recurring dollar amounts are shown per month or per year.
/// </summary>
/// <remarks>
/// Display only. The calculator keeps using the same canonical amount whichever way it is shown, which
/// is what separates this from the contribution frequency toggle on the Savings &amp; Investment page —
/// that one selects between two different formulas. Both appear on that page together.
/// </remarks>
public partial class PeriodToggleView : ContentView
{
    public PeriodToggleView()
    {
        InitializeComponent();
    }
}

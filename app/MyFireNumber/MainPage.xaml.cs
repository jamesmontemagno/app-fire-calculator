using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace MyFireNumber;

public partial class MainPage : ContentPage
{
	public IReadOnlyList<ISeries> Series { get; } =
	[
		new LineSeries<double>
		{
			Values = [120, 185, 275, 405, 590, 825, 1_100, 1_420]
		}
	];

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}

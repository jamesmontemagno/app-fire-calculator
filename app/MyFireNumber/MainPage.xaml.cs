using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MyFireNumber.Data;
using MyFireNumber.Exports;

namespace MyFireNumber;

public partial class MainPage : ContentPage
{
	private string exportProofStatus = "Spreadsheet share ready.";
	private string storageProofStatus = "SQLite check in progress.";

	public IReadOnlyList<ISeries> Series { get; } =
	[
		new LineSeries<double>
		{
			Name = "Portfolio balance",
			Values = [120, 185, 275, 405, 590, 825, 1_100, 1_420],
			YToolTipLabelFormatter = point =>
				(point.Coordinate.PrimaryValue * 1_000d).ToString("C0", CultureInfo.CurrentCulture)
		}
	];

	public string StorageProofStatus
	{
		get => storageProofStatus;
		private set
		{
			if (storageProofStatus == value)
			{
				return;
			}

			storageProofStatus = value;
			OnPropertyChanged();
		}
	}

	public string ExportProofStatus
	{
		get => exportProofStatus;
		private set
		{
			if (exportProofStatus == value)
			{
				return;
			}

			exportProofStatus = value;
			OnPropertyChanged();
		}
	}

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;
		Loaded += OnLoaded;
	}

	private async void OnLoaded(object? sender, EventArgs eventArgs)
	{
		Loaded -= OnLoaded;

		try
		{
			StorageProofStatus = await SqliteStorageProof.VerifyAsync();
		}
		catch (Exception exception)
		{
			StorageProofStatus = $"SQLite check failed: {exception.GetType().Name}.";
		}
	}

	private async void OnShareSpreadsheetProofClicked(object? sender, EventArgs eventArgs)
	{
		try
		{
			var filePath = SpreadsheetExportProof.Create();
			ExportProofStatus = "Spreadsheet proof created in app cache.";
			await Share.Default.RequestAsync(new ShareFileRequest
			{
				Title = "Share spreadsheet proof",
				File = new ShareFile(filePath)
			});
		}
		catch (Exception exception)
		{
			ExportProofStatus = $"Spreadsheet proof failed: {exception.GetType().Name}.";
		}
	}
}

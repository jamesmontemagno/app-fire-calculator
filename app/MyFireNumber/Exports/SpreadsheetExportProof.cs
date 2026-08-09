using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyFireNumber.Exports;

public static class SpreadsheetExportProof
{
	private const string FileName = "my-fire-number-proof.xlsx";

	public static string Create()
	{
		var filePath = Path.Combine(FileSystem.CacheDirectory, FileName);
		if (File.Exists(filePath))
		{
			File.Delete(filePath);
		}

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(
			new SheetData(
				new Row(
					new DocumentFormat.OpenXml.Spreadsheet.Cell
					{
						DataType = CellValues.InlineString,
						InlineString = new InlineString(new Text("My Fire Number export proof"))
					})));

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		sheets.Append(new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1U,
			Name = "Proof"
		});
		workbookPart.Workbook.Save();

		return filePath;
	}
}
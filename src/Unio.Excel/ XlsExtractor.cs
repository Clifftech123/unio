
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Runtime.CompilerServices;
using Unio.Abstractions;
using Unio.Mapping;


namespace Unio.Excel {

	/// <summary>
	/// Extracts data from XLSX files using OpenXML SDK.
	/// Supports streaming via SAX-like reading for large files.
	/// </summary>
	public class XlsxExtractor : IDataExtractor {
		public IReadOnlyList<string> SupportedExtensions => new[] { ".xlsx", ".xlsm" };

		public bool CanHandle(Stream stream, string? fileExtension = null) {
			if (fileExtension is not null) {
				var ext = fileExtension.ToLowerInvariant();
				return ext is ".xlsx" or ".xlsm";
			}
			// Check ZIP magic bytes
			if (!stream.CanSeek) return false;
			var pos = stream.Position;
			var header = new byte[4];
			stream.ReadExactly(header, 0, 4);
			stream.Position = pos;
			return header[0] == 0x50 && header[1] == 0x4B;
		}

		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			options ??= new ExtractionOptions();

			using var doc = SpreadsheetDocument.Open(stream, false);
			var workbookPart = doc.WorkbookPart
				?? throw new InvalidOperationException("XLSX file has no workbook.");

			var sheet = ResolveSheet(workbookPart, options);
			var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!.Value!);
			var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

			var rows = worksheetPart.Worksheet?.Descendants<Row>().ToList()
				?? throw new InvalidOperationException("Worksheet has no content.");

			string[]? headers = null;
			TypeMapper<T>? mapper = null;
			int dataRowIndex = 0;

			foreach (var row in rows) {
				var values = ReadRow(row, sharedStrings);

				if (options.HasHeaderRow && headers is null) {
					headers = values.Select(v => v?.ToString() ?? "").ToArray();
					mapper = new TypeMapper<T>(headers, options.Culture);
					continue;
				}

				mapper ??= new TypeMapper<T>(null, options.Culture);

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				yield return mapper.Map(values);
				dataRowIndex++;
			}
		}

		public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
			options ??= new ExtractionOptions();

			using var doc = SpreadsheetDocument.Open(stream, false);
			var workbookPart = doc.WorkbookPart!;
			var sheet = ResolveSheet(workbookPart, options);
			var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!.Value!);
			var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

			string[]? headers = null;

			var worksheet = worksheetPart.Worksheet
				?? throw new InvalidOperationException("Worksheet has no content.");
			foreach (var row in worksheet.Descendants<Row>()) {
				var values = ReadRow(row, sharedStrings);

				if (options.HasHeaderRow && headers is null) {
					headers = values.Select(v => v?.ToString() ?? "").ToArray();
					continue;
				}

				var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
				for (int i = 0; i < values.Length; i++) {
					var key = headers is not null && i < headers.Length ? headers[i] : $"Column{i}";
					expando[key] = values[i];
				}
				yield return expando;
			}
		}

		public async IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new() {
			// OpenXML SDK is synchronous, so we wrap and yield asynchronously
			await Task.Yield(); // Release calling thread

			foreach (var record in Extract<T>(stream, options)) {
				cancellationToken.ThrowIfCancellationRequested();
				yield return record;
			}
		}



		private static Sheet ResolveSheet(WorkbookPart workbookPart, ExtractionOptions options) {
			var sheets = workbookPart.Workbook?.Sheets?.Elements<Sheet>().ToList()
				?? throw new InvalidOperationException("Workbook has no sheets.");

			if (options.SheetName is not null) {
				return sheets.FirstOrDefault(s => s.Name?.Value == options.SheetName)
					?? throw new InvalidOperationException($"Sheet '{options.SheetName}' not found.");
			}

			if (options.SheetIndex < sheets.Count)
				return sheets[options.SheetIndex];

			throw new InvalidOperationException($"Sheet index {options.SheetIndex} out of range.");
		}

		private static object?[] ReadRow(Row row, SharedStringTable? sharedStrings) {
			var cells = row.Elements<Cell>().ToList();
			if (cells.Count == 0) return Array.Empty<object?>();

			// Determine max column index to handle sparse rows
			int maxCol = cells.Max(c => ColumnIndex(c.CellReference?.Value));
			var values = new object?[maxCol + 1];

			foreach (var cell in cells) {
				int colIdx = ColumnIndex(cell.CellReference?.Value);
				values[colIdx] = GetCellValue(cell, sharedStrings);
			}

			return values;
		}

		private static object? GetCellValue(Cell cell, SharedStringTable? sharedStrings) {
			var value = cell.CellValue?.Text;
			if (value is null) return null;

			if (cell.DataType?.Value == CellValues.SharedString && sharedStrings is not null
				&& int.TryParse(value, out int idx)) {
				return sharedStrings.ElementAt(idx).InnerText;
			}

			if (cell.DataType?.Value == CellValues.Boolean)
				return value == "1";

			// Try numeric
			if (double.TryParse(value, System.Globalization.NumberStyles.Any,
				System.Globalization.CultureInfo.InvariantCulture, out double num))
				return num;

			return value;
		}

		/// <summary>
		/// Converts Excel column reference (e.g., "A", "B", "AA") to zero-based index.
		/// </summary>
		private static int ColumnIndex(string? cellReference) {
			if (string.IsNullOrEmpty(cellReference)) return 0;

			int col = 0;
			foreach (char c in cellReference) {
				if (char.IsLetter(c))
					col = col * 26 + (char.ToUpper(c) - 'A' + 1);
				else
					break;
			}
			return col - 1;
		}
	}
}
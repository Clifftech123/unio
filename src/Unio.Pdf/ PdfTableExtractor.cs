using System.Runtime.CompilerServices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Unio.Abstractions;
using Unio.Mapping;

namespace Unio.Pdf {

	/// <summary>
	/// Extracts tabular data from PDF files using PdfPig.
	/// Groups words into rows by Y-coordinate, then detects column boundaries
	/// from spacing gaps to reconstruct a table grid.
	/// </summary>
	public class PdfTableExtractor : IDataExtractor {
		private const double RowTolerance = 3.0;
		private const double ColumnGapMultiplier = 1.5;

		public IReadOnlyList<string> SupportedExtensions => new[] { ".pdf" };

		public bool CanHandle(Stream stream, string? fileExtension = null) {
			if (fileExtension is not null) {
				return fileExtension.ToLowerInvariant() is ".pdf";
			}

			// Check %PDF magic bytes
			if (!stream.CanSeek) return false;
			var pos = stream.Position;
			var header = new byte[4];
			stream.ReadExactly(header, 0, 4);
			stream.Position = pos;
			return header[0] == 0x25 && header[1] == 0x50
				&& header[2] == 0x44 && header[3] == 0x46;
		}

		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			options ??= new ExtractionOptions();

			var tableRows = ExtractTableRows(stream);

			string[]? headers = null;
			TypeMapper<T>? mapper = null;
			int dataRowIndex = 0;

			foreach (var row in tableRows) {
				if (options.HasHeaderRow && headers is null) {
					headers = row;
					mapper = new TypeMapper<T>(headers, options.Culture);
					continue;
				}

				mapper ??= new TypeMapper<T>(null, options.Culture);

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				var values = row.Cast<object?>().ToArray();
				yield return mapper!.Map(values);
				dataRowIndex++;
			}
		}

		public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
			options ??= new ExtractionOptions();

			var tableRows = ExtractTableRows(stream);

			string[]? headers = null;
			int dataRowIndex = 0;

			foreach (var row in tableRows) {
				if (options.HasHeaderRow && headers is null) {
					headers = row;
					continue;
				}

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
				for (int i = 0; i < row.Length; i++) {
					var key = headers is not null && i < headers.Length ? headers[i] : $"Column{i}";
					expando[key] = row[i];
				}

				yield return expando;
				dataRowIndex++;
			}
		}

		public async IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new() {
			await Task.Yield();

			foreach (var record in Extract<T>(stream, options)) {
				cancellationToken.ThrowIfCancellationRequested();
				yield return record;
			}
		}

		/// <summary>
		/// Reads all pages, clusters words into rows by Y-coordinate,
		/// detects column boundaries, and returns a list of string arrays.
		/// </summary>
		private static List<string[]> ExtractTableRows(Stream stream) {
			using var document = PdfDocument.Open(stream);
			var allWords = new List<Word>();

			foreach (var page in document.GetPages()) {
				allWords.AddRange(page.GetWords());
			}

			if (allWords.Count == 0)
				return [];

			// Group words into rows by Y-coordinate (top of bounding box)
			var rows = ClusterWordsIntoRows(allWords);

			if (rows.Count == 0)
				return [];

			// Detect column boundaries from consistent X gaps
			var columnBoundaries = DetectColumnBoundaries(rows);

			// Map each row's words into column cells
			var result = new List<string[]>();
			foreach (var row in rows) {
				var cells = MapWordsToColumns(row, columnBoundaries);
				result.Add(cells);
			}

			return result;
		}

		/// <summary>
		/// Clusters words into rows based on their vertical position.
		/// Words within <see cref="RowTolerance"/> points of each other are on the same row.
		/// </summary>
		private static List<List<Word>> ClusterWordsIntoRows(List<Word> words) {
			// Sort by Y descending (top of page first), then X ascending
			var sorted = words
				.OrderByDescending(w => w.BoundingBox.Bottom)
				.ThenBy(w => w.BoundingBox.Left)
				.ToList();

			var rows = new List<List<Word>>();
			var currentRow = new List<Word> { sorted[0] };
			double currentY = sorted[0].BoundingBox.Bottom;

			for (int i = 1; i < sorted.Count; i++) {
				var word = sorted[i];
				if (Math.Abs(word.BoundingBox.Bottom - currentY) <= RowTolerance) {
					currentRow.Add(word);
				} else {
					// Sort current row left-to-right before adding
					currentRow.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
					rows.Add(currentRow);
					currentRow = [word];
					currentY = word.BoundingBox.Bottom;
				}
			}

			currentRow.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
			rows.Add(currentRow);

			return rows;
		}

		/// <summary>
		/// Detects column boundaries by analyzing the X positions of words across all rows.
		/// A new column starts where there is a gap larger than the average word spacing.
		/// </summary>
		private static List<double> DetectColumnBoundaries(List<List<Word>> rows) {
			// Collect all left-edge X positions across all rows
			var allLeftEdges = rows
				.SelectMany(row => row.Select(w => w.BoundingBox.Left))
				.OrderBy(x => x)
				.ToList();

			if (allLeftEdges.Count <= 1)
				return [allLeftEdges.FirstOrDefault()];

			// Find average gap between consecutive words within rows
			double totalGap = 0;
			int gapCount = 0;
			foreach (var row in rows) {
				for (int i = 1; i < row.Count; i++) {
					var gap = row[i].BoundingBox.Left - row[i - 1].BoundingBox.Right;
					if (gap > 0) {
						totalGap += gap;
						gapCount++;
					}
				}
			}

			double avgGap = gapCount > 0 ? totalGap / gapCount : 10.0;
			double columnThreshold = avgGap * ColumnGapMultiplier;

			// Cluster the left-edge positions to find distinct column starts
			var boundaries = new List<double> { allLeftEdges[0] };
			for (int i = 1; i < allLeftEdges.Count; i++) {
				if (allLeftEdges[i] - allLeftEdges[i - 1] > columnThreshold) {
					boundaries.Add(allLeftEdges[i]);
				}
			}

			return boundaries;
		}

		/// <summary>
		/// Assigns each word in a row to the nearest column boundary
		/// and concatenates words in the same column.
		/// </summary>
		private static string[] MapWordsToColumns(List<Word> row, List<double> columnBoundaries) {
			var cells = new string[columnBoundaries.Count];
			Array.Fill(cells, "");

			foreach (var word in row) {
				int colIndex = FindNearestColumn(word.BoundingBox.Left, columnBoundaries);
				cells[colIndex] = cells[colIndex].Length > 0
					? cells[colIndex] + " " + word.Text
					: word.Text;
			}

			return cells;
		}

		private static int FindNearestColumn(double x, List<double> boundaries) {
			int best = 0;
			double bestDist = Math.Abs(x - boundaries[0]);

			for (int i = 1; i < boundaries.Count; i++) {
				double dist = Math.Abs(x - boundaries[i]);
				if (dist < bestDist) {
					bestDist = dist;
					best = i;
				}
			}

			return best;
		}
	}
}
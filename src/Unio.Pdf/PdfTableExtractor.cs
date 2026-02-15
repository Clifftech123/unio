using System.Runtime.CompilerServices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Unio.Abstractions;
using Unio.Mapping;

namespace Unio.Pdf {

	/// <summary>
	/// Extracts data from PDF files using PdfPig.
	///
	/// Handles two content types automatically:
	///   1. Tabular PDFs — detects column boundaries from spacing, reconstructs a table grid.
	///   2. Plain text PDFs — extracts each line as a single-column "Text" row.
	///
	/// The extractor auto-detects which mode to use based on how consistent the
	/// column structure is across rows. No configuration needed.
	/// </summary>
	public class PdfTableExtractor : IDataExtractor {
		private const double RowTolerance = 3.0;
		private const double MinTableColumnConsistency = 0.5;
		private const int MinColumnsForTable = 2;

		public IReadOnlyList<string> SupportedExtensions => [".pdf"];

		public bool CanHandle(Stream stream, string? fileExtension = null) {
			if (fileExtension is not null) {
				return fileExtension.ToLowerInvariant() is ".pdf";
			}

			if (!stream.CanSeek) return false;
			var pos = stream.Position;
			var header = new byte[4];
			var bytesRead = stream.Read(header, 0, 4);
			stream.Position = pos;
			return bytesRead == 4
				&& header[0] == 0x25 && header[1] == 0x50
				&& header[2] == 0x44 && header[3] == 0x46;
		}

		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			options ??= new ExtractionOptions();

			var tableRows = ExtractRows(stream);

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

				T mapped;
				try {
					mapped = mapper.Map(values);
				}
				catch (Exception) when (options.OnError != ErrorHandling.ThrowOnFirst) {
					// Skip rows that fail to map (e.g. PDF title fragments,
					// currency symbols, or mixed-table content).
					dataRowIndex++;
					continue;
				}

				yield return mapped;
				dataRowIndex++;
			}
		}

		public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
			options ??= new ExtractionOptions();

			var tableRows = ExtractRows(stream);

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
		/// Reads all pages, clusters words into rows, then decides:
		///   - If consistent column structure ? tabular extraction
		///   - Otherwise ? plain text lines (single "Text" column per row)
		/// </summary>
		private static List<string[]> ExtractRows(Stream stream) {
			using var document = PdfDocument.Open(stream);
			var allWords = new List<Word>();

			foreach (var page in document.GetPages()) {
				allWords.AddRange(page.GetWords());
			}

			if (allWords.Count == 0)
				return [];

			var rows = ClusterWordsIntoRows(allWords);
			if (rows.Count == 0)
				return [];

			var columnBoundaries = DetectColumnBoundaries(rows);

			// Decide: is this actually a table or plain text?
			if (IsTabular(rows, columnBoundaries)) {
				// Tabular mode: map words into column cells
				var result = new List<string[]>();
				foreach (var row in rows) {
					result.Add(MapWordsToColumns(row, columnBoundaries));
				}

				// Remove rows that don't populate enough columns
				// (titles, headings, section breaks, rows from other tables).
				int minPopulated = Math.Max(MinColumnsForTable, columnBoundaries.Count / 2);
				result = result
					.Where(row => row.Count(c => !string.IsNullOrWhiteSpace(c)) >= minPopulated)
					.ToList();

				return result;
			} else {
				// Plain text mode: join each row's words into a single text line
				return ExtractAsTextLines(rows);
			}
		}

		/// <summary>
		/// Determines if the content has a consistent table structure.
		/// A table needs at least 2 detected columns, and a majority of rows
		/// must have words that align to multiple columns.
		/// </summary>
		private static bool IsTabular(List<List<Word>> rows, List<double> columnBoundaries) {
			if (columnBoundaries.Count < MinColumnsForTable)
				return false;

			// Check what fraction of rows use multiple columns
			int multiColumnRows = 0;
			foreach (var row in rows) {
				var usedColumns = new HashSet<int>();
				foreach (var word in row) {
					usedColumns.Add(FindNearestColumn(word.BoundingBox.Left, columnBoundaries));
				}
				if (usedColumns.Count >= MinColumnsForTable)
					multiColumnRows++;
			}

			double ratio = (double)multiColumnRows / rows.Count;
			return ratio >= MinTableColumnConsistency;
		}

		/// <summary>
		/// Plain text mode: joins each row's words into a single string.
		/// Returns rows with a single "Text" column.
		/// </summary>
		private static List<string[]> ExtractAsTextLines(List<List<Word>> rows) {
			var result = new List<string[]>();

			foreach (var row in rows) {
				var lineText = string.Join(" ", row.Select(w => w.Text));
				if (!string.IsNullOrWhiteSpace(lineText)) {
					result.Add([lineText]);
				}
			}

			return result;
		}

		/// <summary>
		/// Clusters words into rows based on vertical position.
		/// Words within RowTolerance points of each other are on the same row.
		/// </summary>
		private static List<List<Word>> ClusterWordsIntoRows(List<Word> words) {
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
		/// Detects column boundaries by merging nearby words into cells first,
		/// then clustering cell left-edge positions across rows.
		/// This avoids splitting multi-word values (e.g. "Kwame Asante") into
		/// separate columns.
		/// </summary>
		private static List<double> DetectColumnBoundaries(List<List<Word>> rows) {
			// Filter out sparse rows (titles/headings) that skew column detection.
			var wordCounts = rows.Select(r => r.Count).OrderBy(c => c).ToList();
			int medianWordCount = wordCounts[wordCounts.Count / 2];
			int minWords = Math.Max(MinColumnsForTable, medianWordCount / 2);
			var tableRows = rows.Where(r => r.Count >= minWords).ToList();
			if (tableRows.Count == 0) tableRows = rows;

			// Compute average character width to determine cell merge threshold
			double avgCharWidth = ComputeAvgCharWidth(tableRows);
			double cellMergeThreshold = avgCharWidth * 2;

			// Merge words into cells within each row, then collect cell left edges
			var allCellLeftEdges = new List<double>();
			foreach (var row in tableRows) {
				var cells = MergeWordsIntoCells(row, cellMergeThreshold);
				allCellLeftEdges.AddRange(cells.Select(c => c[0].BoundingBox.Left));
			}

			allCellLeftEdges.Sort();

			if (allCellLeftEdges.Count <= 1)
				return allCellLeftEdges.Count == 1 ? [allCellLeftEdges[0]] : [0];

			// Cluster nearby cell-left-edges to find column positions.
			// Cells in the same column across rows start at similar X positions.
			double clusterTolerance = avgCharWidth * 3;
			var clusters = new List<List<double>> { new() { allCellLeftEdges[0] } };

			for (int i = 1; i < allCellLeftEdges.Count; i++) {
				if (allCellLeftEdges[i] - clusters[^1][^1] <= clusterTolerance) {
					clusters[^1].Add(allCellLeftEdges[i]);
				} else {
					clusters.Add(new() { allCellLeftEdges[i] });
				}
			}

			// Each significant cluster is a column boundary.
			// Filter out small clusters (noise from titles/outlier rows).
			int minClusterSize = Math.Max(1, tableRows.Count / 3);
			return clusters
				.Where(c => c.Count >= minClusterSize)
				.Select(c => c[0])
				.ToList();
		}

		/// <summary>
		/// Merges consecutive words in a row that are close together into cells.
		/// Normal word spacing within a cell is small; column gaps are larger.
		/// </summary>
		private static List<List<Word>> MergeWordsIntoCells(List<Word> row, double threshold) {
			if (row.Count == 0) return [];

			var cells = new List<List<Word>> { new() { row[0] } };
			for (int i = 1; i < row.Count; i++) {
				var gap = row[i].BoundingBox.Left - row[i - 1].BoundingBox.Right;
				if (gap <= threshold) {
					cells[^1].Add(row[i]);
				} else {
					cells.Add(new() { row[i] });
				}
			}
			return cells;
		}

		/// <summary>
		/// Computes average character width across all words for spatial thresholds.
		/// </summary>
		private static double ComputeAvgCharWidth(List<List<Word>> rows) {
			double totalWidth = 0;
			int totalChars = 0;
			foreach (var row in rows) {
				foreach (var word in row) {
					totalWidth += word.BoundingBox.Width;
					totalChars += word.Text.Length;
				}
			}
			return totalChars > 0 ? totalWidth / totalChars : 5.0;
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

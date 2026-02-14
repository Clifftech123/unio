using Unio.Abstractions;
using Unio.Csv;
using Unio.Detection;

namespace Unio {

	/// <summary>
	/// Main entry point for the Unio library.
	/// Provides static methods for quick extraction without DI setup.
	///
	/// Usage:
	///   var records = Unio.Extract<Invoice>("invoices.csv");
	///   await foreach (var r in Unio.ExtractAsync<Invoice>("huge.xlsx")) { }
	/// </summary>
	public static class Unio {
		private static readonly List<IDataExtractor> _extractors = new()
		{
		new CsvExtractor(),
        // new XlsxExtractor(),  // Added by Unio.Excel package
        // new PdfExtractor(),   // Added by Unio.Pdf package
    };

		/// <summary>
		/// Registers a format extractor. Called by extension packages on init.
		/// </summary>
		public static void RegisterExtractor(IDataExtractor extractor) {
			_extractors.Add(extractor);
		}

		/// <summary>
		/// Extract typed records from a file path.
		/// Format is auto-detected by extension and magic bytes.
		/// </summary>
		public static IEnumerable<T> Extract<T>(string filePath, Action<ExtractionOptions>? configure = null)
			where T : class, new() {
			var options = new ExtractionOptions();
			configure?.Invoke(options);

			using var stream = File.OpenRead(filePath);
			var format = FormatDetector.Detect(stream, filePath);
			var extractor = ResolveExtractor(stream, Path.GetExtension(filePath));

			return extractor.Extract<T>(stream, options).ToList(); // Materialize before stream closes
		}

		/// <summary>
		/// Extract typed records from a stream.
		/// </summary>
		public static IEnumerable<T> Extract<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) where T : class, new() {
			var options = new ExtractionOptions();
			configure?.Invoke(options);

			var extractor = ResolveExtractor(stream, fileExtension);
			return extractor.Extract<T>(stream, options);
		}

		/// <summary>
		/// Extract dynamic rows from a file path (no model needed).
		/// </summary>
		public static IEnumerable<dynamic> Extract(string filePath, Action<ExtractionOptions>? configure = null) {
			var options = new ExtractionOptions();
			configure?.Invoke(options);

			using var stream = File.OpenRead(filePath);
			var extractor = ResolveExtractor(stream, Path.GetExtension(filePath));
			return extractor.Extract(stream, options).ToList();
		}

		/// <summary>
		/// Async streaming extraction from a file path.
		/// </summary>
		public static async IAsyncEnumerable<T> ExtractAsync<T>(string filePath,
			Action<ExtractionOptions>? configure = null,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
			where T : class, new() {
			var options = new ExtractionOptions();
			configure?.Invoke(options);

			await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
				FileShare.Read, 8192, useAsync: true);

			var extractor = ResolveExtractor(stream, Path.GetExtension(filePath));

			await foreach (var record in extractor.ExtractAsync<T>(stream, options, ct)) {
				yield return record;
			}
		}

		/// <summary>
		/// Full extraction with error reporting.
		/// </summary>
		public static ExtractionResult<T> ExtractWithErrors<T>(string filePath,
			Action<ExtractionOptions>? configure = null) where T : class, new() {
			var options = new ExtractionOptions { OnError = ErrorHandling.CollectAndContinue };
			configure?.Invoke(options);

			var records = new List<T>();
			var errors = new List<ExtractionError>();
			int totalRows = 0;

			using var stream = File.OpenRead(filePath);
			var extractor = ResolveExtractor(stream, Path.GetExtension(filePath));

			foreach (var item in extractor.Extract<T>(stream, options)) {
				records.Add(item);
				totalRows++;
			}

			return new ExtractionResult<T> {
				Records = records,
				Errors = errors,
				TotalRowsRead = totalRows,
			};
		}

		private static IDataExtractor ResolveExtractor(Stream stream, string? fileExtension) {
			foreach (var extractor in _extractors) {
				if (extractor.CanHandle(stream, fileExtension))
					return extractor;
			}

			throw new UnioFormatException(
				$"No extractor registered for format '{fileExtension ?? "unknown"}'. " +
				$"Install the appropriate Unio package (e.g., Unio.Excel, Unio.Pdf).");
		}
	}

	public class UnioFormatException : Exception {
		public UnioFormatException(string message) : base(message) { }
	}
}
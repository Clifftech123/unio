using System.Runtime.CompilerServices;
using Unio.Abstractions;
using Unio.Csv;
using Unio.Mapping;

namespace Unio {

	/// <summary>
	/// Main entry point for the Unio library.
	/// Works standalone or injected via DI as IUnioExtractor.
	///
	/// Standalone:
	///   var unio = new Unio();
	///   var records = unio.Extract&lt;Invoice&gt;("invoices.csv");
	///
	/// With DI:
	///   services.AddUnio(config =&gt; config.RegisterExtractor&lt;XlsxExtractor&gt;());
	///   // inject IUnioExtractor
	/// </summary>
	public sealed class Unio : IUnioExtractor {
		private readonly List<IDataExtractor> _extractors;
		private readonly UnioConfiguration _config;

		/// <summary>
		/// Creates a default Unio instance with CSV support.
		/// </summary>
		public Unio() : this(new UnioConfiguration()) { }

		/// <summary>
		/// Creates a Unio instance with custom configuration.
		/// Used by DI registration.
		/// </summary>
		public Unio(UnioConfiguration config) {
			_config = config;
			_extractors = [new CsvExtractor(), .. config.Extractors];
		}

		/// <summary>
		/// Registers an additional format extractor at runtime.
		/// </summary>
		public void RegisterExtractor(IDataExtractor extractor) {
			_extractors.Add(extractor);
		}

		/// <summary>Extract typed records from a file path.</summary>
		public IEnumerable<T> Extract<T>(string filePath, Action<ExtractionOptions>? configure = null)
			where T : class, new() {
			using var stream = File.OpenRead(filePath);
			return Extract<T>(stream, Path.GetExtension(filePath), configure).ToList();
		}

		/// <summary>Extract typed records from a stream.</summary>
		public IEnumerable<T> Extract<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) where T : class, new() {
			var options = BuildOptions(configure);
			var extractor = ResolveExtractor(stream, fileExtension);
			return extractor.Extract<T>(stream, options);
		}

		/// <summary>Extract dynamic rows from a file path.</summary>
		public IEnumerable<dynamic> Extract(string filePath, Action<ExtractionOptions>? configure = null) {
			using var stream = File.OpenRead(filePath);
			return Extract(stream, Path.GetExtension(filePath), configure).ToList();
		}

		/// <summary>Extract dynamic rows from a stream.</summary>
		public IEnumerable<dynamic> Extract(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) {
			var options = BuildOptions(configure);
			var extractor = ResolveExtractor(stream, fileExtension);
			return extractor.Extract(stream, options);
		}

		/// <summary>Async streaming extraction from a file path.</summary>
		public async IAsyncEnumerable<T> ExtractAsync<T>(string filePath,
			Action<ExtractionOptions>? configure = null,
			[EnumeratorCancellation] CancellationToken ct = default)
			where T : class, new() {
			await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
				FileShare.Read, 8192, useAsync: true);

			await foreach (var record in ExtractAsync<T>(stream, Path.GetExtension(filePath), configure, ct)) {
				yield return record;
			}
		}

		/// <summary>Async streaming extraction from a stream.</summary>
		public async IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null,
			[EnumeratorCancellation] CancellationToken ct = default) where T : class, new() {
			var options = BuildOptions(configure);
			var extractor = ResolveExtractor(stream, fileExtension);

			await foreach (var record in extractor.ExtractAsync<T>(stream, options, ct)) {
				yield return record;
			}
		}

		/// <summary>Extract with error reporting and optional validation.</summary>
		public ExtractionResult<T> ExtractWithErrors<T>(string filePath,
			Action<ExtractionOptions>? configure = null) where T : class, new() {
			using var stream = File.OpenRead(filePath);
			return ExtractWithErrors<T>(stream, Path.GetExtension(filePath), configure);
		}

		/// <summary>Extract with error reporting and optional validation.</summary>
		public ExtractionResult<T> ExtractWithErrors<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) where T : class, new() {
			var options = BuildOptions(configure);
			options.OnError = ErrorHandling.CollectAndContinue;
			var extractor = ResolveExtractor(stream, fileExtension);

			var records = new List<T>();
			var errors = new List<ExtractionError>();
			int totalRows = 0;

			foreach (var item in extractor.Extract<T>(stream, options)) {
				totalRows++;

				if (options.RecordValidator is not null) {
					var msgs = options.RecordValidator(item);
					if (msgs.Count > 0) {
						foreach (var msg in msgs)
							errors.Add(new ExtractionError(totalRows, null, msg));

						if (options.OnError == ErrorHandling.ThrowOnFirst)
							throw new InvalidOperationException(msgs[0]);

						continue;
					}
				}

				records.Add(item);
			}

			return new ExtractionResult<T> {
				Records = records,
				Errors = errors,
				TotalRowsRead = totalRows,
			};
		}

		private ExtractionOptions BuildOptions(Action<ExtractionOptions>? configure) {
			var options = new ExtractionOptions {
				Culture = _config.DefaultCulture,
				OnError = _config.OnError,
				RecordValidator = _config.DefaultValidator,
			};
			configure?.Invoke(options);
			return options;
		}

		private IDataExtractor ResolveExtractor(Stream stream, string? fileExtension) {
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

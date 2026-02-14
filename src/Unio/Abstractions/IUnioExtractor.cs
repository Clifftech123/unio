using System.Runtime.CompilerServices;

namespace Unio.Abstractions {

	/// <summary>
	/// Injectable extraction service. Use this with DI instead of the static Unio class.
	///
	/// Usage:
	///   public class ReportService(IUnioExtractor extractor) {
	///       public async Task&lt;List&lt;Invoice&gt;&gt; LoadAsync(Stream file)
	///           =&gt; await extractor.ExtractAsync&lt;Invoice&gt;(file).ToListAsync();
	///   }
	/// </summary>
	public interface IUnioExtractor {

		/// <summary>
		/// Extract typed records from a stream.
		/// Format is auto-detected by extension and magic bytes.
		/// </summary>
		IEnumerable<T> Extract<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) where T : class, new();

		/// <summary>
		/// Extract dynamic rows from a stream (no model needed).
		/// </summary>
		IEnumerable<dynamic> Extract(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null);

		/// <summary>
		/// Async streaming extraction from a stream.
		/// </summary>
		IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null,
			[EnumeratorCancellation] CancellationToken ct = default) where T : class, new();

		/// <summary>
		/// Extract with error reporting and optional validation.
		/// </summary>
		ExtractionResult<T> ExtractWithErrors<T>(Stream stream, string? fileExtension = null,
			Action<ExtractionOptions>? configure = null) where T : class, new();
	}
}

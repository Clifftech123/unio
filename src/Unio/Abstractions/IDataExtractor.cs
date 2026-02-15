

namespace Unio.Abstractions
{/// <summary>
/// Core interface for all format-specific extractors.
/// Each format (CSV, XLSX, PDF, etc.) implements this interface.
/// </summary>
public interface IDataExtractor
{
    /// <summary>
    /// The file formats this extractor can handle.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Checks if this extractor can handle the given stream by inspecting magic bytes.
    /// </summary>
    bool CanHandle(Stream stream, string? fileExtension = null);

    /// <summary>
    /// Extracts typed records from a stream.
    /// </summary>
    IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new();

    /// <summary>
    /// Extracts rows as dynamic objects (no model required).
    /// </summary>
    IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null);
    /// <summary>
    /// Extracts typed records asynchronously with streaming support.
    /// </summary>
    IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null,
        CancellationToken cancellationToken = default) where T : class, new();

}
}
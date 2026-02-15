namespace Unio.Abstractions
{
   /// <summary>
/// Wraps extraction results with row-level error reporting.
/// </summary>
public class ExtractionResult<T> where T : class
{
    /// <summary>The successfully extracted records.</summary>
    public IReadOnlyList<T> Records { get; init; } = Array.Empty<T>();
    /// <summary>Errors encountered during extraction.</summary>
    public IReadOnlyList<ExtractionError> Errors { get; init; } = Array.Empty<ExtractionError>();
    /// <summary>Total number of rows read from the source.</summary>
    public int TotalRowsRead { get; init; }
    /// <summary>Number of successfully extracted records.</summary>
    public int SuccessCount => Records.Count;
    /// <summary>Number of errors encountered.</summary>
    public int ErrorCount => Errors.Count;
    /// <summary>Whether any errors were encountered during extraction.</summary>
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Represents an error encountered while extracting a specific row.
/// </summary>
/// <param name="RowNumber">Zero-based row number where the error occurred.</param>
/// <param name="ColumnName">Column name associated with the error, if applicable.</param>
/// <param name="Message">Description of the error.</param>
/// <param name="InnerException">The underlying exception, if any.</param>
public record ExtractionError(
    int RowNumber,
    string? ColumnName,
    string Message,
    Exception? InnerException = null
);
}
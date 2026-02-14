namespace Unio.Abstractions
{
   /// <summary>
/// Wraps extraction results with row-level error reporting.
/// </summary>
public class ExtractionResult<T> where T : class
{
    public IReadOnlyList<T> Records { get; init; } = Array.Empty<T>();
    public IReadOnlyList<ExtractionError> Errors { get; init; } = Array.Empty<ExtractionError>();
    public int TotalRowsRead { get; init; }
    public int SuccessCount => Records.Count;
    public int ErrorCount => Errors.Count;
    public bool HasErrors => Errors.Count > 0;
}

public record ExtractionError(
    int RowNumber,
    string? ColumnName,
    string Message,
    Exception? InnerException = null
);
}
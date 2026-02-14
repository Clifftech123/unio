

using System.Globalization;

namespace Unio.Abstractions
{/// <summary>
/// Configuration options for data extraction.
/// </summary>
public class ExtractionOptions
{
    /// <summary>Whether the first data row contains column headers.</summary>
    public bool HasHeaderRow { get; set; } = true;

    /// <summary>Zero-based row index to start reading from.</summary>
    public int StartRow { get; set; } = 0;

    /// <summary>Sheet name to read from (Excel only). Null = first sheet.</summary>
    public string? SheetName { get; set; }

    /// <summary>Sheet index to read from (Excel only). Ignored if SheetName is set.</summary>
    public int SheetIndex { get; set; } = 0;

    /// <summary>Culture for parsing numbers, dates, currencies.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Whether to validate records using DataAnnotations.</summary>
    public bool Validate { get; set; } = false;

    /// <summary>How to handle extraction errors.</summary>
    public ErrorHandling OnError { get; set; } = ErrorHandling.ThrowOnFirst;

    /// <summary>CSV delimiter override. Null = auto-detect.</summary>
    public char? Delimiter { get; set; }

    /// <summary>Maximum number of rows to read. Null = read all.</summary>
    public int? MaxRows { get; set; }
}

public enum ErrorHandling
{
    /// <summary>Throw immediately on the first error.</summary>
    ThrowOnFirst,

    /// <summary>Skip bad rows and continue. Errors accessible via ExtractionResult.</summary>
    SkipAndContinue,

    /// <summary>Collect all errors and return them alongside valid records.</summary>
    CollectAndContinue
}
}
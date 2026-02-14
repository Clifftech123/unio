# Unio

**One API, every format.** Extract typed data from Excel, CSV, PDF, JSON, and XML in C#.

[![NuGet](https://img.shields.io/badge/nuget-1.0.0--preview-blue?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Unio)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](https://github.com/Clifftech123/unio/blob/main/LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)

---

## Why Unio

Reading an Excel file requires one library. CSV needs another. PDF tables need a third. Each has its own API, its own patterns, its own quirks.

Unio gives you **one API** for all of them:

```csharp
var unio = new Unio();
var invoices = unio.Extract<Invoice>("invoices.xlsx");
var invoices = unio.Extract<Invoice>("invoices.csv");
var invoices = unio.Extract<Invoice>("invoices.pdf");
var invoices = unio.Extract<Invoice>("invoices.json");
```

Same call. Same result. Format is auto-detected.

---

## Installation

Install the core package and add only the formats you need:

```bash
dotnet add package Unio              # Core + CSV
dotnet add package Unio.Excel        # Add Excel support
dotnet add package Unio.Pdf          # Add PDF support
```

The core package has **zero external dependencies**.

---

## Usage

### Define your model

```csharp
public class Invoice
{
    [Column("Invoice Number")]
    public string InvoiceNo { get; set; }

    [Column("Total Amount")]
    [Required]
    public decimal Amount { get; set; }

    [DateFormat("dd/MM/yyyy")]
    public DateTime DueDate { get; set; }

    [Ignore]
    public string InternalNotes { get; set; }
}
```

### Extract data

```csharp
// From any supported format -- auto-detected
var unio = new Unio();
var invoices = unio.Extract<Invoice>("invoices.xlsx");
```

### Stream large files

Process millions of rows without loading everything into memory:

```csharp
var unio = new Unio();
await foreach (var invoice in unio.ExtractAsync<Invoice>("huge-file.xlsx"))
{
    await ProcessAsync(invoice);
}
```

### Go dynamic

No model? No problem:

```csharp
var unio = new Unio();
var rows = unio.Extract("data.csv"); // IEnumerable<dynamic>
```

### Configure extraction

```csharp
var unio = new Unio();
var records = unio.Extract<Invoice>("data.xlsx", opt =>
{
    opt.SheetName = "Sheet2";
    opt.StartRow = 3;
    opt.HasHeaderRow = true;
    opt.Validate = true;
});
```

### Use with dependency injection

```csharp
services.AddUnio(config =>
{
    config.RegisterExtractor<XlsxExtractor>();
    config.RegisterExtractor<PdfTableExtractor>();
    config.DefaultCulture = new CultureInfo("en-US");
    config.OnError = ErrorHandling.CollectAndContinue;
});
```

```csharp
public class ReportService(IUnioExtractor extractor)
{
    public async Task<List<Invoice>> LoadAsync(Stream file)
        => await extractor.ExtractAsync<Invoice>(file).ToListAsync();
}
```

---

## Packages

| Package | Format | External Dependencies |
|:---|:---|:---|
| `Unio` | CSV | None (+ `Microsoft.Extensions.DependencyInjection.Abstractions`) |
| `Unio.Excel` | XLSX, XLS | [DocumentFormat.OpenXml](https://www.nuget.org/packages/DocumentFormat.OpenXml) |
| `Unio.Pdf` | PDF tables | [PdfPig](https://www.nuget.org/packages/PdfPig) |
| `Unio.Json` | JSON | None (built-in `System.Text.Json`) |
| `Unio.Xml` | XML | None (built-in `System.Xml.Linq`) |
| `Unio.Validation` | -- | None (built-in `System.ComponentModel.DataAnnotations`) |

---

## Key Features

- **Unified API** -- `Extract<T>()` works the same across every format
- **Strongly typed** -- Map directly to your POCOs with attributes
- **Streaming first** -- `IAsyncEnumerable<T>` across all formats, never loads entire files into memory
- **Zero-dep core** -- The core package has no external dependencies
- **Modular** -- Install only the format readers you need
- **Validation** -- Built-in support for `DataAnnotations` and fluent validation rules
- **DI-first** -- Implements `IUnioExtractor` for clean dependency injection
- **Cross-platform** -- No `System.Drawing`, no COM, no `libgdiplus`. Works on Windows, Linux, macOS, and containers
- **Auto-detection** -- File format detected by magic bytes and extension

---

## Validation

### DataAnnotations (attribute-based)

Add standard `[Required]`, `[Range]`, `[StringLength]` attributes to your model, then:

```csharp
using Unio.Validation;

var result = unio.ExtractWithErrors<Invoice>("invoices.csv", opt =>
{
    opt.UseDataAnnotationValidation();
});

Console.WriteLine($"Valid: {result.SuccessCount}, Invalid: {result.ErrorCount}");

foreach (var error in result.Errors)
    Console.WriteLine($"  Row {error.RowNumber}: {error.Message}");
```

### Fluent Validation (no attributes needed)

```csharp
using Unio.Validation;

var validator = new FluentRecordValidator<Invoice>()
    .RuleFor(x => x.Amount, v => v.GreaterThan(0m).LessThan(1_000_000m))
    .RuleFor(x => x.InvoiceNo, v => v.NotEmpty().MaxLength(20))
    .RuleFor(x => x.DueDate, v => v.NotDefault());

var result = unio.ExtractWithErrors<Invoice>("invoices.csv", opt =>
{
    opt.UseFluentValidation(validator);
});

// result.Records -- valid records only
// result.Errors  -- validation failures with row numbers
```

### Standalone Validation (after extraction)

```csharp
var records = unio.Extract<Invoice>("invoices.csv");
var batch = new DataAnnotationValidator().ValidateAll(records);

// batch.Valid    -- records that passed
// batch.Invalid  -- records that failed
// batch.Errors   -- all validation errors with details
```

---

## Error Handling

```csharp
// Collect all errors and get valid records
var result = unio.ExtractWithErrors<Invoice>("data.csv");
// result.Records  -- valid records
// result.Errors   -- all errors with row numbers
// result.HasErrors -- quick check

// Configure error handling mode
var data = unio.Extract<Invoice>("data.csv", opt =>
{
    opt.OnError = ErrorHandling.ThrowOnFirst;       // Throw immediately
    opt.OnError = ErrorHandling.SkipAndContinue;    // Skip bad rows
    opt.OnError = ErrorHandling.CollectAndContinue; // Collect errors
});
```

---

## How It Works

```
File / Stream
     |
     v
 FormatDetector         Detects format via magic bytes + extension
     |
     v
 IDataExtractor<T>      Format-specific reader (CSV, XLSX, PDF...)
     |
     v
 TypeMapper             Maps columns to properties via attributes
     |
     v
 ValidationEngine       Validates using DataAnnotations
     |
     v
 IEnumerable<T>         Your strongly-typed data, ready to use
```

---

## Documentation

Full documentation and guides are available in the [Wiki](https://github.com/Clifftech123/unio/wiki).

---

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

```bash
git clone https://github.com/Clifftech123/unio.git
cd unio
dotnet build
dotnet test
```

---

## License

MIT -- [Isaiah Clifford Opoku](https://github.com/Clifftech123)

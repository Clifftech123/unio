# Unio.Core

**One API, every format.** Extract typed data from Excel, CSV, PDF, JSON, and XML in C#.

## Installation

```bash
dotnet add package Unio.Core         # Core + CSV
dotnet add package Unio.Excel        # Add Excel support
dotnet add package Unio.Pdf          # Add PDF support
dotnet add package Unio.Json         # Add JSON support
dotnet add package Unio.Xml          # Add XML support
dotnet add package Unio.Validation   # Add validation support
```

The core package has **zero external dependencies** and includes CSV support out of the box.

## Quick Start

```csharp
using Unio;

// Define your model
public class Invoice
{
    [Column("Invoice Number")]
    public string InvoiceNumber { get; set; }

    [Column("Amount")]
    public decimal Amount { get; set; }

    [Column("Date")]
    [DateFormat("yyyy-MM-dd")]
    public DateTime Date { get; set; }
}

// Extract data - format is auto-detected
var unio = new Unio();
var invoices = unio.Extract<Invoice>("invoices.csv");

foreach (var invoice in invoices)
{
    Console.WriteLine($"{invoice.InvoiceNumber}: {invoice.Amount:C}");
}
```

## Dependency Injection

```csharp
builder.Services.AddUnio();

// Then inject IUnioExtractor
public class MyService(IUnioExtractor extractor)
{
    public IEnumerable<Invoice> Import(string path)
        => extractor.Extract<Invoice>(path);
}
```

## Streaming Large Files

```csharp
await foreach (var invoice in unio.ExtractAsync<Invoice>("large-file.csv"))
{
    // Process one row at a time - constant memory usage
}
```

## Key Features

- **Unified API** - Same `Extract<T>()` call for CSV, Excel, PDF, JSON, XML
- **Auto-detection** - Format detected from file content (magic bytes) and extension
- **Attribute mapping** - `[Column]`, `[Ignore]`, `[DateFormat]` for flexible mapping
- **Streaming** - `IAsyncEnumerable<T>` support for memory-efficient processing
- **DI-first** - Built-in `AddUnio()` for Microsoft.Extensions.DependencyInjection
- **Zero dependencies** - Core library has no external package dependencies

## Links

- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)
- [License (MIT)](https://github.com/Clifftech123/unio/blob/main/LICENSE)

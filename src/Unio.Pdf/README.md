# Unio.Pdf

PDF table data extraction plugin for [Unio](https://www.nuget.org/packages/Unio).

## Installation

```bash
dotnet add package Unio           # Required: core library
dotnet add package Unio.Pdf       # PDF support
```

## Usage

```csharp
using Unio;

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

var unio = new Unio();

// Extract from PDF tables - format is auto-detected
var invoices = unio.Extract<Invoice>("invoices.pdf");

foreach (var invoice in invoices)
{
    Console.WriteLine($"{invoice.InvoiceNumber}: {invoice.Amount:C}");
}
```

## Features

- Automatic table detection in PDF documents
- Column mapping via `[Column]` attribute
- Row and header parsing
- Built on [PdfPig](https://www.nuget.org/packages/PdfPig) (open-source, Apache 2.0)

## Links

- [Unio on NuGet](https://www.nuget.org/packages/Unio)
- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)

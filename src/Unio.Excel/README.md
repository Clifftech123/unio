# Unio.Excel

Excel (XLSX/XLS) data extraction plugin for [Unio](https://www.nuget.org/packages/Unio).

## Installation

```bash
dotnet add package Unio           # Required: core library
dotnet add package Unio.Excel     # Excel support
```

## Usage

```csharp
using Unio;

public class Employee
{
    [Column("Name")]
    public string Name { get; set; }

    [Column("Department")]
    public string Department { get; set; }

    [Column("Salary")]
    public decimal Salary { get; set; }
}

var unio = new Unio();

// Extract from XLSX - format is auto-detected
var employees = unio.Extract<Employee>("employees.xlsx");

// Stream large Excel files
await foreach (var emp in unio.ExtractAsync<Employee>("large-report.xlsx"))
{
    Console.WriteLine($"{emp.Name}: {emp.Salary:C}");
}
```

## Features

- XLSX and XLS format support
- Automatic sheet detection
- Column mapping via `[Column]` attribute
- Streaming for large files via `IAsyncEnumerable<T>`
- Built on [DocumentFormat.OpenXml](https://www.nuget.org/packages/DocumentFormat.OpenXml)

## Links

- [Unio on NuGet](https://www.nuget.org/packages/Unio)
- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)

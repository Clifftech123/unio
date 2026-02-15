# Unio.Xml

XML data extraction plugin for [Unio](https://www.nuget.org/packages/Unio).

## Installation

```bash
dotnet add package Unio           # Required: core library
dotnet add package Unio.Xml       # XML support
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

// Extract from XML - format is auto-detected
var employees = unio.Extract<Employee>("employees.xml");

// Stream large XML files
await foreach (var emp in unio.ExtractAsync<Employee>("employees.xml"))
{
    Console.WriteLine($"{emp.Name} - {emp.Department}");
}
```

## Features

- Element-based and XPath-based extraction
- Automatic property mapping via `[Column]` attribute
- Streaming for large files via `IAsyncEnumerable<T>`
- Built on System.Xml.Linq (zero additional dependencies)

## Links

- [Unio on NuGet](https://www.nuget.org/packages/Unio)
- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)

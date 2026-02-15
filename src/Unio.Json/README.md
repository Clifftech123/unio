# Unio.Json

JSON data extraction plugin for [Unio](https://www.nuget.org/packages/Unio).

## Installation

```bash
dotnet add package Unio           # Required: core library
dotnet add package Unio.Json      # JSON support
```

## Usage

```csharp
using Unio;

public class Product
{
    [Column("name")]
    public string Name { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("category")]
    public string Category { get; set; }
}

var unio = new Unio();

// Extract from JSON array - format is auto-detected
var products = unio.Extract<Product>("products.json");

// Stream large JSON files
await foreach (var product in unio.ExtractAsync<Product>("catalog.json"))
{
    Console.WriteLine($"{product.Name}: {product.Price:C}");
}
```

## Features

- JSON array and object extraction
- Automatic property mapping via `[Column]` attribute
- Streaming for large files via `IAsyncEnumerable<T>`
- Built on System.Text.Json (zero additional dependencies)

## Links

- [Unio on NuGet](https://www.nuget.org/packages/Unio)
- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)

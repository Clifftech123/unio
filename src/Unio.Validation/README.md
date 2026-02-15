# Unio.Validation

Data validation plugin for [Unio](https://www.nuget.org/packages/Unio).

## Installation

```bash
dotnet add package Unio                # Required: core library
dotnet add package Unio.Validation     # Validation support
```

## Usage

### DataAnnotations Validation

```csharp
using Unio;
using Unio.Validation;
using System.ComponentModel.DataAnnotations;

public class Employee
{
    [Column("Name")]
    [Required]
    public string Name { get; set; }

    [Column("Email")]
    [EmailAddress]
    public string Email { get; set; }

    [Column("Salary")]
    [Range(0, 1_000_000)]
    public decimal Salary { get; set; }
}

var unio = new Unio();
var employees = unio.Extract<Employee>("employees.csv");

// Validate extracted data
var validated = employees.ValidateWithAnnotations();
```

### Fluent Validation

```csharp
var validated = employees.ValidateWith(rule => rule
    .For(e => e.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required")
    .For(e => e.Salary, salary => salary > 0, "Salary must be positive"));
```

### Error Handling Modes

- **ThrowOnFirst** - Throw on the first validation error
- **SkipAndContinue** - Skip invalid records, return only valid ones
- **CollectAndContinue** - Return all records with error details attached

## Features

- DataAnnotations support (`[Required]`, `[Range]`, `[EmailAddress]`, etc.)
- Fluent validation rules for custom logic
- Three error handling strategies
- Zero additional dependencies beyond Unio core

## Links

- [Unio on NuGet](https://www.nuget.org/packages/Unio)
- [GitHub Repository](https://github.com/Clifftech123/unio)
- [Documentation](https://github.com/Clifftech123/unio/wiki)

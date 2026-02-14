using System.ComponentModel.DataAnnotations;
using Unio.Mapping;

namespace Unio.Tests.Models;

// Matches: employees.csv / employees.json / employees.xml / employees.tsv
public class Employee {
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Department { get; set; } = "";
	public decimal Salary { get; set; }
	public DateTime StartDate { get; set; }
	public bool IsActive { get; set; }
	public string Email { get; set; } = "";
}

// Matches: invoices.csv / invoices.json / invoices.xml
public class Invoice {
	public string InvoiceNo { get; set; } = "";
	public string Customer { get; set; } = "";
	public DateTime Date { get; set; }
	public decimal Amount { get; set; }
	public decimal Tax { get; set; }
	public decimal Total { get; set; }
	public string Status { get; set; } = "";
	public DateTime DueDate { get; set; }
}

// Matches: products.csv / products.json / products.xml
public class Product {
	public string ProductId { get; set; } = "";
	public string Name { get; set; } = "";
	public string Category { get; set; } = "";
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public bool InStock { get; set; }
	public double Rating { get; set; }
}

// Matches: edge-cases.csv / edge-cases.json
public class EdgeCaseRecord {
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Notes { get; set; } = "";
	public decimal Value { get; set; }
	public string Empty { get; set; } = "";
	public DateTime Date { get; set; }
}

// For validation tests
public class ValidatedEmployee {
	[Required(ErrorMessage = "Name is required.")]
	public string Name { get; set; } = "";

	[Required]
	public string Department { get; set; } = "";

	[Range(0.01, double.MaxValue, ErrorMessage = "Salary must be positive.")]
	public decimal Salary { get; set; }

	public DateTime StartDate { get; set; }
	public bool IsActive { get; set; }

	[Required]
	[EmailAddress(ErrorMessage = "Invalid email.")]
	public string Email { get; set; } = "";
}

// For [Ignore] attribute test
public class EmployeeWithIgnored {
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Department { get; set; } = "";
	public decimal Salary { get; set; }

	[Ignore]
	public string Computed { get; set; } = "default-value";
}

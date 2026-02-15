using System.ComponentModel.DataAnnotations;

namespace Unio.Samples.API.Models;

/// <summary>
/// Employee record extracted from uploaded files and stored in SQLite.
/// </summary>
public class Employee {
	[Key]
	public int RowId { get; set; }

	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Department { get; set; } = "";
	public decimal Salary { get; set; }
	public DateTime StartDate { get; set; }
	public bool IsActive { get; set; }
	public string Email { get; set; } = "";

	/// <summary>Source file name this record was extracted from.</summary>
	public string SourceFile { get; set; } = "";
	public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Product record extracted from uploaded files and stored in SQLite.
/// </summary>
public class Product {
	[Key]
	public int RowId { get; set; }

	public string ProductId { get; set; } = "";
	public string Name { get; set; } = "";
	public string Category { get; set; } = "";
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public bool InStock { get; set; }
	public double Rating { get; set; }

	public string SourceFile { get; set; } = "";
	public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

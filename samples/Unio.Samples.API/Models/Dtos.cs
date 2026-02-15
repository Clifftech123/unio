namespace Unio.Samples.API.Models;

/// <summary>Unio extraction models used for file parsing (no DB keys).</summary>
public class EmployeeRecord {
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Department { get; set; } = "";
	public decimal Salary { get; set; }
	public DateTime StartDate { get; set; }
	public bool IsActive { get; set; }
	public string Email { get; set; } = "";
}

public class ProductRecord {
	public string ProductId { get; set; } = "";
	public string Name { get; set; } = "";
	public string Category { get; set; } = "";
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public bool InStock { get; set; }
	public double Rating { get; set; }
}

public class ImportResult {
	public string FileName { get; set; } = "";
	public string DataType { get; set; } = "";
	public int RecordsImported { get; set; }
	public int Errors { get; set; }
	public List<string> ErrorMessages { get; set; } = [];
}

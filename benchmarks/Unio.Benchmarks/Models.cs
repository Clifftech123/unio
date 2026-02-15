namespace Unio.Benchmarks;

public class Employee {
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Department { get; set; } = "";
	public decimal Salary { get; set; }
	public DateTime StartDate { get; set; }
	public bool IsActive { get; set; }
	public string Email { get; set; } = "";
}

public class Product {
	public string ProductId { get; set; } = "";
	public string Name { get; set; } = "";
	public string Category { get; set; } = "";
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public bool InStock { get; set; }
	public double Rating { get; set; }
}

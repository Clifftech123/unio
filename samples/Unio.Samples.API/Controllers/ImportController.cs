using Microsoft.AspNetCore.Mvc;
using Unio.Abstractions;
using Unio.Samples.API.Data;
using Unio.Samples.API.Models;

namespace Unio.Samples.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase {
	private readonly IUnioExtractor _unio;
	private readonly AppDbContext _db;

	public ImportController(IUnioExtractor unio, AppDbContext db) {
		_unio = unio;
		_db = db;
	}

	/// <summary>
	/// Upload a file containing employee data (CSV, JSON, XML, XLSX, or PDF).
	/// Unio extracts the records and stores them in SQLite.
	/// </summary>
	[HttpPost("employees")]
	public async Task<ActionResult<ImportResult>> ImportEmployees(IFormFile file) {
		if (file.Length == 0)
			return BadRequest("File is empty.");

		var ext = Path.GetExtension(file.FileName);
		using var stream = file.OpenReadStream();

		var result = _unio.ExtractWithErrors<EmployeeRecord>(stream, ext);

		var entities = result.Records.Select(r => new Employee {
			Id = r.Id,
			Name = r.Name,
			Department = r.Department,
			Salary = r.Salary,
			StartDate = r.StartDate,
			IsActive = r.IsActive,
			Email = r.Email,
			SourceFile = file.FileName,
			ImportedAt = DateTime.UtcNow,
		}).ToList();

		_db.Employees.AddRange(entities);
		await _db.SaveChangesAsync();

		return Ok(new ImportResult {
			FileName = file.FileName,
			DataType = "Employee",
			RecordsImported = entities.Count,
			Errors = result.ErrorCount,
			ErrorMessages = result.Errors.Select(e => e.Message).ToList(),
		});
	}

	/// <summary>
	/// Upload a file containing product data (CSV, JSON, XML, XLSX, or PDF).
	/// Unio extracts the records and stores them in SQLite.
	/// </summary>
	[HttpPost("products")]
	public async Task<ActionResult<ImportResult>> ImportProducts(IFormFile file) {
		if (file.Length == 0)
			return BadRequest("File is empty.");

		var ext = Path.GetExtension(file.FileName);
		using var stream = file.OpenReadStream();

		var result = _unio.ExtractWithErrors<ProductRecord>(stream, ext);

		var entities = result.Records.Select(r => new Product {
			ProductId = r.ProductId,
			Name = r.Name,
			Category = r.Category,
			Price = r.Price,
			Quantity = r.Quantity,
			InStock = r.InStock,
			Rating = r.Rating,
			SourceFile = file.FileName,
			ImportedAt = DateTime.UtcNow,
		}).ToList();

		_db.Products.AddRange(entities);
		await _db.SaveChangesAsync();

		return Ok(new ImportResult {
			FileName = file.FileName,
			DataType = "Product",
			RecordsImported = entities.Count,
			Errors = result.ErrorCount,
			ErrorMessages = result.Errors.Select(e => e.Message).ToList(),
		});
	}
}

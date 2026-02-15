using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unio.Samples.API.Data;
using Unio.Samples.API.Models;

namespace Unio.Samples.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
	private readonly AppDbContext _db;

	public ProductsController(AppDbContext db) => _db = db;

	[HttpGet]
	public async Task<ActionResult<List<Product>>> GetAll() =>
		Ok(await _db.Products.OrderBy(p => p.ProductId).ToListAsync());

	[HttpGet("{productId}")]
	public async Task<ActionResult<Product>> GetById(string productId) {
		var prod = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
		return prod is null ? NotFound() : Ok(prod);
	}

	[HttpGet("by-category/{category}")]
	public async Task<ActionResult<List<Product>>> GetByCategory(string category) =>
		Ok(await _db.Products
			.Where(p => p.Category.ToLower() == category.ToLower())
			.OrderBy(p => p.ProductId)
			.ToListAsync());

	[HttpGet("stats")]
	public async Task<ActionResult> GetStats() {
		var products = await _db.Products.ToListAsync();
		return Ok(new {
			TotalCount = products.Count,
			ByCategory = products.GroupBy(p => p.Category)
				.Select(g => new { Category = g.Key, Count = g.Count(), AvgPrice = g.Average(p => (double)p.Price) }),
			InStock = products.Count(p => p.InStock),
			OutOfStock = products.Count(p => !p.InStock),
		});
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteAll() {
		_db.Products.RemoveRange(_db.Products);
		var deleted = await _db.SaveChangesAsync();
		return Ok(new { Deleted = deleted });
	}
}

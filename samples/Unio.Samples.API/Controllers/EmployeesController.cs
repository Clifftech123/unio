using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unio.Samples.API.Data;
using Unio.Samples.API.Models;

namespace Unio.Samples.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase {
	private readonly AppDbContext _db;

	public EmployeesController(AppDbContext db) => _db = db;

	[HttpGet]
	public async Task<ActionResult<List<Employee>>> GetAll() =>
		Ok(await _db.Employees.OrderBy(e => e.Id).ToListAsync());

	[HttpGet("{id:int}")]
	public async Task<ActionResult<Employee>> GetById(int id) {
		var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);
		return emp is null ? NotFound() : Ok(emp);
	}

	[HttpGet("by-department/{department}")]
	public async Task<ActionResult<List<Employee>>> GetByDepartment(string department) =>
		Ok(await _db.Employees
			.Where(e => e.Department.ToLower() == department.ToLower())
			.OrderBy(e => e.Id)
			.ToListAsync());

	[HttpGet("stats")]
	public async Task<ActionResult> GetStats() {
		var employees = await _db.Employees.ToListAsync();
		return Ok(new {
			TotalCount = employees.Count,
			ByDepartment = employees.GroupBy(e => e.Department)
				.Select(g => new { Department = g.Key, Count = g.Count(), AvgSalary = g.Average(e => (double)e.Salary) }),
			SourceFiles = employees.Select(e => e.SourceFile).Distinct(),
		});
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteAll() {
		_db.Employees.RemoveRange(_db.Employees);
		var deleted = await _db.SaveChangesAsync();
		return Ok(new { Deleted = deleted });
	}
}

using Microsoft.EntityFrameworkCore;
using Unio.Samples.API.Models;

namespace Unio.Samples.API.Data;

public class AppDbContext : DbContext {
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<Employee> Employees => Set<Employee>();
	public DbSet<Product> Products => Set<Product>();
}

using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Unio;
using Unio.Abstractions;
using Unio.Excel;
using Unio.Json;
using Unio.Pdf;
using Unio.Samples.API.Data;
using Unio.Xml;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + SQLite ---
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite("Data Source=unio_samples.db"));

// --- Unio (DI registration with AddUnio) ---
// This is the recommended way to register Unio in ASP.NET Core.
// It registers IUnioExtractor as a singleton, ready for constructor injection.
//
// Usage in any service or controller:
//   public class ReportService(IUnioExtractor extractor)
//   {
//       public async Task<List<Invoice>> LoadAsync(Stream file)
//           => await extractor.ExtractAsync<Invoice>(file).ToListAsync();
//   }
builder.Services.AddUnio(config => {
	config.RegisterExtractor<JsonExtractor>();
	config.RegisterExtractor<XmlExtractor>();
	config.RegisterExtractor<XlsxExtractor>();
	config.RegisterExtractor<PdfTableExtractor>();
	config.OnError = ErrorHandling.CollectAndContinue;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Create DB on startup ---
using (var scope = app.Services.CreateScope()) {
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment()) {
	app.MapOpenApi();
	app.MapScalarApiReference(options => {
		options.WithTitle("Unio Sample API")
			.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
	});
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

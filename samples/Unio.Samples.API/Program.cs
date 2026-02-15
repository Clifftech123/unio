using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Unio.Excel;
using Unio.Json;
using Unio.Pdf;
using Unio.Samples.API.Data;
using Unio.Xml;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + SQLite ---
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite("Data Source=unio_samples.db"));

// --- Unio (register all format extractors) ---
builder.Services.AddSingleton(_ => {
	var unio = new Unio.Unio();
	unio.RegisterExtractor(new JsonExtractor());
	unio.RegisterExtractor(new XmlExtractor());
	unio.RegisterExtractor(new XlsxExtractor());
	unio.RegisterExtractor(new PdfTableExtractor());
	return unio;
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

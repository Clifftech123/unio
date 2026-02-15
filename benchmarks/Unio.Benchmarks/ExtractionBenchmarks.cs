using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Unio.Excel;
using Unio.Json;
using Unio.Pdf;
using Unio.Xml;

namespace Unio.Benchmarks;

/// <summary>
/// Benchmarks Unio extraction across all supported formats.
/// Uses the same employee/product data in CSV, JSON, XML, XLSX, and PDF.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExtractionBenchmarks {
	private static readonly string DataDir = FindDataDir();
	private readonly Unio _unio;

	public ExtractionBenchmarks() {
		_unio = new Unio();
		_unio.RegisterExtractor(new JsonExtractor());
		_unio.RegisterExtractor(new XmlExtractor());
		_unio.RegisterExtractor(new XlsxExtractor());
		_unio.RegisterExtractor(new PdfTableExtractor());
	}

	// ?? Small file: 10 employees ????????????????????????????????

	[Benchmark(Description = "CSV  (10 rows)")]
	public List<Employee> Csv_Employees() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "employees.csv")).ToList();

	[Benchmark(Description = "JSON (10 rows)")]
	public List<Employee> Json_Employees() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "employees.json")).ToList();

	[Benchmark(Description = "XML  (10 rows)")]
	public List<Employee> Xml_Employees() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "employees.xml")).ToList();

	[Benchmark(Description = "XLSX (10 rows)")]
	public List<Employee> Xlsx_Employees() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "employees.xlsx")).ToList();

	[Benchmark(Description = "PDF  (10 rows)")]
	public List<Employee> Pdf_Employees() =>
		_unio.ExtractWithErrors<Employee>(Path.Combine(DataDir, "employees.pdf")).Records.ToList();

	// ?? Large file: 10k rows ????????????????????????????????????

	[Benchmark(Description = "CSV  (10k rows)")]
	public List<Employee> Csv_Large() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "large-10k.csv")).ToList();

	[Benchmark(Description = "XLSX (10k rows)")]
	public List<Employee> Xlsx_Large() =>
		_unio.Extract<Employee>(Path.Combine(DataDir, "large-10k.xlsx")).ToList();

	// ?? Products (8 rows, tests different model) ????????????????

	[Benchmark(Description = "CSV  Products (8 rows)")]
	public List<Product> Csv_Products() =>
		_unio.Extract<Product>(Path.Combine(DataDir, "products.csv")).ToList();

	[Benchmark(Description = "JSON Products (8 rows)")]
	public List<Product> Json_Products() =>
		_unio.Extract<Product>(Path.Combine(DataDir, "products.json")).ToList();

	// ?? Dynamic extraction (no model) ???????????????????????????

	[Benchmark(Description = "CSV  Dynamic (10 rows)")]
	public List<dynamic> Csv_Dynamic() =>
		_unio.Extract(Path.Combine(DataDir, "employees.csv")).ToList();

	[Benchmark(Description = "JSON Dynamic (10 rows)")]
	public List<dynamic> Json_Dynamic() =>
		_unio.Extract(Path.Combine(DataDir, "employees.json")).ToList();

	/// <summary>
	/// Finds the Data folder. It's copied to the output directory via the csproj link.
	/// </summary>
	private static string FindDataDir() {
		// Data files are copied to output via csproj Content link
		var dir = AppContext.BaseDirectory;
		var local = Path.Combine(dir, "Data");
		if (Directory.Exists(local))
			return local;

		// Fallback: walk up to the repo root (for local dev runs)
		for (int i = 0; i < 15; i++) {
			var candidate = Path.Combine(dir, "tests", "Unio.Tests", "Data");
			if (Directory.Exists(candidate))
				return candidate;
			dir = Path.GetDirectoryName(dir)!;
		}
		throw new DirectoryNotFoundException(
			"Could not find benchmark Data folder. " +
			"Ensure the project is built so data files are copied to output.");
	}
}

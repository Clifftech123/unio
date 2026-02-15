using Unio.Excel;
using Unio.Json;
using Unio.Pdf;
using Unio.Samples.ConsoleApp;
using Unio.Xml;

var dataDir = Path.Combine(AppContext.BaseDirectory, "SamplesData");

// Create a Unio instance and register all format extractors
var unio = new Unio.Unio();
unio.RegisterExtractor(new JsonExtractor());
unio.RegisterExtractor(new XmlExtractor());
unio.RegisterExtractor(new XlsxExtractor());
unio.RegisterExtractor(new PdfTableExtractor());

// ── CSV ─────────────────────────────────────────────────────────
PrintSection("CSV — Employees (employees.csv)");
var csvEmployees = unio.Extract<Employee>(Path.Combine(dataDir, "employees.csv"));
PrintEmployees(csvEmployees);

PrintSection("CSV — Invoices (invoices.csv)");
var csvInvoices = unio.Extract<Invoice>(Path.Combine(dataDir, "invoices.csv"));
PrintInvoices(csvInvoices);

PrintSection("CSV — Products (products.csv)");
var csvProducts = unio.Extract<Product>(Path.Combine(dataDir, "products.csv"));
PrintProducts(csvProducts);

// ── JSON ────────────────────────────────────────────────────────
PrintSection("JSON — Employees (employees.json)");
var jsonEmployees = unio.Extract<Employee>(Path.Combine(dataDir, "employees.json"));
PrintEmployees(jsonEmployees);

PrintSection("JSON — Products (products.json)");
var jsonProducts = unio.Extract<Product>(Path.Combine(dataDir, "products.json"));
PrintProducts(jsonProducts);

PrintSection("JSON — Invoices (invoices.json)");
var jsonInvoices = unio.Extract<Invoice>(Path.Combine(dataDir, "invoices.json"));
PrintInvoices(jsonInvoices);

// ── XML ─────────────────────────────────────────────────────────
PrintSection("XML — Employees (employees.xml)");
var xmlEmployees = unio.Extract<Employee>(Path.Combine(dataDir, "employees.xml"));
PrintEmployees(xmlEmployees);

PrintSection("XML — Products (products.xml)");
var xmlProducts = unio.Extract<Product>(Path.Combine(dataDir, "products.xml"));
PrintProducts(xmlProducts);

PrintSection("XML — Invoices (invoices.xml)");
var xmlInvoices = unio.Extract<Invoice>(Path.Combine(dataDir, "invoices.xml"));
PrintInvoices(xmlInvoices);

// ── Excel (XLSX) ────────────────────────────────────────────────
PrintSection("XLSX — Employees (employees.xlsx)");
var xlsxEmployees = unio.Extract<Employee>(Path.Combine(dataDir, "employees.xlsx"));
PrintEmployees(xlsxEmployees);

PrintSection("XLSX — Products (products.xlsx)");
var xlsxProducts = unio.Extract<Product>(Path.Combine(dataDir, "products.xlsx"));
PrintProducts(xlsxProducts);

PrintSection("XLSX — Invoices (invoices.xlsx)");
var xlsxInvoices = unio.Extract<Invoice>(Path.Combine(dataDir, "invoices.xlsx"));
PrintInvoices(xlsxInvoices);

// ── PDF (with error handling — PDFs may have complex layouts) ───
PrintSection("PDF — Employees (employees.pdf)");
var pdfEmpResult = unio.ExtractWithErrors<Employee>(Path.Combine(dataDir, "employees.pdf"));
Console.WriteLine($"  Rows read: {pdfEmpResult.TotalRowsRead} | Mapped: {pdfEmpResult.SuccessCount} | Errors: {pdfEmpResult.ErrorCount}");
PrintEmployees(pdfEmpResult.Records);

PrintSection("PDF — Products (products.pdf)");
var pdfProdResult = unio.ExtractWithErrors<Product>(Path.Combine(dataDir, "products.pdf"));
Console.WriteLine($"  Rows read: {pdfProdResult.TotalRowsRead} | Mapped: {pdfProdResult.SuccessCount} | Errors: {pdfProdResult.ErrorCount}");
PrintProducts(pdfProdResult.Records);

PrintSection("PDF Dynamic — employees.pdf (no model)");
var pdfDynamic = unio.Extract(Path.Combine(dataDir, "employees.pdf"));
foreach (var row in pdfDynamic) {
	var dict = (IDictionary<string, object?>)row;
	Console.WriteLine("  " + string.Join(" | ", dict.Select(kv => $"{kv.Key}={kv.Value}")));
}

// ── Dynamic extraction (no model) ──────────────────────────────
PrintSection("Dynamic — employees.csv (no model)");
var dynamicRows = unio.Extract(Path.Combine(dataDir, "employees.csv"));
foreach (var row in dynamicRows) {
	var dict = (IDictionary<string, object?>)row;
	Console.WriteLine(string.Join(" | ", dict.Select(kv => $"{kv.Key}={kv.Value}")));
}

// ── ExtractWithErrors ───────────────────────────────────────────
PrintSection("ExtractWithErrors — employees.csv");
var result = unio.ExtractWithErrors<Employee>(Path.Combine(dataDir, "employees.csv"));
Console.WriteLine($"  Total rows read : {result.TotalRowsRead}");
Console.WriteLine($"  Success count   : {result.SuccessCount}");
Console.WriteLine($"  Error count     : {result.ErrorCount}");
if (result.HasErrors) {
	foreach (var err in result.Errors)
		Console.WriteLine($"   Row {err.RowNumber}: {err.Message}");
}

// ── Async streaming ─────────────────────────────────────────────
PrintSection("Async Streaming — employees.json");
await foreach (var emp in unio.ExtractAsync<Employee>(Path.Combine(dataDir, "employees.json"))) {
	Console.WriteLine($"  {emp.Id,-4} {emp.Name,-20} {emp.Department,-15} {emp.Salary,10:C}");
}

Console.WriteLine();
Console.WriteLine(" All extractions completed successfully.");

// ── Helper methods ──────────────────────────────────────────────
static void PrintSection(string title) {
	Console.WriteLine();
	Console.WriteLine(new string('═', 60));
	Console.WriteLine($"  {title}");
	Console.WriteLine(new string('─', 60));
}

static void PrintEmployees(IEnumerable<Employee> employees) {
	Console.WriteLine($"  {"Id",-4} {"Name",-20} {"Department",-15} {"Salary",10} {"StartDate",-12} {"Active",-6} {"Email"}");
	Console.WriteLine($"  {new string('-', 4)} {new string('-', 20)} {new string('-', 15)} {new string('-', 10)} {new string('-', 12)} {new string('-', 6)} {new string('-', 25)}");
	foreach (var e in employees) {
		Console.WriteLine($"  {e.Id,-4} {e.Name,-20} {e.Department,-15} {e.Salary,10:N2} {e.StartDate,-12:yyyy-MM-dd} {e.IsActive,-6} {e.Email}");
	}
}

static void PrintInvoices(IEnumerable<Invoice> invoices) {
	Console.WriteLine($"  {"InvoiceNo",-12} {"Customer",-18} {"Date",-12} {"Amount",10} {"Tax",8} {"Total",10} {"Status",-10} {"DueDate",-12}");
	Console.WriteLine($"  {new string('-', 12)} {new string('-', 18)} {new string('-', 12)} {new string('-', 10)} {new string('-', 8)} {new string('-', 10)} {new string('-', 10)} {new string('-', 12)}");
	foreach (var i in invoices) {
		Console.WriteLine($"  {i.InvoiceNo,-12} {i.Customer,-18} {i.Date,-12:yyyy-MM-dd} {i.Amount,10:N2} {i.Tax,8:N2} {i.Total,10:N2} {i.Status,-10} {i.DueDate,-12:yyyy-MM-dd}");
	}
}

static void PrintProducts(IEnumerable<Product> products) {
	Console.WriteLine($"  {"ProductId",-12} {"Name",-20} {"Category",-15} {"Price",8} {"Qty",5} {"InStock",-7} {"Rating",6}");
	Console.WriteLine($"  {new string('-', 12)} {new string('-', 20)} {new string('-', 15)} {new string('-', 8)} {new string('-', 5)} {new string('-', 7)} {new string('-', 6)}");
	foreach (var p in products) {
		Console.WriteLine($"  {p.ProductId,-12} {p.Name,-20} {p.Category,-15} {p.Price,8:N2} {p.Quantity,5} {p.InStock,-7} {p.Rating,6:F1}");
	}
}

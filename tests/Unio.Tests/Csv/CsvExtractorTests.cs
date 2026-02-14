using FluentAssertions;
using Unio.Csv;
using Unio.Abstractions;
using Unio.Tests.Models;

namespace Unio.Tests.Csv;

public class CsvExtractorTests {
	private readonly CsvExtractor _extractor = new();

	// --- CanHandle ---

	[Theory]
	[InlineData(".csv", true)]
	[InlineData(".tsv", true)]
	[InlineData(".CSV", true)]
	[InlineData(".xlsx", false)]
	[InlineData(".json", false)]
	public void CanHandle_Extension(string ext, bool expected) {
		_extractor.CanHandle(Stream.Null, ext).Should().Be(expected);
	}

	[Fact]
	public void CanHandle_NullExtension_ReturnsFalse() {
		_extractor.CanHandle(Stream.Null).Should().BeFalse();
	}

	// --- Extract<T> typed ---

	[Fact]
	public void Extract_Employees_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.csv");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().HaveCount(10);
		results[0].Id.Should().Be(1);
		results[0].Name.Should().Be("Kwame Asante");
		results[0].Department.Should().Be("Engineering");
		results[0].Salary.Should().Be(85000.5m);
		results[0].StartDate.Should().Be(new DateTime(2021, 3, 15));
		results[0].IsActive.Should().BeTrue();
		results[0].Email.Should().Be("kwame@unio.dev");
	}

	[Fact]
	public void Extract_Invoices_ReturnsCorrectValues() {
		using var stream = DataPath.OpenRead("invoices.csv");
		var results = _extractor.Extract<Invoice>(stream).ToList();

		results.Should().HaveCount(5);
		results[0].InvoiceNo.Should().Be("INV-001");
		results[0].Customer.Should().Be("Accra Tech Ltd");
		results[0].Amount.Should().Be(12500.0m);
		results[0].Total.Should().Be(14062.5m);
		results[0].Status.Should().Be("Paid");
	}

	[Fact]
	public void Extract_Products_ParsesAllTypes() {
		using var stream = DataPath.OpenRead("products.csv");
		var results = _extractor.Extract<Product>(stream).ToList();

		results.Should().HaveCount(8);
		results[0].ProductId.Should().Be("PRD-001");
		results[0].Price.Should().Be(49.99m);
		results[0].Quantity.Should().Be(150);
		results[0].InStock.Should().BeTrue();
		results[0].Rating.Should().BeApproximately(4.5, 0.01);
		results[3].InStock.Should().BeFalse();
		results[3].Quantity.Should().Be(0);
	}

	// --- Dynamic ---

	[Fact]
	public void Extract_Dynamic_ReturnsCorrectRows() {
		using var stream = DataPath.OpenRead("employees.csv");
		var results = _extractor.Extract(stream).ToList();

		results.Should().HaveCount(10);
		string name = results[0].Name;
		name.Should().Be("Kwame Asante");
		string dept = results[2].Department;
		dept.Should().Be("Engineering");
	}

	// --- MaxRows ---

	[Fact]
	public void Extract_WithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("employees.csv");
		var options = new ExtractionOptions { MaxRows = 3 };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(3);
	}

	[Fact]
	public void Extract_DynamicWithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("invoices.csv");
		var options = new ExtractionOptions { MaxRows = 2 };
		var results = _extractor.Extract(stream, options).ToList();

		results.Should().HaveCount(2);
	}

	// --- StartRow ---

	[Fact]
	public void Extract_WithStartRow_SkipsRows() {
		using var stream = DataPath.OpenRead("employees.csv");
		// StartRow=1 skips the first raw row (the original header line),
		// then reads the next row as the new header.
		var options = new ExtractionOptions { StartRow = 1 };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(9);
	}

	// --- No header ---

	[Fact]
	public void Extract_NoHeaderRow_MapsByIndex() {
		using var stream = DataPath.OpenRead("employees-no-header.csv");
		var options = new ExtractionOptions { HasHeaderRow = false };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(10);
		results[0].Id.Should().Be(1);
		results[0].Name.Should().Be("Kwame Asante");
		results[0].Department.Should().Be("Engineering");
		results[0].Salary.Should().Be(85000.5m);
		results[0].IsActive.Should().BeTrue();
	}

	// --- TSV ---

	[Fact]
	public void Extract_TsvFile_WithTabDelimiter() {
		using var stream = DataPath.OpenRead("employees.tsv");
		var options = new ExtractionOptions { Delimiter = '\t' };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(10);
		results[0].Name.Should().Be("Kwame Asante");
		results[0].Salary.Should().Be(85000.5m);
		}

	// --- Semicolon delimiter ---

	[Fact]
	public void Extract_SemicolonDelimiter_ParsesCorrectly() {
		using var stream = DataPath.OpenRead("invoices-semicolon.csv");
		var options = new ExtractionOptions { Delimiter = ';' };
		var results = _extractor.Extract<Invoice>(stream, options).ToList();

		results.Should().HaveCount(5);
		results[0].InvoiceNo.Should().Be("INV-001");
		results[0].Amount.Should().Be(12500.0m);
	}

	// --- Edge cases ---

	[Fact]
	public void Extract_EdgeCases_HandlesMultilineAndSpecialChars() {
		using var stream = DataPath.OpenRead("edge-cases.csv");
		var results = _extractor.Extract<EdgeCaseRecord>(stream).ToList();

		results.Should().HaveCount(5);
		results[0].Name.Should().Be("O'Brien, James");
		results[0].Notes.Should().Contain("hello, world");
		results[0].Value.Should().Be(0m);
		// Row 2 has a multiline field "Line1\nLine2"
		results[1].Name.Should().Be("José García");
		results[1].Notes.Should().Contain("Line1\nLine2");
		results[1].Value.Should().Be(-99.5m);
		// Row 3 has unicode
		results[2].Notes.Should().Contain("Unicode");
	}

	// --- Empty file ---

	[Fact]
	public void Extract_EmptyFile_ReturnsEmpty() {
		using var stream = DataPath.OpenRead("empty.csv");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().BeEmpty();
	}

	// --- Single row ---

	[Fact]
	public void Extract_SingleRow_ReturnsOneRecord() {
		using var stream = DataPath.OpenRead("single-row.csv");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().HaveCount(1);
		results[0].Name.Should().Be("Kwame Asante");
	}

	// --- Ignore attribute ---

	[Fact]
	public void Extract_IgnoredProperty_KeepsDefaultValue() {
		using var stream = DataPath.OpenRead("employees.csv");
		var results = _extractor.Extract<EmployeeWithIgnored>(stream).ToList();

		results.Should().NotBeEmpty();
		results[0].Computed.Should().Be("default-value");
		results[0].Name.Should().Be("Kwame Asante");
	}

	// --- Async ---

	[Fact]
	public async Task ExtractAsync_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.csv");
		var results = new List<Employee>();

		await foreach (var record in _extractor.ExtractAsync<Employee>(stream)) {
			results.Add(record);
		}

		results.Should().HaveCount(10);
		results[0].Name.Should().Be("Kwame Asante");
	}

	[Fact]
	public async Task ExtractAsync_WithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("employees.csv");
		var options = new ExtractionOptions { MaxRows = 2 };
		var results = new List<Employee>();

		await foreach (var record in _extractor.ExtractAsync<Employee>(stream, options)) {
			results.Add(record);
		}

		results.Should().HaveCount(2);
	}

	[Fact]
	public async Task ExtractAsync_SupportsCancellation() {
		using var stream = DataPath.OpenRead("employees.csv");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => {
			await foreach (var _ in _extractor.ExtractAsync<Employee>(stream, cancellationToken: cts.Token)) { }
		};

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// --- ParseLine ---

	[Fact]
	public void ParseLine_SimpleFields() {
		CsvExtractor.ParseLine("a,b,c", ',').Should().Equal("a", "b", "c");
	}

	[Fact]
	public void ParseLine_QuotedFieldWithComma() {
		CsvExtractor.ParseLine("\"hello, world\",b", ',').Should().Equal("hello, world", "b");
	}

	[Fact]
	public void ParseLine_EscapedQuotes() {
		CsvExtractor.ParseLine("\"say \"\"hi\"\"\",b", ',').Should().Equal("say \"hi\"", "b");
	}

	[Fact]
	public void ParseLine_EmptyFields() {
		CsvExtractor.ParseLine(",,", ',').Should().Equal("", "", "");
	}

	[Fact]
	public void ParseLine_TabDelimiter() {
		CsvExtractor.ParseLine("a\tb\tc", '\t').Should().Equal("a", "b", "c");
	}

	[Fact]
	public void ParseLine_SemicolonDelimiter() {
		CsvExtractor.ParseLine("a;b;c", ';').Should().Equal("a", "b", "c");
	}
}

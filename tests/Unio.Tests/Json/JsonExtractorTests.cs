using FluentAssertions;
using Unio.Json;
using Unio.Abstractions;
using Unio.Tests.Models;

namespace Unio.Tests.Json;

public class JsonExtractorTests {
	private readonly JsonExtractor _extractor = new();

	// --- CanHandle ---

	[Theory]
	[InlineData(".json", true)]
	[InlineData(".JSON", true)]
	[InlineData(".csv", false)]
	[InlineData(".xml", false)]
	public void CanHandle_Extension(string ext, bool expected) {
		_extractor.CanHandle(Stream.Null, ext).Should().Be(expected);
	}

	[Fact]
	public void CanHandle_StreamStartingWithBracket_ReturnsTrue() {
		var bytes = System.Text.Encoding.UTF8.GetBytes("[{\"a\":1}]");
		using var stream = new MemoryStream(bytes);
		_extractor.CanHandle(stream).Should().BeTrue();
	}

	// --- Extract<T> ---

	[Fact]
	public void Extract_Employees_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.json");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().HaveCount(10);
		results[0].Id.Should().Be(1);
		results[0].Name.Should().Be("Kwame Asante");
		results[0].Department.Should().Be("Engineering");
		results[0].Salary.Should().Be(85000.5m);
		results[0].IsActive.Should().BeTrue();
	}

	[Fact]
	public void Extract_Invoices_ReturnsCorrectValues() {
		using var stream = DataPath.OpenRead("invoices.json");
		var results = _extractor.Extract<Invoice>(stream).ToList();

		results.Should().HaveCount(5);
		results[0].InvoiceNo.Should().Be("INV-001");
		results[0].Amount.Should().Be(12500.0m);
		results[0].Status.Should().Be("Paid");
	}

	[Fact]
	public void Extract_Products_ParsesAllTypes() {
		using var stream = DataPath.OpenRead("products.json");
		var results = _extractor.Extract<Product>(stream).ToList();

		results.Should().HaveCount(8);
		results[0].ProductId.Should().Be("PRD-001");
		results[0].Price.Should().Be(49.99m);
		results[0].InStock.Should().BeTrue();
	}

	// --- Dynamic ---

	[Fact]
	public void Extract_Dynamic_ReturnsCorrectRows() {
		using var stream = DataPath.OpenRead("employees.json");
		var results = _extractor.Extract(stream).ToList();

		results.Should().HaveCount(10);
		string name = results[0].Name;
		name.Should().Be("Kwame Asante");
	}

	// --- MaxRows ---

	[Fact]
	public void Extract_WithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("employees.json");
		var options = new ExtractionOptions { MaxRows = 3 };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(3);
	}

	// --- Empty ---

	[Fact]
	public void Extract_EmptyArray_ReturnsEmpty() {
		using var stream = DataPath.OpenRead("empty.json");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().BeEmpty();
	}

	// --- Single item ---

	[Fact]
	public void Extract_SingleItem_ReturnsOneRecord() {
		using var stream = DataPath.OpenRead("single-item.json");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().HaveCount(1);
		results[0].Name.Should().Be("Kwame Asante");
	}

	// --- Edge cases ---

	[Fact]
	public void Extract_EdgeCases_HandlesSpecialChars() {
		using var stream = DataPath.OpenRead("edge-cases.json");
		var results = _extractor.Extract<EdgeCaseRecord>(stream).ToList();

		results.Should().HaveCountGreaterThanOrEqualTo(3);
		results[0].Name.Should().Be("O'Brien, James");
		results[1].Name.Should().Be("José García");
	}

	// --- Async ---

	[Fact]
	public async Task ExtractAsync_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.json");
		var results = new List<Employee>();

		await foreach (var record in _extractor.ExtractAsync<Employee>(stream)) {
			results.Add(record);
		}

		results.Should().HaveCount(10);
		}
		}

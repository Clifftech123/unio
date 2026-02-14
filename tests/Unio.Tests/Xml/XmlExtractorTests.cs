using FluentAssertions;
using Unio.Xml;
using Unio.Abstractions;
using Unio.Tests.Models;

namespace Unio.Tests.Xml;

public class XmlExtractorTests {
	private readonly XmlExtractor _extractor = new();

	// --- CanHandle ---

	[Theory]
	[InlineData(".xml", true)]
	[InlineData(".XML", true)]
	[InlineData(".csv", false)]
	[InlineData(".json", false)]
	public void CanHandle_Extension(string ext, bool expected) {
		_extractor.CanHandle(Stream.Null, ext).Should().Be(expected);
	}

	[Fact]
	public void CanHandle_StreamStartingWithAngleBracket_ReturnsTrue() {
		var bytes = System.Text.Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><r/>");
		using var stream = new MemoryStream(bytes);
		_extractor.CanHandle(stream).Should().BeTrue();
	}

	// --- Extract<T> ---

	[Fact]
	public void Extract_Employees_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.xml");
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
		using var stream = DataPath.OpenRead("invoices.xml");
		var results = _extractor.Extract<Invoice>(stream).ToList();

		results.Should().HaveCount(5);
		results[0].InvoiceNo.Should().Be("INV-001");
		results[0].Amount.Should().Be(12500.0m);
		results[0].Status.Should().Be("Paid");
	}

	// --- Dynamic ---

	[Fact]
	public void Extract_Dynamic_ReturnsCorrectRows() {
		using var stream = DataPath.OpenRead("employees.xml");
		var results = _extractor.Extract(stream).ToList();

		results.Should().HaveCount(10);
		string name = results[0].Name;
		name.Should().Be("Kwame Asante");
	}

	// --- MaxRows ---

	[Fact]
	public void Extract_WithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("employees.xml");
		var options = new ExtractionOptions { MaxRows = 4 };
		var results = _extractor.Extract<Employee>(stream, options).ToList();

		results.Should().HaveCount(4);
	}

	// --- Attributes XML ---

	[Fact]
	public void Extract_AttributeBasedXml_ReturnsCorrectRecords() {
		using var stream = DataPath.OpenRead("employees-attributes.xml");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().HaveCount(10);
		results[0].Name.Should().Be("Kwame Asante");
		results[0].Salary.Should().Be(85000.5m);
	}

	// --- Empty ---

	[Fact]
	public void Extract_EmptyXml_ReturnsEmpty() {
		using var stream = DataPath.OpenRead("empty.xml");
		var results = _extractor.Extract<Employee>(stream).ToList();

		results.Should().BeEmpty();
	}

	// --- Async ---

	[Fact]
	public async Task ExtractAsync_ReturnsAllRecords() {
		using var stream = DataPath.OpenRead("employees.xml");
		var results = new List<Employee>();

		await foreach (var record in _extractor.ExtractAsync<Employee>(stream)) {
			results.Add(record);
		}

		results.Should().HaveCount(10);
		}
		}

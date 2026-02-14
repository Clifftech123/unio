using FluentAssertions;
using Unio.Abstractions;
using Unio.Csv;
using Unio.Json;
using Unio.Xml;
using Unio.Tests.Models;

namespace Unio.Tests.Core;

public class UnioTests {

	// --- Standalone usage ---

	[Fact]
	public void Extract_CsvFile_ReturnsRecords() {
		var unio = new Unio();
		var results = unio.Extract<Employee>(DataPath.Get("employees.csv"));

		results.Should().HaveCount(10);
		results.First().Name.Should().Be("Kwame Asante");
	}

	[Fact]
	public void Extract_CsvStream_ReturnsRecords() {
		var unio = new Unio();
		using var stream = DataPath.OpenRead("employees.csv");
		var results = unio.Extract<Employee>(stream, ".csv").ToList();

		results.Should().HaveCount(10);
	}

	[Fact]
	public void Extract_Dynamic_CsvFile_ReturnsRows() {
		var unio = new Unio();
		var results = unio.Extract(DataPath.Get("employees.csv"));

		results.Should().HaveCount(10);
		string name = results.First().Name;
		name.Should().Be("Kwame Asante");
	}

	[Fact]
	public void Extract_WithOptions_RespectsMaxRows() {
		var unio = new Unio();
		var results = unio.Extract<Employee>(DataPath.Get("employees.csv"), opt => opt.MaxRows = 3);

		results.Should().HaveCount(3);
	}

	// --- RegisterExtractor ---

	[Fact]
	public void RegisterExtractor_Json_WorksAfterRegistration() {
		var unio = new Unio();
		unio.RegisterExtractor(new JsonExtractor());

		using var stream = DataPath.OpenRead("employees.json");
		var results = unio.Extract<Employee>(stream, ".json").ToList();

		results.Should().HaveCount(10);
	}

	[Fact]
	public void RegisterExtractor_Xml_WorksAfterRegistration() {
		var unio = new Unio();
		unio.RegisterExtractor(new XmlExtractor());

		using var stream = DataPath.OpenRead("employees.xml");
		var results = unio.Extract<Employee>(stream, ".xml").ToList();

		results.Should().HaveCount(10);
	}

	// --- Configuration ---

	[Fact]
	public void Constructor_WithConfig_RegistersExtractors() {
		var config = new UnioConfiguration();
		config.RegisterExtractor<JsonExtractor>();
		config.RegisterExtractor<XmlExtractor>();

		var unio = new Unio(config);

		using var jsonStream = DataPath.OpenRead("employees.json");
		unio.Extract<Employee>(jsonStream, ".json").ToList().Should().HaveCount(10);
	}

	// --- Unsupported format ---

	[Fact]
	public void Extract_UnsupportedFormat_ThrowsUnioFormatException() {
		var unio = new Unio();
		using var stream = new MemoryStream();

		var act = () => unio.Extract<Employee>(stream, ".xyz").ToList();

		act.Should().Throw<UnioFormatException>()
			.WithMessage("*No extractor registered*");
	}

	// --- ExtractWithErrors ---

	[Fact]
	public void ExtractWithErrors_CsvFile_ReturnsResult() {
		var unio = new Unio();
		var result = unio.ExtractWithErrors<Employee>(DataPath.Get("employees.csv"));

		result.Records.Should().HaveCount(10);
		result.TotalRowsRead.Should().Be(10);
		result.SuccessCount.Should().Be(10);
		result.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void ExtractWithErrors_WithValidator_CollectsErrors() {
		var unio = new Unio();
		var result = unio.ExtractWithErrors<Employee>(DataPath.Get("employees.csv"), opt => {
			opt.RecordValidator = record => {
				var emp = (Employee)record;
				var errors = new List<string>();
				if (emp.Salary > 100_000m)
					errors.Add($"Salary too high: {emp.Salary}");
				return errors;
			};
		});

		// Nana Osei has Salary=105000.0 — should be flagged
		result.HasErrors.Should().BeTrue();
		result.Errors.Should().NotBeEmpty();
		result.Records.Should().HaveCount(9);
	}

	// --- Async ---

	[Fact]
	public async Task ExtractAsync_CsvFile_ReturnsAllRecords() {
		var unio = new Unio();
		var results = new List<Employee>();

		await foreach (var record in unio.ExtractAsync<Employee>(DataPath.Get("employees.csv"))) {
			results.Add(record);
		}

		results.Should().HaveCount(10);
	}

	[Fact]
	public async Task ExtractAsync_Stream_ReturnsAllRecords() {
		var unio = new Unio();
		using var stream = DataPath.OpenRead("employees.csv");
		var results = new List<Employee>();

		await foreach (var record in unio.ExtractAsync<Employee>(stream, ".csv")) {
			results.Add(record);
		}

		results.Should().HaveCount(10);
		}
		}

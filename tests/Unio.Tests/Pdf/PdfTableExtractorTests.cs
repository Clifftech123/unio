using FluentAssertions;
using Unio.Pdf;
using Unio.Abstractions;
using Unio.Tests.Models;
using Unio.Mapping;

namespace Unio.Tests.Pdf;

public class PdfTableExtractorTests {
	private readonly PdfTableExtractor _extractor = new();

	// --- CanHandle ---

	[Theory]
	[InlineData(".pdf", true)]
	[InlineData(".PDF", true)]
	[InlineData(".csv", false)]
	[InlineData(".xlsx", false)]
	public void CanHandle_Extension(string ext, bool expected) {
		_extractor.CanHandle(Stream.Null, ext).Should().Be(expected);
	}

	[Fact]
	public void CanHandle_NullExtension_ReturnsFalse() {
		_extractor.CanHandle(Stream.Null).Should().BeFalse();
	}

	[Fact]
	public void CanHandle_PdfMagicBytes_ReturnsTrue() {
		var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
		using var stream = new MemoryStream(bytes);
		_extractor.CanHandle(stream).Should().BeTrue();
	}

	[Fact]
	public void CanHandle_NonPdfBytes_ReturnsFalse() {
		var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP
		using var stream = new MemoryStream(bytes);
		_extractor.CanHandle(stream).Should().BeFalse();
	}

	[Fact]
	public void CanHandle_PdfMagicBytes_ResetsStreamPosition() {
		var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
		using var stream = new MemoryStream(bytes);
		_extractor.CanHandle(stream);
		stream.Position.Should().Be(0);
	}

	// --- Extract employees.pdf (dynamic only � complex multi-table layout) ---

	[Fact]
	public void Extract_EmployeesPdf_Dynamic_ReturnsRows() {
		using var stream = DataPath.OpenRead("employees.pdf");
		var results = _extractor.Extract(stream).ToList();

		results.Should().NotBeEmpty();
	}

	// --- Extract products.pdf (clean single-table layout) ---

	[Fact]
	public void Extract_ProductsPdf_ReturnsRecords() {
		using var stream = DataPath.OpenRead("products.pdf");
		var results = _extractor.Extract<Product>(stream).ToList();

		results.Should().NotBeEmpty();
	}

	[Fact]
	public void Extract_ProductsPdf_Dynamic_ReturnsRows() {
		using var stream = DataPath.OpenRead("products.pdf");
		var results = _extractor.Extract(stream).ToList();

		results.Should().NotBeEmpty();
	}

	[Fact]
	public void Extract_ProductsPdf_WithMaxRows_LimitsOutput() {
		using var stream = DataPath.OpenRead("products.pdf");
		var options = new ExtractionOptions { MaxRows = 2 };
		var results = _extractor.Extract<Product>(stream, options).ToList();

		results.Should().HaveCount(2);
	}

	// --- Plain text fallback ---

	[Fact]
	public void Extract_PlainTextPdf_NoHeader_ReturnsTextLines() {
		using var stream = DataPath.OpenRead("employees.pdf");
		var options = new ExtractionOptions { HasHeaderRow = false };
		var results = _extractor.Extract(stream, options).ToList();

		results.Should().NotBeEmpty();
	}

	// --- Async ---

	[Fact]
	public async Task ExtractAsync_ProductsPdf_ReturnsRecords() {
		using var stream = DataPath.OpenRead("products.pdf");
		var results = new List<Product>();

		await foreach (var record in _extractor.ExtractAsync<Product>(stream)) {
			results.Add(record);
		}

		results.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ExtractAsync_SupportsCancellation() {
		using var stream = DataPath.OpenRead("employees.pdf");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => {
			await foreach (var _ in _extractor.ExtractAsync<Employee>(stream, cancellationToken: cts.Token)) { }
		};

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// --- Via Unio (integration) ---

	[Fact]
	public void Unio_WithPdfExtractor_ExtractsFromPdf() {
		var unio = new Unio();
		unio.RegisterExtractor(new PdfTableExtractor());

		using var stream = DataPath.OpenRead("products.pdf");
		var results = unio.Extract<Product>(stream, ".pdf").ToList();

		results.Should().NotBeEmpty();
	}

	[Fact]
	public void Unio_WithoutPdfExtractor_ThrowsForPdf() {
		var unio = new Unio();
		using var stream = new MemoryStream();

		var act = () => unio.Extract<Employee>(stream, ".pdf").ToList();

		act.Should().Throw<UnioFormatException>();
	}
}

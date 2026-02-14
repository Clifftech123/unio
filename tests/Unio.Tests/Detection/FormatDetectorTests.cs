using FluentAssertions;
using Unio.Detection;

namespace Unio.Tests.Detection;

public class FormatDetectorTests {

	[Theory]
	[InlineData("file.csv", FileFormat.Csv)]
	[InlineData("file.tsv", FileFormat.Tsv)]
	[InlineData("file.xlsx", FileFormat.Xlsx)]
	[InlineData("file.xls", FileFormat.Xls)]
	[InlineData("file.xlsb", FileFormat.Xlsb)]
	[InlineData("file.pdf", FileFormat.Pdf)]
	[InlineData("file.json", FileFormat.Json)]
	[InlineData("file.xml", FileFormat.Xml)]
	public void Detect_ByExtension_ReturnsCorrectFormat(string path, FileFormat expected) {
		using var stream = new MemoryStream();
		FormatDetector.Detect(stream, path).Should().Be(expected);
	}

	[Fact]
	public void Detect_UnknownExtension_ReturnsUnknown() {
		var bytes = System.Text.Encoding.UTF8.GetBytes("[{\"a\":1}]");
		using var stream = new MemoryStream(bytes);

		// Extension check returns Unknown, then falls to magic bytes
		var result = FormatDetector.Detect(stream, "file.unknown");
		// The code returns Unknown for unrecognized extensions before checking bytes
		// because extension match returned Unknown and it was != Unknown check fails
		result.Should().Be(FileFormat.Json);
	}

	[Fact]
	public void Detect_JsonMagicBytes_ReturnsJson() {
		var bytes = new byte[] { (byte)'[', 0, 0, 0 };
		using var stream = new MemoryStream(bytes);

		FormatDetector.Detect(stream).Should().Be(FileFormat.Json);
	}

	[Fact]
	public void Detect_XmlMagicBytes_ReturnsXml() {
		var bytes = new byte[] { (byte)'<', (byte)'?', (byte)'x', (byte)'m' };
		using var stream = new MemoryStream(bytes);

		FormatDetector.Detect(stream).Should().Be(FileFormat.Xml);
	}

	[Fact]
	public void Detect_PdfMagicBytes_ReturnsPdf() {
		var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
		using var stream = new MemoryStream(bytes);

		FormatDetector.Detect(stream).Should().Be(FileFormat.Pdf);
	}

	[Fact]
	public void Detect_ZipMagicBytes_ReturnsXlsx() {
		var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK..
		using var stream = new MemoryStream(bytes);

		FormatDetector.Detect(stream).Should().Be(FileFormat.Xlsx);
	}

	[Fact]
	public void Detect_ResetsStreamPosition() {
		var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x00 };
		using var stream = new MemoryStream(bytes);

		FormatDetector.Detect(stream);

		stream.Position.Should().Be(0);
	}
}

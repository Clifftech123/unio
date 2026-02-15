

namespace Unio.Detection {

/// <summary>
/// Detects file format by extension and magic bytes.
/// </summary>
public static class FormatDetector {
		// Magic bytes signatures
		private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };  // PK.. (XLSX, XLSB)
		private static readonly byte[] OleSignature = { 0xD0, 0xCF, 0x11, 0xE0 };  // XLS (OLE2)
		private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 };  // %PDF

		/// <summary>Detects the file format from extension and magic bytes.</summary>
		public static FileFormat Detect(Stream stream, string? filePath = null) {
			// Try extension first
			if (filePath is not null) {
				var ext = Path.GetExtension(filePath).ToLowerInvariant();
				var byExt = ext switch {
					".csv" => FileFormat.Csv,
					".tsv" => FileFormat.Tsv,
					".xlsx" => FileFormat.Xlsx,
					".xls" => FileFormat.Xls,
					".xlsb" => FileFormat.Xlsb,
					".pdf" => FileFormat.Pdf,
					".json" => FileFormat.Json,
					".xml" => FileFormat.Xml,
					_ => FileFormat.Unknown,
				};
				if (byExt != FileFormat.Unknown) return byExt;
			}

			// Fall back to magic bytes
			if (!stream.CanSeek) return FileFormat.Unknown;

			var pos = stream.Position;
			var header = new byte[4];
			var bytesRead = stream.Read(header, 0, 4);
			stream.Position = pos; // Reset

			if (bytesRead < 4) return FileFormat.Csv; // Assume CSV for tiny files

			if (StartsWith(header, PdfSignature)) return FileFormat.Pdf;
			if (StartsWith(header, ZipSignature)) return FileFormat.Xlsx; // Could be XLSB too
			if (StartsWith(header, OleSignature)) return FileFormat.Xls;
			if (header[0] == '{' || header[0] == '[') return FileFormat.Json;
			if (header[0] == '<') return FileFormat.Xml;

			return FileFormat.Csv; // Default fallback
		}

		private static bool StartsWith(byte[] data, byte[] prefix) {
			for (int i = 0; i < prefix.Length; i++) {
				if (i >= data.Length || data[i] != prefix[i]) return false;
			}
			return true;
		}
	}


	/// <summary>Supported file formats that Unio can detect and extract.</summary>
	public enum FileFormat {
		/// <summary>Format could not be determined.</summary>
		Unknown,
		/// <summary>Comma-separated values.</summary>
		Csv,
		/// <summary>Tab-separated values.</summary>
		Tsv,
		/// <summary>Excel Open XML spreadsheet (.xlsx).</summary>
		Xlsx,
		/// <summary>Legacy Excel binary format (.xls).</summary>
		Xls,
		/// <summary>Excel binary spreadsheet (.xlsb).</summary>
		Xlsb,
		/// <summary>Portable Document Format (.pdf).</summary>
		Pdf,
		/// <summary>JavaScript Object Notation (.json).</summary>
		Json,
		/// <summary>Extensible Markup Language (.xml).</summary>
		Xml
	}
}
using System.Runtime.CompilerServices;
using System.Text;
using Unio.Abstractions;
using Unio.Mapping;

namespace Unio.Csv;

/// <summary>
/// High-performance CSV extractor with streaming support.
/// RFC 4180 compliant — handles multiline quoted fields.
/// Zero external dependencies.
/// </summary>
public class CsvExtractor : IDataExtractor {
	public IReadOnlyList<string> SupportedExtensions => new[] { ".csv", ".tsv" };

	public bool CanHandle(Stream stream, string? fileExtension = null) {
		if (fileExtension is not null) {
			var ext = fileExtension.ToLowerInvariant();
			return ext is ".csv" or ".tsv";
		}
		return false;
	}

	public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
		options ??= new ExtractionOptions();
		var delimiter = ResolveDelimiter(options);

		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
			bufferSize: 8192, leaveOpen: true);

		string[]? headers = null;
		TypeMapper<T>? mapper = null;
		int rowIndex = 0;
		int dataRowIndex = 0;

		while (ReadLogicalLine(reader) is { } line) {
			if (rowIndex < options.StartRow) { rowIndex++; continue; }

			var fields = ParseLine(line, delimiter);

			if (options.HasHeaderRow && headers is null) {
				headers = fields;
				mapper = new TypeMapper<T>(headers, options.Culture);
				rowIndex++;
				continue;
			}

			mapper ??= new TypeMapper<T>(null, options.Culture);

			if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
				yield break;

			var values = fields.Cast<object?>().ToArray();
			yield return mapper.Map(values);
			dataRowIndex++;
			rowIndex++;
		}
	}

	public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
		options ??= new ExtractionOptions();
		var delimiter = ResolveDelimiter(options);

		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
			bufferSize: 8192, leaveOpen: true);

		string[]? headers = null;
		int rowIndex = 0;
		int dataRowIndex = 0;

		while (ReadLogicalLine(reader) is { } line) {
			if (rowIndex < options.StartRow) { rowIndex++; continue; }

			var fields = ParseLine(line, delimiter);

			if (options.HasHeaderRow && headers is null) {
				headers = fields;
				rowIndex++;
				continue;
			}

			if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
				yield break;

			var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
			for (int i = 0; i < fields.Length; i++) {
				var key = headers is not null && i < headers.Length
					? headers[i].Trim()
					: $"Column{i}";
				expando[key] = fields[i];
			}

			yield return expando;
			dataRowIndex++;
			rowIndex++;
		}
	}

	public async IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new() {
		options ??= new ExtractionOptions();
		var delimiter = ResolveDelimiter(options);

		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
			bufferSize: 8192, leaveOpen: true);

		string[]? headers = null;
		TypeMapper<T>? mapper = null;
		int rowIndex = 0;
		int dataRowIndex = 0;

		while (await ReadLogicalLineAsync(reader, cancellationToken) is { } line) {
			cancellationToken.ThrowIfCancellationRequested();

			if (rowIndex < options.StartRow) { rowIndex++; continue; }

			var fields = ParseLine(line, delimiter);

			if (options.HasHeaderRow && headers is null) {
				headers = fields;
				mapper = new TypeMapper<T>(headers, options.Culture);
				rowIndex++;
				continue;
			}

			mapper ??= new TypeMapper<T>(null, options.Culture);

			if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
				yield break;

			var values = fields.Cast<object?>().ToArray();
			yield return mapper.Map(values);
			dataRowIndex++;
			rowIndex++;
		}
	}

	/// <summary>
	/// Reads a complete logical CSV row, joining multiple physical lines
	/// when a quoted field contains embedded newlines (RFC 4180).
	/// </summary>
	private static string? ReadLogicalLine(StreamReader reader) {
		var first = reader.ReadLine();
		if (first is null) return null;

		int quoteCount = 0;
		foreach (char c in first) {
			if (c == '"') quoteCount++;
		}
		if (quoteCount % 2 == 0) return first;

		var sb = new StringBuilder(first);
		while (true) {
			var next = reader.ReadLine();
			if (next is null) break;
			sb.Append('\n').Append(next);
			foreach (char c in next) {
				if (c == '"') quoteCount++;
			}
			if (quoteCount % 2 == 0) break;
		}
		return sb.ToString();
	}

	/// <summary>
	/// Async version of ReadLogicalLine.
	/// </summary>
	private static async Task<string?> ReadLogicalLineAsync(StreamReader reader, CancellationToken ct) {
		var first = await reader.ReadLineAsync(ct);
		if (first is null) return null;

		int quoteCount = 0;
		foreach (char c in first) {
			if (c == '"') quoteCount++;
		}
		if (quoteCount % 2 == 0) return first;

		var sb = new StringBuilder(first);
		while (true) {
			var next = await reader.ReadLineAsync(ct);
			if (next is null) break;
			sb.Append('\n').Append(next);
			foreach (char c in next) {
				if (c == '"') quoteCount++;
			}
			if (quoteCount % 2 == 0) break;
		}
		return sb.ToString();
	}

	private static char ResolveDelimiter(ExtractionOptions options) {
		return options.Delimiter ?? ',';
	}

	internal static char SniffDelimiter(Stream stream) {
		if (!stream.CanSeek) return ',';
		var pos = stream.Position;
		using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
		var firstLine = reader.ReadLine();
		stream.Position = pos;
		if (firstLine is null) return ',';
		int tabs = firstLine.Count(c => c == '\t');
		int commas = firstLine.Count(c => c == ',');
		return tabs > commas ? '\t' : ',';
	}

	/// <summary>
	/// RFC 4180 compliant CSV line parser. Handles quoted fields, escaped quotes,
	/// embedded delimiters, and embedded newlines.
	/// </summary>
	internal static string[] ParseLine(string line, char delimiter) {
		var fields = new List<string>();
		var field = new StringBuilder();
		bool inQuotes = false;
		int i = 0;

		while (i < line.Length) {
			char c = line[i];

			if (inQuotes) {
				if (c == '"') {
					if (i + 1 < line.Length && line[i + 1] == '"') {
						field.Append('"');
						i += 2;
						continue;
					} else {
						inQuotes = false;
					}
				} else {
					field.Append(c);
				}
			} else {
				if (c == '"') {
					inQuotes = true;
				} else if (c == delimiter) {
					fields.Add(field.ToString());
					field.Clear();
				} else {
					field.Append(c);
				}
			}

			i++;
		}

		fields.Add(field.ToString());
		return fields.ToArray();
	}
}

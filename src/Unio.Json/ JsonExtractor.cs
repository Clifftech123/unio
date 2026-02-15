using System.Runtime.CompilerServices;
using System.Text.Json;
using Unio.Abstractions;
using Unio.Mapping;

namespace Unio.Json {

	/// <summary>
	/// Extracts data from JSON files.
	/// Supports JSON arrays of objects, e.g. [{"Name":"Alice","Age":30}, ...].
	/// Uses System.Text.Json for high-performance parsing.
	/// </summary>
	public class JsonExtractor : IDataExtractor {
		/// <inheritdoc/>
		public IReadOnlyList<string> SupportedExtensions => new[] { ".json" };

		/// <inheritdoc/>
		public bool CanHandle(Stream stream, string? fileExtension = null) {
			if (fileExtension is not null) {
				return fileExtension.ToLowerInvariant() is ".json";
			}

			// Check for JSON array/object start byte
			if (!stream.CanSeek) return false;
			var pos = stream.Position;
			int firstByte;
			do {
				firstByte = stream.ReadByte();
			} while (firstByte is ' ' or '\t' or '\r' or '\n');
			stream.Position = pos;
			return firstByte is '{' or '[';
		}

		/// <inheritdoc/>
		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			options ??= new ExtractionOptions();

			var elements = ReadJsonElements(stream);

			// Build headers from the first element's property names
			string[]? headers = null;
			TypeMapper<T>? mapper = null;
			int dataRowIndex = 0;

			foreach (var element in elements) {
				if (element.ValueKind != JsonValueKind.Object)
					continue;

				if (headers is null) {
					headers = element.EnumerateObject().Select(p => p.Name).ToArray();
					mapper = new TypeMapper<T>(headers, options.Culture);
				}

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				var values = ExtractValues(element, headers);
				yield return mapper!.Map(values);
				dataRowIndex++;
			}
		}

		/// <inheritdoc/>
		public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
			options ??= new ExtractionOptions();

			var elements = ReadJsonElements(stream);
			int dataRowIndex = 0;

			foreach (var element in elements) {
				if (element.ValueKind != JsonValueKind.Object)
					continue;

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
				foreach (var prop in element.EnumerateObject()) {
					expando[prop.Name] = GetJsonValue(prop.Value);
				}

				yield return expando;
				dataRowIndex++;
			}
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new() {
			await Task.Yield();

			foreach (var record in Extract<T>(stream, options)) {
				cancellationToken.ThrowIfCancellationRequested();
				yield return record;
			}
		}

		/// <summary>
		/// Reads the stream as a JSON document and returns the array elements.
		/// Supports both a top-level array and a top-level object wrapping an array.
		/// </summary>
		private static List<JsonElement> ReadJsonElements(Stream stream) {
			using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions {
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip,
			});

			var root = doc.RootElement;

			if (root.ValueKind == JsonValueKind.Array) {
				return root.EnumerateArray().Select(e => e.Clone()).ToList();
			}

			if (root.ValueKind == JsonValueKind.Object) {
				// Look for the first array property in the object
				var arrayProp = root.EnumerateObject()
					.FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Array);

				if (arrayProp.Value.ValueKind == JsonValueKind.Array) {
					return arrayProp.Value.EnumerateArray().Select(e => e.Clone()).ToList();
				}

				// Single object � treat as one-row result
				return [root.Clone()];
			}

			return [];
		}

		/// <summary>
		/// Extracts values from a JSON object in the order of the given headers.
		/// </summary>
		private static object?[] ExtractValues(JsonElement element, string[] headers) {
			var values = new object?[headers.Length];
			for (int i = 0; i < headers.Length; i++) {
				if (element.TryGetProperty(headers[i], out var prop)) {
					values[i] = GetJsonValue(prop);
				}
			}
			return values;
		}

		/// <summary>
		/// Converts a JsonElement to a CLR value.
		/// </summary>
		private static object? GetJsonValue(JsonElement element) {
			return element.ValueKind switch {
				JsonValueKind.String => element.GetString(),
				JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				JsonValueKind.Undefined => null,
				_ => element.GetRawText(),
			};
		}
	}
}
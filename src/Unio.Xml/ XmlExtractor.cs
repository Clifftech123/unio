using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Unio.Abstractions;
using Unio.Mapping;

namespace Unio.Xml {

	/// <summary>
	/// Extracts tabular data from XML files.
	/// Detects repeating child elements under the root as rows,
	/// and maps their child element/attribute values as columns.
	/// </summary>
	public class XmlExtractor : IDataExtractor {
		/// <inheritdoc/>
		public IReadOnlyList<string> SupportedExtensions => new[] { ".xml" };

		/// <inheritdoc/>
		public bool CanHandle(Stream stream, string? fileExtension = null) {
			if (fileExtension is not null) {
				return fileExtension.ToLowerInvariant() is ".xml";
			}

			// Check for XML declaration or root element start
			if (!stream.CanSeek) return false;
			var pos = stream.Position;
			int firstByte;
			do {
				firstByte = stream.ReadByte();
			} while (firstByte is ' ' or '\t' or '\r' or '\n');
			stream.Position = pos;
			return firstByte == '<';
		}

		/// <inheritdoc/>
		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			options ??= new ExtractionOptions();

			var rowElements = ReadRowElements(stream);

			string[]? headers = null;
			TypeMapper<T>? mapper = null;
			int dataRowIndex = 0;

			foreach (var element in rowElements) {
				if (headers is null) {
					headers = GetHeaders(element);
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

			var rowElements = ReadRowElements(stream);

			string[]? headers = null;
			int dataRowIndex = 0;

			foreach (var element in rowElements) {
				headers ??= GetHeaders(element);

				if (options.MaxRows.HasValue && dataRowIndex >= options.MaxRows.Value)
					yield break;

				var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
				foreach (var child in element.Elements()) {
					expando[child.Name.LocalName] = child.Value;
				}
				foreach (var attr in element.Attributes()) {
					expando[attr.Name.LocalName] = attr.Value;
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
		/// Parses the XML and returns the repeating row elements.
		/// Finds the most common child element name under the root
		/// (or under a single wrapper element) as the row element.
		/// </summary>
		private static List<XElement> ReadRowElements(Stream stream) {
			var doc = XDocument.Load(stream);
			var root = doc.Root;
			if (root is null)
				return [];

			// If root has children with the same name, those are the rows
			var childGroups = root.Elements()
				.GroupBy(e => e.Name)
				.OrderByDescending(g => g.Count())
				.ToList();

			if (childGroups.Count == 0)
				return [];

			var largestGroup = childGroups[0];

			// If the largest group has only 1 element and it has children,
			// it might be a wrapper (e.g., <Root><Items><Item/><Item/></Items></Root>)
			if (largestGroup.Count() == 1 && childGroups.Count == 1) {
				var wrapper = largestGroup.First();
				var innerGroups = wrapper.Elements()
					.GroupBy(e => e.Name)
					.OrderByDescending(g => g.Count())
					.ToList();

				if (innerGroups.Count > 0 && innerGroups[0].Count() > 1) {
					return innerGroups[0].ToList();
				}

				// Single wrapper with diverse children � treat wrapper children as rows
				if (wrapper.HasElements) {
					return wrapper.Elements().ToList();
				}
			}

			return largestGroup.ToList();
		}

		/// <summary>
		/// Builds column headers from the first row element's child element
		/// and attribute names.
		/// </summary>
		private static string[] GetHeaders(XElement element) {
			var headers = new List<string>();
			foreach (var child in element.Elements()) {
				headers.Add(child.Name.LocalName);
			}
			foreach (var attr in element.Attributes()) {
				headers.Add(attr.Name.LocalName);
			}
			return headers.ToArray();
		}

		/// <summary>
		/// Extracts values from an element in header order.
		/// </summary>
		private static object?[] ExtractValues(XElement element, string[] headers) {
			var values = new object?[headers.Length];
			for (int i = 0; i < headers.Length; i++) {
				var name = headers[i];
				var child = element.Element(XName.Get(name));
				if (child is not null) {
					values[i] = child.Value;
					continue;
				}
				var attr = element.Attribute(XName.Get(name));
				values[i] = attr?.Value;
			}
			return values;
		}
	}
}


using System.Globalization;
using System.Reflection;

namespace Unio.Mapping {

	/// <summary>
	/// Maps raw row data (string[] or object[]) to strongly-typed objects using attributes.
	/// This is the heart of Unio's type mapping engine.
	/// </summary>
	public class TypeMapper<T> where T : class, new() {
		private readonly PropertyMapping[] _mappings;
		private readonly CultureInfo _culture;

		/// <summary>Creates a new mapper using the given column headers and culture.</summary>
		public TypeMapper(string[]? headers, CultureInfo? culture = null) {
			_culture = culture ?? CultureInfo.InvariantCulture;
			_mappings = BuildMappings(headers);
		}

		/// <summary>
		/// Maps a single row of raw values to a typed object.
		/// </summary>
		public T Map(object?[] values) {
			var obj = new T();

			foreach (var mapping in _mappings) {
				if (mapping.ColumnIndex < 0 || mapping.ColumnIndex >= values.Length)
					continue;

				var rawValue = values[mapping.ColumnIndex];
				if (rawValue is null || (rawValue is string s && string.IsNullOrWhiteSpace(s)))
					continue;

				try {
					var converted = ConvertValue(rawValue, mapping.Property.PropertyType, mapping.DateFormat);
					mapping.Property.SetValue(obj, converted);
				}
				catch (Exception ex) {
					throw new UnioMappingException(
						$"Failed to map column '{mapping.ColumnName}' (index {mapping.ColumnIndex}) " +
						$"to property '{mapping.Property.Name}' of type '{mapping.Property.PropertyType.Name}'. " +
						$"Raw value: '{rawValue}'", ex);
				}
			}

			return obj;
		}

		/// <summary>
		/// Builds the property mappings based on the target type's properties and the provided headers.
		/// </summary>
		private static PropertyMapping[] BuildMappings(string[]? headers) {
			var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanWrite)
				.Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null)
				.ToArray();

			var mappings = new List<PropertyMapping>();
			int autoIndex = 0;

			foreach (var prop in properties) {
				var mapping = CreatePropertyMapping(prop, headers, autoIndex);
				if (mapping is not null) {
					mappings.Add(mapping);
				}
				autoIndex++;
			}

			return mappings.ToArray();
		}

		/// <summary>
		/// Creates a PropertyMapping for a given property, determining the column index and name based on attributes and headers.
		/// </summary>
		private static PropertyMapping? CreatePropertyMapping(PropertyInfo prop, string[]? headers, int autoIndex) {
			var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
			var dateAttr = prop.GetCustomAttribute<DateFormatAttribute>();

			var (index, name) = ResolveColumnIndexAndName(prop, colAttr, headers, autoIndex);

			if (index < 0)
				return null;

			return new PropertyMapping {
				Property = prop,
				ColumnIndex = index,
				ColumnName = name ?? prop.Name,
				DateFormat = dateAttr?.Format,
			};
		}

		/// <summary>
		/// Resolves the column index and name for a property based on the ColumnAttribute and headers.
		/// </summary>
		private static (int index, string? name) ResolveColumnIndexAndName(PropertyInfo prop, ColumnAttribute? colAttr, string[]? headers, int autoIndex) {
			int index = -1;
			string? name = null;

			if (colAttr is not null) {
				if (colAttr.Index >= 0) {
					index = colAttr.Index;
					name = colAttr.Name ?? prop.Name;
				}
				else if (colAttr.Name is not null && headers is not null) {
					name = colAttr.Name;
					index = FindHeaderIndex(headers, colAttr.Name);
				}
			}
			else if (headers is not null) {
				// Convention: match property name to header (case-insensitive)
				name = prop.Name;
				index = FindHeaderIndex(headers, prop.Name);
			}
			else {
				// No headers, no attribute � map by property declaration order
				index = autoIndex;
				name = prop.Name;
			}

			return (index, name);
		}
		/// <summary>
		/// Finds the column index in the headers array that matches the given name.
		/// </summary>
		private static int FindHeaderIndex(string[] headers, string name) {
			return HeaderMatcher.FindIndex(headers, name);
		}

		/// <summary>
		/// Converts a raw value to the target property type.
		/// </summary>
		private object? ConvertValue(object raw, Type targetType, string? dateFormat) {
			// Handle nullable types
			var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

			if (raw.GetType() == underlying)
				return raw;

			var str = raw.ToString() ?? "";

			// Delegate to specialized conversion methods
			if (underlying == typeof(string))
				return str;

			if (underlying == typeof(bool))
				return ParseBool(str);

			if (underlying == typeof(DateTime))
				return ConvertToDateTime(raw, str, dateFormat);

			if (underlying.IsEnum)
				return Enum.Parse(underlying, str, ignoreCase: true);

			return ConvertNumericOrOther(str, underlying);
		}


		/// <summary>
		/// Converts numeric types and other common types using culture-aware parsing.
		/// </summary>
		private object ConvertNumericOrOther(string str, Type underlying) {
			if (underlying == typeof(int))
				return int.Parse(CleanNumericString(str), NumberStyles.Any, _culture);

			if (underlying == typeof(long))
				return long.Parse(CleanNumericString(str), NumberStyles.Any, _culture);

			if (underlying == typeof(decimal))
				return decimal.Parse(CleanNumericString(str), NumberStyles.Any, _culture);

			if (underlying == typeof(double))
				return double.Parse(CleanNumericString(str), NumberStyles.Any, _culture);

			if (underlying == typeof(float))
				return float.Parse(CleanNumericString(str), NumberStyles.Any, _culture);

			if (underlying == typeof(DateTimeOffset))
				return DateTimeOffset.Parse(str, _culture);

			if (underlying == typeof(Guid))
				return Guid.Parse(str);

			return Convert.ChangeType(str, underlying, _culture);
		}

		/// <summary>
		/// Strips common currency symbols and whitespace that may appear in
		/// values extracted from PDFs or other formatted sources.
		/// </summary>
		private static string CleanNumericString(string value) {
			var span = value.AsSpan().Trim();
			while (span.Length > 0 && (span[0] == '$' || span[0] == '�' || span[0] == '�' || span[0] == '�'))
				span = span[1..].TrimStart();
			return span.ToString();
		}


		/// <summary>
		/// Converts date values, handling both string representations and Excel's OADate format.
		/// </summary>
		private object ConvertToDateTime(object raw, string str, string? dateFormat) {
			if (dateFormat is not null)
				return DateTime.ParseExact(str, dateFormat, _culture);

			if (raw is double oaDate)
				return DateTime.FromOADate(oaDate);

			return DateTime.Parse(str, _culture);
		}

		/// <summary>
		/// Parses boolean values from various common representations.
		/// </summary>
		private static bool ParseBool(string value) {
			return value.ToLowerInvariant() switch {
				"true" or "1" or "yes" or "y" or "on" => true,
				"false" or "0" or "no" or "n" or "off" => false,
				_ => bool.Parse(value)
			};
		}

		/// <summary>
		/// Internal class to hold mapping info for each property, including column index, name, and date format if applicable.
		/// </summary>
		private sealed class PropertyMapping {
			public required PropertyInfo Property { get; init; }
			public required int ColumnIndex { get; init; }
			public required string ColumnName { get; init; }
			public string? DateFormat { get; init; }
		}
	}


	/// <summary>
	/// Custom exception type for mapping errors, providing detailed context about the failure.
	/// </summary>

	/// <inheritdoc/>
	public class UnioMappingException : Exception {
		/// <summary>Creates a new <see cref="UnioMappingException"/> with the specified message.</summary>
		public UnioMappingException(string message, Exception? inner = null)
			: base(message, inner) { }
	}
}


using System.Globalization;
using System.Reflection;

namespace Unio.Mapping {

	/// <summary>
	/// Maps raw row data (string[] or object[]) to strongly-typed objects using attributes.
	/// This is the heart of Unio's type mapping engine.
	/// </summary>
	internal class TypeMapper<T> where T : class, new() {
		private readonly PropertyMapping[] _mappings;
		private readonly CultureInfo _culture;

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
		/// Builds the property mappings based on the target type's properties, the provided headers, and any mapping attributes. It iterates over all writable properties, checks for IgnoreAttribute, and then creates a PropertyMapping for each using CreatePropertyMapping. The resulting array of mappings is used for efficient mapping during extraction.
		/// </summary>
		/// <param name="headers"></param>
		/// <returns></returns>
		private static PropertyMapping[] BuildMappings(string[]? headers) {
			var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanWrite)
				.Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null);

			var mappings = new List<PropertyMapping>();

			foreach (var prop in properties) {
				var mapping = CreatePropertyMapping(prop, headers);
				if (mapping is not null) {
					mappings.Add(mapping);
				}
			}

			return mappings.ToArray();
		}

		/// <summary>
		/// Creates a PropertyMapping for a given property, determining the column index and name based on attributes and headers. It first checks for a ColumnAttribute, then tries to match by name if headers are available, and finally falls back to convention-based matching. Returns null if no valid mapping can be created (e.g., column index out of range).
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		private static PropertyMapping? CreatePropertyMapping(PropertyInfo prop, string[]? headers) {
			var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
			var dateAttr = prop.GetCustomAttribute<DateFormatAttribute>();

			var (index, name) = ResolveColumnIndexAndName(prop, colAttr, headers);

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
		/// Resolves the column index and name for a property based on the ColumnAttribute and headers. It first checks for explicit index, then name matching, and finally falls back to convention-based matching if no attribute is present. Returns -1 if no match is found.
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="colAttr"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		private static (int index, string? name) ResolveColumnIndexAndName(PropertyInfo prop, ColumnAttribute? colAttr, string[]? headers) {
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

			return (index, name);
		}
		/// <summary>
		/// Finds the column index in the headers array that matches the given name, using case-insensitive comparison and some normalization for common variations (spaces, underscores). Returns -1 if not found.
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="name"></param>
		/// <returns></returns>

		private static int FindHeaderIndex(string[] headers, string name) {
			for (int i = 0; i < headers.Length; i++) {
				if (string.Equals(headers[i]?.Trim() ?? "", name, StringComparison.OrdinalIgnoreCase))
					return i;
			}

			// Try fuzzy: remove spaces and underscores
			var normalized = name.Replace(" ", "").Replace("_", "");
			for (int i = 0; i < headers.Length; i++) {
				var headerNorm = (headers[i]?.Trim() ?? "").Replace(" ", "").Replace("_", "");
				if (string.Equals(headerNorm, normalized, StringComparison.OrdinalIgnoreCase))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Converts a raw value to the target property type, handling nullable types, enums, and using culture-aware parsing for numbers and dates. Also supports custom date formats via attributes.
		/// </summary>
		/// <param name="raw"></param>
		/// <param name="targetType"></param>
		/// <param name="dateFormat"></param>
		/// <returns></returns>
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
		/// Converts numeric types and other common types like DateTimeOffset and Guid, using culture-aware parsing where applicable.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="underlying"></param>
		/// <returns></returns>
		private object ConvertNumericOrOther(string str, Type underlying) {
			if (underlying == typeof(int))
				return int.Parse(str, _culture);

			if (underlying == typeof(long))
				return long.Parse(str, _culture);

			if (underlying == typeof(decimal))
				return decimal.Parse(str, NumberStyles.Any, _culture);

			if (underlying == typeof(double))
				return double.Parse(str, NumberStyles.Any, _culture);

			if (underlying == typeof(float))
				return float.Parse(str, NumberStyles.Any, _culture);

			if (underlying == typeof(DateTimeOffset))
				return DateTimeOffset.Parse(str, _culture);

			if (underlying == typeof(Guid))
				return Guid.Parse(str);

			return Convert.ChangeType(str, underlying, _culture);
		}


		/// <summary>
		///    Converts date values, handling both string representations and Excel's OADate format.
		/// </summary>
		/// <param name="raw"></param>
		/// <param name="str"></param>
		/// <param name="dateFormat"></param>
		/// <returns></returns>
		private object ConvertToDateTime(object raw, string str, string? dateFormat) {
			if (dateFormat is not null)
				return DateTime.ParseExact(str, dateFormat, _culture);

			if (raw is double oaDate)
				return DateTime.FromOADate(oaDate);

			return DateTime.Parse(str, _culture);
		}

		/// <summary>
		///  Parses boolean values from various common representations.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
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

	public class UnioMappingException : Exception {
		public UnioMappingException(string message, Exception? inner = null)
			: base(message, inner) { }
	}
}
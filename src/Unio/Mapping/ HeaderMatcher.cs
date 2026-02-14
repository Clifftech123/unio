

namespace Unio.Mapping {

	/// <summary>
	/// Utility for matching column headers to property names using
	/// multiple strategies: exact, case-insensitive, and normalized (fuzzy).
	///
	/// This is used by TypeMapper and can be used by extractor implementations
	/// that need custom header resolution.
	/// </summary>
	public static class HeaderMatcher {

		/// <summary>
		/// Finds the index of a header that matches the given name.
		/// Tries exact case-insensitive match first, then normalized fuzzy match.
		/// Returns -1 if no match is found.
		/// </summary>
		public static int FindIndex(string[] headers, string name) {
			// Exact case-insensitive match
			for (int i = 0; i < headers.Length; i++) {
				if (string.Equals(headers[i]?.Trim() ?? "", name, StringComparison.OrdinalIgnoreCase))
					return i;
			}

			// Fuzzy: remove spaces, underscores, and hyphens
			var normalized = Normalize(name);
			for (int i = 0; i < headers.Length; i++) {
				var headerNorm = Normalize(headers[i] ?? "");
				if (string.Equals(headerNorm, normalized, StringComparison.OrdinalIgnoreCase))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Checks if two header names are considered equivalent.
		/// </summary>
		public static bool IsMatch(string header, string propertyName) {
			if (string.Equals(header?.Trim(), propertyName, StringComparison.OrdinalIgnoreCase))
				return true;

			return string.Equals(
				Normalize(header ?? ""),
				Normalize(propertyName),
				StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns the best matching header name for a property name, or null if none found.
		/// </summary>
		public static string? FindBestMatch(string[] headers, string propertyName) {
			int idx = FindIndex(headers, propertyName);
			return idx >= 0 ? headers[idx] : null;
		}

		private static string Normalize(string value)
			=> value.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");
	}
}



namespace Unio.Mapping {

	/// <summary>
	/// Maps a property to a column by name or index.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ColumnAttribute : Attribute {
		/// <summary>Column header name to match against.</summary>
		public string? Name { get; }

		/// <summary>Zero-based column index.</summary>
		public int Index { get; } = -1;

		/// <summary>Maps this property to the column with the given header name.</summary>
		public ColumnAttribute(string name) => Name = name;

		/// <summary>Maps this property to the column at the given zero-based index.</summary>
		public ColumnAttribute(int index) => Index = index;
	}

}

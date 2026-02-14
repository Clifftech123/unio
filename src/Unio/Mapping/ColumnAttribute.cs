

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

		public ColumnAttribute(string name) => Name = name;

		public ColumnAttribute(int index) => Index = index;
	}

}

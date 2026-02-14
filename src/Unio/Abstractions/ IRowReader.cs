namespace Unio.Abstractions {

	/// <summary>
	/// Reads raw rows from a data source one at a time.
	/// Each extractor uses an IRowReader internally to stream rows
	/// without loading the entire file into memory.
	/// </summary>
	public interface IRowReader : IDisposable {
		/// <summary>
		/// The column headers, if available. Null if no header row was detected.
		/// </summary>
		string[]? Headers { get; }

		/// <summary>
		/// Advances to the next row. Returns false when no more rows are available.
		/// </summary>
		bool Read();

		/// <summary>
		/// Gets the current row's values as an object array.
		/// Call after <see cref="Read"/> returns true.
		/// </summary>
		object?[] GetValues();

		/// <summary>
		/// The zero-based index of the current row.
		/// </summary>
		int CurrentRowIndex { get; }
	}
}
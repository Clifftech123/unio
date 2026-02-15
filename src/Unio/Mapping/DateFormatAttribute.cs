

namespace Unio.Mapping {

	/// <summary>
	/// Specifies the date/time format string for parsing.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class DateFormatAttribute : Attribute {
		/// <summary>The date/time format string (e.g. "yyyy-MM-dd").</summary>
		public string Format { get; }
		/// <summary>Creates a new <see cref="DateFormatAttribute"/> with the specified format string.</summary>
		public DateFormatAttribute(string format) => Format = format;
	}
}

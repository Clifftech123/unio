

namespace Unio.Mapping {

	/// <summary>
	/// Specifies the date/time format string for parsing.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class DateFormatAttribute : Attribute {
		public string Format { get; }
		public DateFormatAttribute(string format) => Format = format;
	}
}

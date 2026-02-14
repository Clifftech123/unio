

namespace Unio.Mapping {

	/// <summary>
	/// Marks a property to be ignored during mapping.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class IgnoreAttribute : Attribute { }
}
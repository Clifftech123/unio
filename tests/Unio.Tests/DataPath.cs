namespace Unio.Tests;

internal static class DataPath {
	internal static string Get(string fileName) =>
		Path.Combine(AppContext.BaseDirectory, "Data", fileName);

	internal static Stream OpenRead(string fileName) =>
		File.OpenRead(Get(fileName));
}

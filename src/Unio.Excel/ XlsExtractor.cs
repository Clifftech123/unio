using Unio.Abstractions;

namespace Unio.Excel {
	public class XlsExtractor : IDataExtractor {
		public IReadOnlyList<string> SupportedExtensions => throw new NotImplementedException();

		public bool CanHandle(Stream stream, string? fileExtension = null) {
			throw new NotImplementedException();
		}

		public IEnumerable<T> Extract<T>(Stream stream, ExtractionOptions? options = null) where T : class, new() {
			throw new NotImplementedException();
		}

		public IEnumerable<dynamic> Extract(Stream stream, ExtractionOptions? options = null) {
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<T> ExtractAsync<T>(Stream stream, ExtractionOptions? options = null, CancellationToken cancellationToken = default) where T : class, new() {
			throw new NotImplementedException();
		}
	}
}
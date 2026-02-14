using System.Globalization;
using Unio.Abstractions;

namespace Unio {

	/// <summary>
	/// Configuration for the Unio DI registration.
	///
	/// Usage:
	///   services.AddUnio(config =&gt; {
	///       config.RegisterExtractor&lt;XlsxExtractor&gt;();
	///       config.DefaultCulture = new CultureInfo("en-US");
	///       config.OnError = ErrorHandling.CollectAndContinue;
	///   });
	/// </summary>
	public class UnioConfiguration {
		internal List<IDataExtractor> Extractors { get; } = [];

		/// <summary>Default culture for all extractions.</summary>
		public CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;

		/// <summary>Default error handling mode.</summary>
		public ErrorHandling OnError { get; set; } = ErrorHandling.ThrowOnFirst;

		/// <summary>Default validation callback applied to all extractions.</summary>
		public Func<object, IReadOnlyList<string>>? DefaultValidator { get; set; }

		/// <summary>
		/// Register a format extractor by instance.
		/// </summary>
		public UnioConfiguration RegisterExtractor(IDataExtractor extractor) {
			Extractors.Add(extractor);
			return this;
		}

		/// <summary>
		/// Register a format extractor by type.
		/// </summary>
		public UnioConfiguration RegisterExtractor<TExtractor>() where TExtractor : IDataExtractor, new() {
			Extractors.Add(new TExtractor());
			return this;
		}
	}
}

using Microsoft.Extensions.DependencyInjection;
using Unio.Abstractions;

namespace Unio {

	/// <summary>
	/// DI registration for Unio.
	///
	/// Usage:
	///   services.AddUnio();
	///
	///   services.AddUnio(config => {
	///       config.RegisterExtractor&lt;XlsxExtractor&gt;();
	///       config.RegisterExtractor&lt;PdfTableExtractor&gt;();
	///       config.DefaultCulture = new CultureInfo("en-US");
	///       config.OnError = ErrorHandling.CollectAndContinue;
	///   });
	/// </summary>
	public static class ServiceCollectionExtensions {

		/// <summary>
		/// Registers Unio with default configuration (CSV support only).
		/// </summary>
		public static IServiceCollection AddUnio(this IServiceCollection services) {
			return services.AddUnio(_ => { });
		}

		/// <summary>
		/// Registers Unio with custom configuration.
		/// </summary>
		public static IServiceCollection AddUnio(this IServiceCollection services, Action<UnioConfiguration> configure) {
			var config = new UnioConfiguration();
			configure(config);

			services.AddSingleton(config);
			services.AddSingleton<IUnioExtractor>(sp => new Unio(sp.GetRequiredService<UnioConfiguration>()));

			return services;
		}
	}
}

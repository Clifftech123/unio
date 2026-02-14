using System.ComponentModel.DataAnnotations;
using Unio.Abstractions;

namespace Unio.Validation {

	/// <summary>
	/// Extension methods to plug validation into Unio's extraction pipeline.
	///
	/// Usage with DataAnnotations:
	///   var result = Unio.ExtractWithErrors&lt;Invoice&gt;("invoices.csv", opt => {
	///       opt.UseDataAnnotationValidation();
	///   });
	///   // result.Errors contains validation failures
	///   // result.Records contains only valid records
	///
	/// Usage with fluent rules:
	///   var validator = new FluentRecordValidator&lt;Invoice&gt;()
	///       .RuleFor(x => x.Amount, v => v.GreaterThan(0m))
	///       .RuleFor(x => x.InvoiceNo, v => v.NotEmpty());
	///
	///   var result = Unio.ExtractWithErrors&lt;Invoice&gt;("invoices.csv", opt => {
	///       opt.UseFluentValidation(validator);
	///   });
	///
	/// Standalone usage (without pipeline):
	///   var records = Unio.Extract&lt;Invoice&gt;("invoices.csv");
	///   var batch = new DataAnnotationValidator().ValidateAll(records);
	///   // batch.Valid, batch.Invalid, batch.Errors
	/// </summary>
	public static class ValidationExtensions {

		/// <summary>
		/// Enables DataAnnotation validation ([Required], [Range], etc.)
		/// on each extracted record during ExtractWithErrors.
		/// </summary>
		public static ExtractionOptions UseDataAnnotationValidation(this ExtractionOptions options) {
			options.Validate = true;
			options.RecordValidator = record => {
				var context = new ValidationContext(record);
				var results = new List<ValidationResult>();
				Validator.TryValidateObject(record, context, results, validateAllProperties: true);
				return results
					.Select(r => $"{string.Join(", ", r.MemberNames)}: {r.ErrorMessage}")
					.ToList();
			};
			return options;
		}

		/// <summary>
		/// Enables fluent validation on each extracted record during ExtractWithErrors.
		/// </summary>
		public static ExtractionOptions UseFluentValidation<T>(
			this ExtractionOptions options,
			FluentRecordValidator<T> validator) where T : class {
			options.Validate = true;
			options.RecordValidator = record => {
				if (record is not T typed)
					return Array.Empty<string>();

				var outcome = validator.Validate(typed);
				return outcome.Errors.Select(e => $"{e.PropertyName}: {e.Message}").ToList();
			};
			return options;
		}
	}
}

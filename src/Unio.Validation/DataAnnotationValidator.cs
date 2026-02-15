using System.ComponentModel.DataAnnotations;
using Unio.Abstractions;

namespace Unio.Validation {

	/// <summary>
	/// Validates extracted records using System.ComponentModel.DataAnnotations attributes
	/// such as [Required], [Range], [StringLength], [RegularExpression], etc.
	/// 
	/// Usage:
	///   var validator = new DataAnnotationValidator();
	///   var result = validator.Validate(record);
	///   var batchResult = validator.ValidateAll(records);
	/// </summary>
	public class DataAnnotationValidator {

		/// <summary>
		/// Validates a single record using DataAnnotations.
		/// Returns a ValidationOutcome with any validation errors.
		/// </summary>
		public ValidationOutcome Validate<T>(T record, int rowNumber = 0) where T : class {
			ArgumentNullException.ThrowIfNull(record);

			var context = new ValidationContext(record);
			var results = new List<ValidationResult>();
			bool isValid = Validator.TryValidateObject(record, context, results, validateAllProperties: true);

			if (isValid)
				return ValidationOutcome.Success(rowNumber);

			var errors = results
				.Select(r => new ValidationError(
					PropertyName: r.MemberNames.FirstOrDefault(),
					Message: r.ErrorMessage ?? "Validation failed.",
					RowNumber: rowNumber))
				.ToList();

			return new ValidationOutcome(rowNumber, false, errors);
		}

		/// <summary>
		/// Validates all records and returns a batch result with valid records,
		/// invalid records, and all errors collected.
		/// </summary>
		public BatchValidationResult<T> ValidateAll<T>(IEnumerable<T> records) where T : class {
			ArgumentNullException.ThrowIfNull(records);

			var valid = new List<T>();
			var invalid = new List<T>();
			var allErrors = new List<ValidationError>();
			int row = 0;

			foreach (var record in records) {
				var outcome = Validate(record, row);
				if (outcome.IsValid) {
					valid.Add(record);
				} else {
					invalid.Add(record);
					allErrors.AddRange(outcome.Errors);
				}
				row++;
			}

			return new BatchValidationResult<T>(valid, invalid, allErrors);
		}

		/// <summary>
		/// Validates all records and returns only the valid ones.
		/// Invalid records are silently skipped.
		/// </summary>
		public IEnumerable<T> FilterValid<T>(IEnumerable<T> records) where T : class {
			foreach (var record in records) {
				var context = new ValidationContext(record);
				if (Validator.TryValidateObject(record, context, null, validateAllProperties: true))
					yield return record;
			}
		}

		/// <summary>
		/// Validates records and converts to ExtractionErrors for integration
		/// with Unio's error reporting pipeline.
		/// </summary>
		public List<ExtractionError> ToExtractionErrors<T>(IEnumerable<T> records) where T : class {
			var errors = new List<ExtractionError>();
			int row = 0;

			foreach (var record in records) {
				var outcome = Validate(record, row);
				foreach (var error in outcome.Errors) {
					errors.Add(new ExtractionError(
						RowNumber: error.RowNumber,
						ColumnName: error.PropertyName,
						Message: error.Message));
				}
				row++;
			}

			return errors;
		}
	}

	/// <summary>
	/// Result of validating a single record.
	/// </summary>
	public sealed class ValidationOutcome {
		/// <summary>The zero-based row number of the validated record.</summary>
		public int RowNumber { get; }
		/// <summary>Whether the record passed validation.</summary>
		public bool IsValid { get; }
		/// <summary>Validation errors found, if any.</summary>
		public IReadOnlyList<ValidationError> Errors { get; }

		/// <summary>Creates a new validation outcome.</summary>
		public ValidationOutcome(int rowNumber, bool isValid, IReadOnlyList<ValidationError> errors) {
			RowNumber = rowNumber;
			IsValid = isValid;
			Errors = errors;
		}

		/// <summary>Creates a successful validation outcome with no errors.</summary>
		public static ValidationOutcome Success(int rowNumber)
			=> new(rowNumber, true, Array.Empty<ValidationError>());
	}

	/// <summary>
	/// A single validation error with property name, message, and row context.
	/// </summary>
	public record ValidationError(
		string? PropertyName,
		string Message,
		int RowNumber
	);

	/// <summary>
	/// Result of validating a batch of records.
	/// </summary>
	public sealed class BatchValidationResult<T> where T : class {
		/// <summary>Records that passed validation.</summary>
		public IReadOnlyList<T> Valid { get; }
		/// <summary>Records that failed validation.</summary>
		public IReadOnlyList<T> Invalid { get; }
		/// <summary>All validation errors across all records.</summary>
		public IReadOnlyList<ValidationError> Errors { get; }
		/// <summary>Total number of records validated.</summary>
		public int TotalCount => Valid.Count + Invalid.Count;
		/// <summary>Whether any validation errors were found.</summary>
		public bool HasErrors => Errors.Count > 0;

		/// <summary>Creates a new batch validation result.</summary>
		public BatchValidationResult(
			IReadOnlyList<T> valid,
			IReadOnlyList<T> invalid,
			IReadOnlyList<ValidationError> errors) {
			Valid = valid;
			Invalid = invalid;
			Errors = errors;
		}
	}
}
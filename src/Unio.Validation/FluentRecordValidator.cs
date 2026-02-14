using System.Linq.Expressions;

namespace Unio.Validation {

	/// <summary>
	/// Fluent validation builder for extracted records.
	/// Allows defining validation rules without DataAnnotation attributes.
	///
	/// Usage:
	///   var validator = new FluentRecordValidator&lt;Invoice&gt;()
	///       .RuleFor(x => x.Amount, v => v.GreaterThan(0).LessThan(1_000_000))
	///       .RuleFor(x => x.InvoiceNo, v => v.NotEmpty().MaxLength(20))
	///       .RuleFor(x => x.Date, v => v.NotDefault());
	///
	///   var result = validator.Validate(invoice);
	///   var batch = validator.ValidateAll(invoices);
	/// </summary>
	public class FluentRecordValidator<T> where T : class {
		private readonly List<IPropertyRule<T>> _rules = [];

		/// <summary>
		/// Define validation rules for a property using a fluent builder.
		/// </summary>
		public FluentRecordValidator<T> RuleFor<TProp>(
			Expression<Func<T, TProp>> propertyExpr,
			Action<PropertyRuleBuilder<T, TProp>> configure) {
			var memberExpr = propertyExpr.Body as MemberExpression
				?? throw new ArgumentException("Expression must be a property access.", nameof(propertyExpr));

			var compiled = propertyExpr.Compile();
			var builder = new PropertyRuleBuilder<T, TProp>(memberExpr.Member.Name, compiled);
			configure(builder);
			_rules.AddRange(builder.Build());
			return this;
		}

		/// <summary>
		/// Validates a single record against all defined rules.
		/// </summary>
		public ValidationOutcome Validate(T record, int rowNumber = 0) {
			var errors = new List<ValidationError>();

			foreach (var rule in _rules) {
				var error = rule.Evaluate(record, rowNumber);
				if (error is not null)
					errors.Add(error);
			}

			return errors.Count == 0
				? ValidationOutcome.Success(rowNumber)
				: new ValidationOutcome(rowNumber, false, errors);
		}

		/// <summary>
		/// Validates all records and returns a batch result.
		/// </summary>
		public BatchValidationResult<T> ValidateAll(IEnumerable<T> records) {
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
		/// Returns only records that pass all validation rules.
		/// </summary>
		public IEnumerable<T> FilterValid(IEnumerable<T> records) {
			int row = 0;
			foreach (var record in records) {
				if (Validate(record, row).IsValid)
					yield return record;
				row++;
			}
		}
	}

	/// <summary>
	/// Internal interface for a single validation rule on a typed record.
	/// </summary>
	internal interface IPropertyRule<in T> {
		ValidationError? Evaluate(T record, int rowNumber);
	}

	/// <summary>
	/// Fluent builder for defining validation rules on a single property.
	/// </summary>
	public class PropertyRuleBuilder<T, TProp> where T : class {
		private readonly string _propertyName;
		private readonly Func<T, TProp> _getter;
		private readonly List<IPropertyRule<T>> _rules = [];

		internal PropertyRuleBuilder(string propertyName, Func<T, TProp> getter) {
			_propertyName = propertyName;
			_getter = getter;
		}

		internal List<IPropertyRule<T>> Build() => _rules;

		/// <summary>Value must not be null.</summary>
		public PropertyRuleBuilder<T, TProp> NotNull(string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must not be null.",
				record => _getter(record) is not null));
			return this;
		}

		/// <summary>String must not be null or empty.</summary>
		public PropertyRuleBuilder<T, TProp> NotEmpty(string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must not be empty.",
				record => {
					var val = _getter(record);
					if (val is null) return false;
					if (val is string s) return !string.IsNullOrWhiteSpace(s);
					return true;
				}));
			return this;
		}

		/// <summary>String length must not exceed the maximum.</summary>
		public PropertyRuleBuilder<T, TProp> MaxLength(int max, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must be at most {max} characters.",
				record => _getter(record)?.ToString()?.Length <= max));
			return this;
		}

		/// <summary>String length must meet the minimum.</summary>
		public PropertyRuleBuilder<T, TProp> MinLength(int min, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must be at least {min} characters.",
				record => (_getter(record)?.ToString()?.Length ?? 0) >= min));
			return this;
		}

		/// <summary>Numeric value must be greater than the specified value.</summary>
		public PropertyRuleBuilder<T, TProp> GreaterThan(TProp min, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must be greater than {min}.",
				record => Comparer<TProp>.Default.Compare(_getter(record), min) > 0));
			return this;
		}

		/// <summary>Numeric value must be less than the specified value.</summary>
		public PropertyRuleBuilder<T, TProp> LessThan(TProp max, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must be less than {max}.",
				record => Comparer<TProp>.Default.Compare(_getter(record), max) < 0));
			return this;
		}

		/// <summary>Value must be within an inclusive range.</summary>
		public PropertyRuleBuilder<T, TProp> Between(TProp min, TProp max, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must be between {min} and {max}.",
				record => {
					var val = _getter(record);
					return Comparer<TProp>.Default.Compare(val, min) >= 0
						&& Comparer<TProp>.Default.Compare(val, max) <= 0;
				}));
			return this;
		}

		/// <summary>Value must not be the default for its type.</summary>
		public PropertyRuleBuilder<T, TProp> NotDefault(string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must not be the default value.",
				record => !EqualityComparer<TProp>.Default.Equals(_getter(record), default!)));
			return this;
		}

		/// <summary>String must match the given regex pattern.</summary>
		public PropertyRuleBuilder<T, TProp> Matches(string pattern, string? message = null) {
			var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} must match pattern '{pattern}'.",
				record => {
					var val = _getter(record)?.ToString();
					return val is not null && regex.IsMatch(val);
				}));
			return this;
		}

		/// <summary>Custom validation predicate.</summary>
		public PropertyRuleBuilder<T, TProp> Must(Func<TProp, bool> predicate, string? message = null) {
			_rules.Add(new DelegateRule<T>(_propertyName,
				message ?? $"{_propertyName} failed custom validation.",
				record => predicate(_getter(record))));
			return this;
		}
	}

	/// <summary>
	/// A validation rule backed by a delegate.
	/// </summary>
	internal sealed class DelegateRule<T> : IPropertyRule<T> {
		private readonly string _propertyName;
		private readonly string _message;
		private readonly Func<T, bool> _predicate;

		public DelegateRule(string propertyName, string message, Func<T, bool> predicate) {
			_propertyName = propertyName;
			_message = message;
			_predicate = predicate;
		}

		public ValidationError? Evaluate(T record, int rowNumber) {
			return _predicate(record)
				? null
				: new ValidationError(_propertyName, _message, rowNumber);
		}
	}
}

using FluentAssertions;
using Unio.Validation;
using Unio.Tests.Models;

namespace Unio.Tests.Validation;

public class FluentRecordValidatorTests {

	[Fact]
	public void Validate_AllRulesPass_ReturnsSuccess() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.NotEmpty())
			.RuleFor(x => x.Salary, v => v.GreaterThan(0m))
			.RuleFor(x => x.Id, v => v.NotDefault());

		var employee = new Employee { Id = 1, Name = "Kwame", Salary = 85000m };
		var result = validator.Validate(employee);

		result.IsValid.Should().BeTrue();
		result.Errors.Should().BeEmpty();
	}

	[Fact]
	public void Validate_NotEmpty_FailsOnEmpty() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.NotEmpty());

		var result = validator.Validate(new Employee { Name = "" });

		result.IsValid.Should().BeFalse();
		result.Errors.Should().HaveCount(1);
		result.Errors[0].PropertyName.Should().Be("Name");
	}

	[Fact]
	public void Validate_GreaterThan_FailsOnZero() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.GreaterThan(0m));

		var result = validator.Validate(new Employee { Salary = 0m });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_LessThan_FailsOnExceed() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.LessThan(100_000m));

		var result = validator.Validate(new Employee { Salary = 150_000m });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_Between_PassesInRange() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.Between(50_000m, 200_000m));

		var result = validator.Validate(new Employee { Salary = 85_000m });

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_Between_FailsOutOfRange() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.Between(50_000m, 200_000m));

		var result = validator.Validate(new Employee { Salary = 10_000m });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_MaxLength_FailsOnLongString() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.MaxLength(5));

		var result = validator.Validate(new Employee { Name = "Kwame Asante" });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_MinLength_FailsOnShortString() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.MinLength(10));

		var result = validator.Validate(new Employee { Name = "Ama" });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_NotDefault_FailsOnDefaultDateTime() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.StartDate, v => v.NotDefault());

		var result = validator.Validate(new Employee { StartDate = default });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_Matches_FailsOnBadPattern() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Email, v => v.Matches(@"^.+@.+\..+$"));

		var result = validator.Validate(new Employee { Email = "not-an-email" });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_Matches_PassesOnGoodPattern() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Email, v => v.Matches(@"^.+@.+\..+$"));

		var result = validator.Validate(new Employee { Email = "kwame@unio.dev" });

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_Must_CustomPredicate() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Department, v => v.Must(
				d => d == "Engineering" || d == "Marketing",
				"Department must be Engineering or Marketing."));

		validator.Validate(new Employee { Department = "Engineering" }).IsValid.Should().BeTrue();
		validator.Validate(new Employee { Department = "Fake" }).IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_NotNull_FailsOnNull() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.NotNull());

		var result = validator.Validate(new Employee { Name = null! });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_CustomMessage_IsUsed() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.GreaterThan(0m, "Pay your people!"));

		var result = validator.Validate(new Employee { Salary = 0m });

		result.Errors[0].Message.Should().Be("Pay your people!");
	}

	// --- ValidateAll ---

	[Fact]
	public void ValidateAll_SeparatesValidAndInvalid() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.GreaterThan(70_000m));

		var employees = new List<Employee> {
			new() { Name = "High", Salary = 90_000m },
			new() { Name = "Low", Salary = 50_000m },
			new() { Name = "Mid", Salary = 75_000m },
		};

		var result = validator.ValidateAll(employees);

		result.Valid.Should().HaveCount(2);
		result.Invalid.Should().HaveCount(1);
		result.Invalid[0].Name.Should().Be("Low");
	}

	// --- FilterValid ---

	[Fact]
	public void FilterValid_ReturnsOnlyPassing() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.IsActive, v => v.Must(a => a, "Must be active."));

		var employees = new List<Employee> {
			new() { Name = "Active", IsActive = true },
			new() { Name = "Inactive", IsActive = false },
		};

		var valid = validator.FilterValid(employees).ToList();

		valid.Should().HaveCount(1);
		valid[0].Name.Should().Be("Active");
	}

	// --- Multiple rules on same property ---

	[Fact]
	public void Validate_MultipleRules_ReportsAll() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Name, v => v.NotEmpty().MinLength(3).MaxLength(50));

		var result = validator.Validate(new Employee { Name = "" });

		result.IsValid.Should().BeFalse();
		result.Errors.Should().HaveCountGreaterThanOrEqualTo(1);
	}
}

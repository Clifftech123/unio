using FluentAssertions;
using Unio.Validation;
using Unio.Tests.Models;

namespace Unio.Tests.Validation;

public class DataAnnotationValidatorTests {
	private readonly DataAnnotationValidator _validator = new();

	[Fact]
	public void Validate_ValidRecord_ReturnsSuccess() {
		var employee = new ValidatedEmployee {
			Name = "Kwame Asante",
			Department = "Engineering",
			Salary = 85000.5m,
			StartDate = new DateTime(2021, 3, 15),
			IsActive = true,
			Email = "kwame@unio.dev"
		};

		var result = _validator.Validate(employee);

		result.IsValid.Should().BeTrue();
		result.Errors.Should().BeEmpty();
	}

	[Fact]
	public void Validate_MissingRequired_ReturnsErrors() {
		var employee = new ValidatedEmployee {
			Name = "",
			Department = "",
			Salary = 85000m,
			Email = "test@test.com"
		};

		var result = _validator.Validate(employee);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().NotBeEmpty();
	}

	[Fact]
	public void Validate_InvalidRange_ReturnsError() {
		var employee = new ValidatedEmployee {
			Name = "Test",
			Department = "HR",
			Salary = 0m,
			Email = "test@test.com"
		};

		var result = _validator.Validate(employee);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.Message.Contains("Salary"));
	}

	[Fact]
	public void Validate_InvalidEmail_ReturnsError() {
		var employee = new ValidatedEmployee {
			Name = "Test",
			Department = "HR",
			Salary = 50000m,
			Email = "not-an-email"
		};

		var result = _validator.Validate(employee);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Email");
	}

	[Fact]
	public void Validate_RowNumber_IsPreserved() {
		var employee = new ValidatedEmployee {
			Name = "",
			Department = "HR",
			Salary = 50000m,
			Email = "test@test.com"
		};

		var result = _validator.Validate(employee, rowNumber: 42);

		result.RowNumber.Should().Be(42);
		result.Errors.Should().AllSatisfy(e => e.RowNumber.Should().Be(42));
	}

	// --- ValidateAll ---

	[Fact]
	public void ValidateAll_MixedRecords_SeparatesValidAndInvalid() {
		var records = new List<ValidatedEmployee> {
			new() { Name = "Good", Department = "HR", Salary = 50000m, Email = "good@test.com" },
			new() { Name = "", Department = "", Salary = 0m, Email = "" },
			new() { Name = "Also Good", Department = "Eng", Salary = 80000m, Email = "also@test.com" },
		};

		var result = _validator.ValidateAll(records);

		result.Valid.Should().HaveCount(2);
		result.Invalid.Should().HaveCount(1);
		result.HasErrors.Should().BeTrue();
		result.TotalCount.Should().Be(3);
	}

	// --- FilterValid ---

	[Fact]
	public void FilterValid_ReturnsOnlyValidRecords() {
		var records = new List<ValidatedEmployee> {
			new() { Name = "Good", Department = "HR", Salary = 50000m, Email = "good@test.com" },
			new() { Name = "", Department = "", Salary = 0m, Email = "" },
		};

		var valid = _validator.FilterValid(records).ToList();

		valid.Should().HaveCount(1);
		valid[0].Name.Should().Be("Good");
	}
}

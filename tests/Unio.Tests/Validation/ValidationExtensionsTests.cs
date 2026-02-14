using FluentAssertions;
using Unio.Abstractions;
using Unio.Validation;
using Unio.Tests.Models;

namespace Unio.Tests.Validation;

public class ValidationExtensionsTests {

	[Fact]
	public void UseDataAnnotationValidation_IntoPipeline_ValidatesRecords() {
		var unio = new Unio();
		var result = unio.ExtractWithErrors<ValidatedEmployee>(DataPath.Get("employees.csv"), opt => {
			opt.UseDataAnnotationValidation();
		});

		// All employees in the file are valid
		result.HasErrors.Should().BeFalse();
		result.Records.Should().HaveCount(10);
	}

	[Fact]
	public void UseFluentValidation_IntoPipeline_FiltersInvalid() {
		var validator = new FluentRecordValidator<Employee>()
			.RuleFor(x => x.Salary, v => v.LessThan(100_000m));

		var unio = new Unio();
		var result = unio.ExtractWithErrors<Employee>(DataPath.Get("employees.csv"), opt => {
			opt.UseFluentValidation(validator);
		});

		// Nana Osei (105000) should be flagged
		result.HasErrors.Should().BeTrue();
		result.Records.Should().HaveCountLessThan(10);
	}
}

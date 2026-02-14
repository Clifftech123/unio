using FluentAssertions;
using Unio.Mapping;

namespace Unio.Tests.Mapping;

public class HeaderMatcherTests {

	[Fact]
	public void FindIndex_ExactMatch_ReturnsIndex() {
		var headers = new[] { "Id", "Name", "Department" };
		HeaderMatcher.FindIndex(headers, "Name").Should().Be(1);
	}

	[Fact]
	public void FindIndex_CaseInsensitive_ReturnsIndex() {
		var headers = new[] { "id", "name", "department" };
		HeaderMatcher.FindIndex(headers, "Name").Should().Be(1);
	}

	[Fact]
	public void FindIndex_WithUnderscores_MatchesFuzzy() {
		var headers = new[] { "first_name", "last_name" };
		HeaderMatcher.FindIndex(headers, "FirstName").Should().Be(0);
		HeaderMatcher.FindIndex(headers, "LastName").Should().Be(1);
	}

	[Fact]
	public void FindIndex_WithSpaces_MatchesFuzzy() {
		var headers = new[] { "First Name", "Last Name" };
		HeaderMatcher.FindIndex(headers, "FirstName").Should().Be(0);
	}

	[Fact]
	public void FindIndex_WithHyphens_MatchesFuzzy() {
		var headers = new[] { "invoice-no", "due-date" };
		HeaderMatcher.FindIndex(headers, "InvoiceNo").Should().Be(0);
		HeaderMatcher.FindIndex(headers, "DueDate").Should().Be(1);
	}

	[Fact]
	public void FindIndex_NoMatch_ReturnsNegativeOne() {
		var headers = new[] { "Id", "Name" };
		HeaderMatcher.FindIndex(headers, "NonExistent").Should().Be(-1);
	}

	[Fact]
	public void IsMatch_ExactMatch_ReturnsTrue() {
		HeaderMatcher.IsMatch("Name", "Name").Should().BeTrue();
	}

	[Fact]
	public void IsMatch_CaseInsensitive_ReturnsTrue() {
		HeaderMatcher.IsMatch("name", "Name").Should().BeTrue();
	}

	[Fact]
	public void IsMatch_Fuzzy_ReturnsTrue() {
		HeaderMatcher.IsMatch("first_name", "FirstName").Should().BeTrue();
	}

	[Fact]
	public void IsMatch_NoMatch_ReturnsFalse() {
		HeaderMatcher.IsMatch("Foo", "Bar").Should().BeFalse();
	}

	[Fact]
	public void FindBestMatch_ReturnsOriginalHeader() {
		var headers = new[] { "first_name", "last_name" };
		HeaderMatcher.FindBestMatch(headers, "FirstName").Should().Be("first_name");
	}

	[Fact]
	public void FindBestMatch_NoMatch_ReturnsNull() {
		var headers = new[] { "Id", "Name" };
		HeaderMatcher.FindBestMatch(headers, "Missing").Should().BeNull();
	}
}

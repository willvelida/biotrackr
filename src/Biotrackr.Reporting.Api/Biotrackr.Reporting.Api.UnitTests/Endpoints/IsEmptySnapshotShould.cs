using System.Text.Json;
using Biotrackr.Reporting.Api.Validation;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.UnitTests.Endpoints
{
    public class IsEmptySnapshotShould
    {
        [Fact]
        public void ReturnTrueForNullJsonElement()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("null");
            SnapshotValidator.IsEmpty(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyObject()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("{}");
            SnapshotValidator.IsEmpty(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyArray()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("[]");
            SnapshotValidator.IsEmpty(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyString()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("\"\"");
            SnapshotValidator.IsEmpty(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForWhitespaceString()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("\"   \"");
            SnapshotValidator.IsEmpty(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseForNonEmptyObject()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("""{"steps":[1,2,3]}""");
            SnapshotValidator.IsEmpty(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForNonEmptyArray()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("[1,2,3]");
            SnapshotValidator.IsEmpty(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForNumberValue()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("42");
            SnapshotValidator.IsEmpty(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForBooleanValue()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("true");
            SnapshotValidator.IsEmpty(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnTrueForNonJsonEmptyString()
        {
            SnapshotValidator.IsEmpty("").Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForNonJsonEmptyObjectString()
        {
            SnapshotValidator.IsEmpty("{}").Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseForNonJsonNonEmptyObject()
        {
            SnapshotValidator.IsEmpty("some data").Should().BeFalse();
        }
    }
}

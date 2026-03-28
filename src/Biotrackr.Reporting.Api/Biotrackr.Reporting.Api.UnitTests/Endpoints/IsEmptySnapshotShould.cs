using System.Text.Json;
using Biotrackr.Reporting.Api.Endpoints;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.UnitTests.Endpoints
{
    public class IsEmptySnapshotShould
    {
        [Fact]
        public void ReturnTrueForNullJsonElement()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("null");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyObject()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("{}");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyArray()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("[]");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForEmptyString()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("\"\"");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForWhitespaceString()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("\"   \"");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseForNonEmptyObject()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("""{"steps":[1,2,3]}""");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForNonEmptyArray()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("[1,2,3]");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForNumberValue()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("42");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseForBooleanValue()
        {
            var element = JsonSerializer.Deserialize<JsonElement>("true");
            GenerateEndpoints.IsEmptySnapshot(element).Should().BeFalse();
        }

        [Fact]
        public void ReturnTrueForNonJsonEmptyString()
        {
            GenerateEndpoints.IsEmptySnapshot("").Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueForNonJsonEmptyObjectString()
        {
            GenerateEndpoints.IsEmptySnapshot("{}").Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseForNonJsonNonEmptyObject()
        {
            GenerateEndpoints.IsEmptySnapshot("some data").Should().BeFalse();
        }
    }
}

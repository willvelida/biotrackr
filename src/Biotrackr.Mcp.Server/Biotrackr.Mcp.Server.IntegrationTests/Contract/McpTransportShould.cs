using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Biotrackr.Mcp.Server.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Biotrackr.Mcp.Server.IntegrationTests.Contract
{
    /// <summary>
    /// Tests that verify MCP protocol interactions work correctly
    /// through the HTTP transport layer.
    /// MapMcp() default pattern is "" (root path), so MCP endpoint is at "/".
    /// The MCP Streamable HTTP transport returns SSE format responses.
    /// </summary>
    [Collection(nameof(Collections.IntegrationTestCollection))]
    public class McpTransportShould
    {
        private readonly IntegrationTestFixture _fixture;

        public McpTransportShould(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Creates a JSON-RPC request with the required Accept header
        /// for the MCP Streamable HTTP transport.
        /// </summary>
        private static HttpRequestMessage CreateMcpRequest(object jsonRpcPayload)
        {
            var json = JsonSerializer.Serialize(jsonRpcPayload);
            var request = new HttpRequestMessage(HttpMethod.Post, "/")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            return request;
        }

        /// <summary>
        /// Extracts JSON-RPC data from an SSE (Server-Sent Events) response body.
        /// SSE format: "event: message\ndata: {json}\n\n"
        /// </summary>
        private static JsonDocument ParseSseResponse(string responseBody)
        {
            // Try plain JSON first
            try
            {
                return JsonDocument.Parse(responseBody);
            }
            catch (JsonException)
            {
                // Parse SSE format - extract the "data:" line
                var lines = responseBody.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("data: ", StringComparison.Ordinal))
                    {
                        var jsonData = line["data: ".Length..];
                        return JsonDocument.Parse(jsonData);
                    }
                }

                throw new InvalidOperationException(
                    $"Could not parse SSE response. Body: {responseBody}");
            }
        }

        [Fact]
        public async Task AcceptInitializeRequest()
        {
            // Arrange
            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "initialize",
                id = 1,
                @params = new
                {
                    protocolVersion = "2025-03-26",
                    capabilities = new { },
                    clientInfo = new { name = "integration-test", version = "1.0.0" }
                }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                "MCP endpoint should be mapped and accepting requests");
        }

        [Fact]
        public async Task ReturnJsonRpcResponse_ForInitialize()
        {
            // Arrange
            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "initialize",
                id = 42,
                @params = new
                {
                    protocolVersion = "2025-03-26",
                    capabilities = new { },
                    clientInfo = new { name = "integration-test", version = "1.0.0" }
                }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseJson = ParseSseResponse(responseBody);
            responseJson.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
            responseJson.RootElement.GetProperty("id").GetInt32().Should().Be(42);
            responseJson.RootElement.TryGetProperty("result", out var result).Should().BeTrue();
            result.TryGetProperty("serverInfo", out _).Should().BeTrue();
        }

        [Fact]
        public async Task ListRegisteredTools_ViaToolsListRequest()
        {
            // Arrange
            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "tools/list",
                id = 2,
                @params = new { }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseJson = ParseSseResponse(responseBody);
            responseJson.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");

            var result = responseJson.RootElement.GetProperty("result");
            var tools = result.GetProperty("tools");
            tools.GetArrayLength().Should().BeGreaterThan(0, "MCP server should register tools from assembly");
        }

        [Fact]
        public async Task RegisterAllExpectedTools()
        {
            // Arrange
            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "tools/list",
                id = 3,
                @params = new { }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            var responseJson = ParseSseResponse(responseBody);
            var tools = responseJson.RootElement.GetProperty("result").GetProperty("tools");

            var toolNames = new List<string>();
            foreach (var tool in tools.EnumerateArray())
            {
                toolNames.Add(tool.GetProperty("name").GetString()!);
            }

            // 4 domains x 3 tools each = 12 tools (SDK uses snake_case names)
            toolNames.Should().HaveCount(12);

            var expectedToolNames = new[]
            {
                "get_activity_by_date", "get_activity_by_date_range", "get_activity_records",
                "get_food_by_date", "get_food_by_date_range", "get_food_records",
                "get_sleep_by_date", "get_sleep_by_date_range", "get_sleep_records",
                "get_vitals_by_date", "get_vitals_by_date_range", "get_vitals_records"
            };
            toolNames.Should().BeEquivalentTo(expectedToolNames);
        }

        [Fact]
        public async Task InvokeToolSuccessfully_WhenCalledViaProtocol()
        {
            // Arrange - Set up mock response for activity endpoint
            _fixture.Factory.MockApiHandler.WithJsonResponse("/activity/2025-01-15", """
            {
                "id": "test-123",
                "activity": {},
                "date": "2025-01-15",
                "documentType": "activity"
            }
            """);

            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                id = 4,
                @params = new
                {
                    name = "get_activity_by_date",
                    arguments = new { date = "2025-01-15" }
                }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseJson = ParseSseResponse(responseBody);
            responseJson.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
            responseJson.RootElement.GetProperty("id").GetInt32().Should().Be(4);

            var result = responseJson.RootElement.GetProperty("result");
            result.TryGetProperty("content", out var contentArray).Should().BeTrue();
            contentArray.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ReturnErrorContent_WhenToolCalledWithInvalidDate()
        {
            // Arrange
            var request = CreateMcpRequest(new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                id = 5,
                @params = new
                {
                    name = "get_activity_by_date",
                    arguments = new { date = "not-a-date" }
                }
            });

            // Act
            var response = await _fixture.Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            responseBody.Should().Contain("error");
        }
    }
}

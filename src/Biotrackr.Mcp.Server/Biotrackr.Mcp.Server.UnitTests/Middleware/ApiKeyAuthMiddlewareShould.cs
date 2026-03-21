using Biotrackr.Mcp.Server.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Mcp.Server.UnitTests.Middleware
{
    public class ApiKeyAuthMiddlewareShould
    {
        private const string ValidApiKey = "test-api-key-12345";

        private readonly Mock<ILogger<ApiKeyAuthMiddleware>> _loggerMock;

        public ApiKeyAuthMiddlewareShould()
        {
            _loggerMock = new Mock<ILogger<ApiKeyAuthMiddleware>>();
        }

        private ApiKeyAuthMiddleware CreateMiddleware(string? configuredApiKey, RequestDelegate? next = null)
        {
            next ??= _ => Task.CompletedTask;

            var configData = new Dictionary<string, string?>();
            if (configuredApiKey is not null)
            {
                configData["mcpserverapikey"] = configuredApiKey;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            return new ApiKeyAuthMiddleware(next, configuration, _loggerMock.Object);
        }

        private static HttpContext CreateHttpContext(string path, string? apiKey = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();

            if (apiKey is not null)
            {
                context.Request.Headers["X-Api-Key"] = apiKey;
            }

            return context;
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturn401_WhenApiKeyHeaderIsMissing()
        {
            // Arrange
            var middleware = CreateMiddleware(ValidApiKey);
            var context = CreateHttpContext("/mcp");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturn401_WhenApiKeyHeaderIsInvalid()
        {
            // Arrange
            var middleware = CreateMiddleware(ValidApiKey);
            var context = CreateHttpContext("/mcp", apiKey: "wrong-key");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCallNext_WhenApiKeyHeaderIsValid()
        {
            // Arrange
            var nextCalled = false;
            var middleware = CreateMiddleware(ValidApiKey, next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var context = CreateHttpContext("/mcp", apiKey: ValidApiKey);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_ShouldBypassAuth_ForHealthCheckEndpoint()
        {
            // Arrange
            var nextCalled = false;
            var middleware = CreateMiddleware(ValidApiKey, next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var context = CreateHttpContext("/api/healthz");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAllowAllRequests_WhenNoApiKeyIsConfigured()
        {
            // Arrange
            var nextCalled = false;
            var middleware = CreateMiddleware(configuredApiKey: null, next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var context = CreateHttpContext("/mcp");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAllowAllRequests_WhenApiKeyIsEmptyString()
        {
            // Arrange
            var nextCalled = false;
            var middleware = CreateMiddleware(configuredApiKey: "", next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var context = CreateHttpContext("/mcp");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogWarning_WhenApiKeyIsInvalid()
        {
            // Arrange
            var middleware = CreateMiddleware(ValidApiKey);
            var context = CreateHttpContext("/mcp", apiKey: "wrong-key");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _loggerMock.VerifyLog(l => l.LogWarning(
                It.Is<string>(s => s.Contains("missing or invalid API key"))),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotCallNext_WhenApiKeyIsInvalid()
        {
            // Arrange
            var nextCalled = false;
            var middleware = CreateMiddleware(ValidApiKey, next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var context = CreateHttpContext("/mcp", apiKey: "wrong-key");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_ShouldWriteUnauthorizedJsonResponse_WhenRejected()
        {
            // Arrange
            var middleware = CreateMiddleware(ValidApiKey);
            var context = CreateHttpContext("/mcp");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            body.Should().Contain("Unauthorized");
        }
    }
}

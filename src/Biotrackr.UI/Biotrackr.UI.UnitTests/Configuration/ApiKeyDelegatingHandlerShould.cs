using System.Net;
using Biotrackr.UI.Configuration;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.UI.UnitTests.Configuration
{
    public class ApiKeyDelegatingHandlerShould
    {
        [Fact]
        public async Task SendAsync_ShouldAddSubscriptionKeyHeader_WhenKeyIsConfigured()
        {
            // Arrange
            var settings = new BiotrackrApiSettings { SubscriptionKey = "test-key-123" };
            var optionsMock = new Mock<IOptions<BiotrackrApiSettings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            var innerHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new ApiKeyDelegatingHandler(optionsMock.Object)
            {
                InnerHandler = innerHandler
            };
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };

            // Act
            await client.GetAsync("/test");

            // Assert
            innerHandler.LastRequest!.Headers.GetValues("Ocp-Apim-Subscription-Key")
                .Should().ContainSingle().Which.Should().Be("test-key-123");
        }

        [Fact]
        public async Task SendAsync_ShouldNotAddHeader_WhenKeyIsNull()
        {
            // Arrange
            var settings = new BiotrackrApiSettings { SubscriptionKey = null };
            var optionsMock = new Mock<IOptions<BiotrackrApiSettings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            var innerHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new ApiKeyDelegatingHandler(optionsMock.Object)
            {
                InnerHandler = innerHandler
            };
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };

            // Act
            await client.GetAsync("/test");

            // Assert
            innerHandler.LastRequest!.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldNotAddHeader_WhenKeyIsEmpty()
        {
            // Arrange
            var settings = new BiotrackrApiSettings { SubscriptionKey = "" };
            var optionsMock = new Mock<IOptions<BiotrackrApiSettings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            var innerHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new ApiKeyDelegatingHandler(optionsMock.Object)
            {
                InnerHandler = innerHandler
            };
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };

            // Act
            await client.GetAsync("/test");

            // Assert
            innerHandler.LastRequest!.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeFalse();
        }
    }
}

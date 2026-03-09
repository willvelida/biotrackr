using System.Net;
using System.Text;
using System.Text.Json;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Chat;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.UI.UnitTests.Services
{
    public class ChatApiServiceShould
    {
        private readonly Mock<ILogger<ChatApiService>> _loggerMock;

        public ChatApiServiceShould()
        {
            _loggerMock = new Mock<ILogger<ChatApiService>>();
        }

        private ChatApiService CreateSut(HttpResponseMessage response)
        {
            var handler = new MockHttpMessageHandler(response);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com/chat/") };
            return new ChatApiService(httpClient, _loggerMock.Object);
        }

        private ChatApiService CreateSut(Exception exception)
        {
            var handler = new MockHttpMessageHandler(exception);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com/chat/") };
            return new ChatApiService(httpClient, _loggerMock.Object);
        }

        private static HttpResponseMessage CreateSuccessResponse<T>(T data) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(data))
        };

        private static HttpResponseMessage CreateNotFoundResponse() => new(HttpStatusCode.NotFound);

        private static HttpResponseMessage CreateSseResponse(params string[] events)
        {
            var sseContent = new StringBuilder();
            foreach (var evt in events)
            {
                sseContent.AppendLine($"data: {evt}");
                sseContent.AppendLine();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sseContent.ToString(), Encoding.UTF8, "text/event-stream")
            };
        }

        // GetConversationsAsync tests
        [Fact]
        public async Task GetConversationsAsync_ReturnsConversations_WhenApiReturnsSuccess()
        {
            var expected = new PaginatedResponse<ChatConversationSummary>
            {
                Items = [new ChatConversationSummary { SessionId = "abc", Title = "Test convo" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetConversationsAsync();

            result.Items.Should().HaveCount(1);
            result.Items[0].SessionId.Should().Be("abc");
            result.Items[0].Title.Should().Be("Test convo");
        }

        [Fact]
        public async Task GetConversationsAsync_ReturnsEmptyResponse_WhenApiReturnsError()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await sut.GetConversationsAsync();

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationsAsync_ReturnsEmptyResponse_WhenNetworkError()
        {
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            var result = await sut.GetConversationsAsync();

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationsAsync_ClampsPaginationParameters()
        {
            var expected = new PaginatedResponse<ChatConversationSummary> { Items = [], TotalCount = 0 };
            var handler = new MockHttpMessageHandler(CreateSuccessResponse(expected));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com/chat/") };
            var sut = new ChatApiService(httpClient, _loggerMock.Object);

            await sut.GetConversationsAsync(pageNumber: 0, pageSize: 200);

            handler.LastRequest!.RequestUri!.ToString().Should().Contain("pageNumber=1");
            handler.LastRequest.RequestUri.ToString().Should().Contain("pageSize=100");
        }

        // GetConversationAsync tests
        [Fact]
        public async Task GetConversationAsync_ReturnsConversation_WhenFound()
        {
            var expected = new ChatConversationDocument
            {
                Id = "abc",
                SessionId = "abc",
                Title = "Test",
                Messages =
                [
                    new ChatMessage { Role = "user", Content = "Hello" },
                    new ChatMessage { Role = "assistant", Content = "Hi there", ToolCalls = ["GetActivityByDate"] }
                ]
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetConversationAsync("abc");

            result.Should().NotBeNull();
            result!.SessionId.Should().Be("abc");
            result.Messages.Should().HaveCount(2);
            result.Messages[1].ToolCalls.Should().Contain("GetActivityByDate");
        }

        [Fact]
        public async Task GetConversationAsync_ReturnsNull_WhenNotFound()
        {
            var sut = CreateSut(CreateNotFoundResponse());

            var result = await sut.GetConversationAsync("nonexistent");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetConversationAsync_ReturnsNull_WhenNetworkError()
        {
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            var result = await sut.GetConversationAsync("abc");

            result.Should().BeNull();
        }

        // DeleteConversationAsync tests
        [Fact]
        public async Task DeleteConversationAsync_CompletesSuccessfully()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NoContent));

            var act = () => sut.DeleteConversationAsync("abc");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DeleteConversationAsync_LogsWarning_WhenApiReturnsError()
        {
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            await sut.DeleteConversationAsync("abc");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InternalServerError")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteConversationAsync_HandlesNetworkError()
        {
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            var act = () => sut.DeleteConversationAsync("abc");

            await act.Should().NotThrowAsync();
        }

        // SendMessageAsync tests
        [Fact]
        public async Task SendMessageAsync_YieldsEvents_WhenApiStreamsSSE()
        {
            var events = new[]
            {
                JsonSerializer.Serialize(new { type = "RUN_STARTED", runId = "r1", threadId = "t1" }),
                JsonSerializer.Serialize(new { type = "TEXT_MESSAGE_START", messageId = "m1", role = "assistant" }),
                JsonSerializer.Serialize(new { type = "TEXT_MESSAGE_CONTENT", messageId = "m1", delta = "Hello" }),
                JsonSerializer.Serialize(new { type = "TEXT_MESSAGE_CONTENT", messageId = "m1", delta = " world" }),
                JsonSerializer.Serialize(new { type = "TEXT_MESSAGE_END", messageId = "m1" }),
                JsonSerializer.Serialize(new { type = "RUN_FINISHED", runId = "r1", threadId = "t1" })
            };
            var sut = CreateSut(CreateSseResponse(events));

            var results = new List<AGUIEvent>();
            await foreach (var evt in sut.SendMessageAsync(null, "Hi"))
            {
                results.Add(evt);
            }

            results.Should().HaveCount(6);
            results[0].Type.Should().Be("RUN_STARTED");
            results[0].ThreadId.Should().Be("t1");
            results[2].Type.Should().Be("TEXT_MESSAGE_CONTENT");
            results[2].Delta.Should().Be("Hello");
            results[3].Delta.Should().Be(" world");
            results[5].Type.Should().Be("RUN_FINISHED");
        }

        [Fact]
        public async Task SendMessageAsync_YieldsNoEvents_WhenStreamEmpty()
        {
            var sut = CreateSut(CreateSseResponse());

            var results = new List<AGUIEvent>();
            await foreach (var evt in sut.SendMessageAsync(null, "Hi"))
            {
                results.Add(evt);
            }

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SendMessageAsync_HandlesNetworkError()
        {
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            var results = new List<AGUIEvent>();
            await foreach (var evt in sut.SendMessageAsync(null, "Hi"))
            {
                results.Add(evt);
            }

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SendMessageAsync_SkipsInvalidJsonLines()
        {
            var sseContent = "data: {\"type\":\"RUN_STARTED\"}\n\ndata: not-valid-json\n\ndata: {\"type\":\"RUN_FINISHED\"}\n\n";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
            };
            var sut = CreateSut(response);

            var results = new List<AGUIEvent>();
            await foreach (var evt in sut.SendMessageAsync(null, "Hi"))
            {
                results.Add(evt);
            }

            results.Should().HaveCount(2);
            results[0].Type.Should().Be("RUN_STARTED");
            results[1].Type.Should().Be("RUN_FINISHED");
        }

        [Fact]
        public async Task SendMessageAsync_IncludesConversationId_InRequest()
        {
            var handler = new MockHttpMessageHandler(CreateSseResponse());
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com/chat/") };
            var sut = new ChatApiService(httpClient, _loggerMock.Object);

            await foreach (var _ in sut.SendMessageAsync("session-123", "Hello")) { }

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
            var body = await handler.LastRequest.Content!.ReadAsStringAsync();
            body.Should().Contain("session-123");
        }

        // Constructor validation tests
        [Fact]
        public void Constructor_ShouldThrow_WhenHttpClientIsNull()
        {
            var act = () => new ChatApiService(null!, _loggerMock.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenLoggerIsNull()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("https://test.api.com/chat/") };

            var act = () => new ChatApiService(httpClient, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
    }
}

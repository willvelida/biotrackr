using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Middleware;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatConversationDocument = Biotrackr.Chat.Api.Models.ChatConversationDocument;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class ConversationPersistenceMiddlewareShould
    {
        private readonly Mock<IChatHistoryRepository> _repositoryMock;
        private readonly Mock<ILogger<ConversationPersistenceMiddleware>> _loggerMock;
        private readonly ConversationPersistenceMiddleware _sut;

        public ConversationPersistenceMiddlewareShould()
        {
            _repositoryMock = new Mock<IChatHistoryRepository>();
            _loggerMock = new Mock<ILogger<ConversationPersistenceMiddleware>>();

            _repositoryMock
                .Setup(r => r.SaveMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(new ChatConversationDocument());

            _sut = CreateSut();
        }

        private ConversationPersistenceMiddleware CreateSut(ConversationPolicyOptions? options = null)
        {
            var policyOptions = Options.Create(options ?? new ConversationPolicyOptions());
            return new ConversationPersistenceMiddleware(_repositoryMock.Object, policyOptions, _loggerMock.Object);
        }

        private static IEnumerable<ChatMessage> CreateMessages(string userText)
        {
            return [new ChatMessage(ChatRole.User, userText)];
        }

        [Fact]
        public async Task LogToolCallWithArguments_WhenFunctionCallContentIsYielded()
        {
            // Arrange
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15", ["page"] = 1 };
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate", arguments);
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert
            _loggerMock.VerifyLog(l => l.LogInformation(
                It.Is<string>(s => s.Contains("GetActivityByDate") && s.Contains("2026-01-15"))),
                Times.Once);
        }

        [Fact]
        public async Task LogNullArguments_WhenFunctionCallHasNoArguments()
        {
            // Arrange
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate");
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert
            _loggerMock.VerifyLog(l => l.LogInformation(
                It.Is<string>(s => s.Contains("GetActivityByDate") && s.Contains("null"))),
                Times.Once);
        }

        [Fact]
        public async Task NotPersistArgumentsToCosmosDb_WhenToolCallIsLogged()
        {
            // Arrange
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15" };
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate", arguments);
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert — only tool names are persisted, not arguments
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(),
                "assistant",
                It.IsAny<string>(),
                It.Is<List<string>?>(tc => tc != null && tc.Contains("GetActivityByDate") && tc.All(t => !t.Contains("2026-01-15")))),
                Times.Once);
        }

        [Fact]
        public void MaskSensitiveFields_ShouldReturnNull_WhenArgumentsAreNull()
        {
            var result = ConversationPersistenceMiddleware.MaskSensitiveFields(null);
            result.Should().Be("null");
        }

        [Fact]
        public void MaskSensitiveFields_ShouldSerializeArguments_WhenArgumentsAreProvided()
        {
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15", ["page"] = 1 };
            var result = ConversationPersistenceMiddleware.MaskSensitiveFields(arguments);
            result.Should().Contain("2026-01-15");
            result.Should().Contain("page");
        }

        [Fact]
        public async Task HandleAsync_ShouldContinueStreaming_WhenUserMessagePersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act — should not throw
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — streaming continued despite persistence failure
            updates.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HandleAsync_ShouldContinueStreaming_WhenAssistantPersistenceFails()
        {
            // Arrange — user persistence succeeds, assistant persistence fails
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB throttled"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act — should not throw
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — all agent updates were yielded before persistence was attempted
            updates.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HandleAsync_ShouldLogError_WhenUserMessagePersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(
                It.IsAny<InvalidOperationException>(),
                It.Is<string>(s => s.Contains("Failed to persist user message"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogError_WhenAssistantPersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB throttled"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(
                It.IsAny<InvalidOperationException>(),
                It.Is<string>(s => s.Contains("Failed to persist assistant response"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldHydrateConversationHistory_WhenPriorMessagesExist()
        {
            // Arrange — Cosmos has two prior messages (user + assistant) plus the newly saved user message
            var existingConversation = new ChatConversationDocument
            {
                SessionId = "test-session",
                Messages =
                [
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "How many steps on March 14th?" },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "assistant", Content = "You took 15,244 steps." },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "How many calories?" }
                ]
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(existingConversation);

            var capturingAgent = new MessageCapturingAgent(new TextContent("491 calories."));
            var messages = CreateMessages("How many calories?");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None))
            {
                // Drain
            }

            // Assert — agent should have received history + current message
            var receivedMessages = capturingAgent.CapturedMessages!.ToList();
            receivedMessages.Should().HaveCount(3);
            receivedMessages[0].Role.Should().Be(ChatRole.User);
            receivedMessages[0].Text.Should().Contain("How many steps on March 14th?");
            receivedMessages[1].Role.Should().Be(ChatRole.Assistant);
            receivedMessages[1].Text.Should().Contain("You took 15,244 steps.");
            receivedMessages[2].Role.Should().Be(ChatRole.User);
            receivedMessages[2].Text.Should().Contain("How many calories?");
        }

        [Fact]
        public async Task HandleAsync_ShouldNotHydrateHistory_WhenNoConversationExists()
        {
            // Arrange — Cosmos returns null (first message in a new conversation)
            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync((ChatConversationDocument?)null);

            var capturingAgent = new MessageCapturingAgent(new TextContent("Hello!"));
            var messages = CreateMessages("Hi there");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None))
            {
                // Drain
            }

            // Assert — agent should only receive the current message
            var receivedMessages = capturingAgent.CapturedMessages!.ToList();
            receivedMessages.Should().HaveCount(1);
            receivedMessages[0].Text.Should().Contain("Hi there");
        }

        [Fact]
        public async Task HandleAsync_ShouldContinueWithoutHistory_WhenHydrationFails()
        {
            // Arrange — Cosmos throws on GetConversationAsync
            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var capturingAgent = new MessageCapturingAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act — should not throw
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — streaming continued with just the current message
            updates.Should().NotBeEmpty();
            var receivedMessages = capturingAgent.CapturedMessages!.ToList();
            receivedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogError_WhenHydrationFails()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var capturingAgent = new MessageCapturingAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(
                It.IsAny<InvalidOperationException>(),
                It.Is<string>(s => s.Contains("Failed to hydrate conversation history"))),
                Times.Once);
        }

        // ===== ASI06 Control: Context Window Cap =====

        [Fact]
        public async Task HandleAsync_ShouldCapHydratedMessages_WhenHistoryExceedsMaxHydration()
        {
            // Arrange — 10 messages in history, but cap is 4
            var existingConversation = new ChatConversationDocument
            {
                SessionId = "test-session",
                Messages = Enumerable.Range(1, 10)
                    .Select(i => new Biotrackr.Chat.Api.Models.ChatMessage
                    {
                        Role = i % 2 == 1 ? "user" : "assistant",
                        Content = $"Message {i}"
                    })
                    .Append(new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "Latest question" })
                    .ToList()
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(existingConversation);

            var sut = CreateSut(new ConversationPolicyOptions { MaxHydrationMessageCount = 4 });
            var capturingAgent = new MessageCapturingAgent(new TextContent("Response"));
            var messages = CreateMessages("Latest question");

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None)) { }

            // Assert — only last 4 historical + 1 current = 5 messages
            var receivedMessages = capturingAgent.CapturedMessages!.ToList();
            receivedMessages.Should().HaveCount(5);
            // First hydrated message should be #7 (the 7th of the 10 historical messages)
            receivedMessages[0].Text.Should().Contain("Message 7");
            receivedMessages[3].Text.Should().Contain("Message 10");
            receivedMessages[4].Text.Should().Contain("Latest question");
        }

        [Fact]
        public async Task HandleAsync_ShouldHydrateAllMessages_WhenHistoryIsUnderCap()
        {
            // Arrange — 4 messages in history, cap is 50
            var existingConversation = new ChatConversationDocument
            {
                SessionId = "test-session",
                Messages =
                [
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "Q1" },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "assistant", Content = "A1" },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "Q2" },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "assistant", Content = "A2" },
                    new Biotrackr.Chat.Api.Models.ChatMessage { Role = "user", Content = "Q3" }
                ]
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(existingConversation);

            var capturingAgent = new MessageCapturingAgent(new TextContent("A3"));
            var messages = CreateMessages("Q3");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, capturingAgent, CancellationToken.None)) { }

            // Assert — all 4 historical + 1 current = 5 messages
            var receivedMessages = capturingAgent.CapturedMessages!.ToList();
            receivedMessages.Should().HaveCount(5);
        }

        // ===== ASI06 Control: Per-Message Content Length Limit =====

        [Fact]
        public async Task HandleAsync_ShouldTruncateUserMessage_WhenContentExceedsMaxLength()
        {
            // Arrange
            var sut = CreateSut(new ConversationPolicyOptions { MaxMessageContentLength = 20 });
            var longMessage = new string('A', 50);
            var agent = new FakeAgent(new TextContent("Short response."));
            var messages = CreateMessages(longMessage);

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — persisted content should be truncated to 20 chars
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(),
                "user",
                It.Is<string>(s => s.Length == 20),
                null),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotTruncateUserMessage_WhenContentIsUnderMaxLength()
        {
            // Arrange
            var agent = new FakeAgent(new TextContent("Response."));
            var messages = CreateMessages("Short message");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — full content persisted
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(),
                "user",
                "Short message",
                null),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldTruncateAssistantResponse_WhenContentExceedsMaxLength()
        {
            // Arrange
            var longResponse = new string('B', 50);
            var sut = CreateSut(new ConversationPolicyOptions { MaxMessageContentLength = 20 });
            var agent = new FakeAgent(new TextContent(longResponse));
            var messages = CreateMessages("Question");

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — persisted assistant content should be truncated to 20 chars
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(),
                "assistant",
                It.Is<string>(s => s.Length == 20),
                null),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogWarning_WhenContentIsTruncated()
        {
            // Arrange
            var sut = CreateSut(new ConversationPolicyOptions { MaxMessageContentLength = 10 });
            var agent = new FakeAgent(new TextContent("Short."));
            var messages = CreateMessages(new string('X', 50));

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogWarning(
                It.Is<string>(s => s.Contains("truncated"))),
                Times.AtLeastOnce);
        }

        // ===== ASI06 Control: Conversation Message Count Cap =====

        [Fact]
        public async Task HandleAsync_ShouldNotPersistMessages_WhenConversationCapReached()
        {
            // Arrange — conversation already has max messages
            var fullConversation = new ChatConversationDocument
            {
                SessionId = "full-session",
                Messages = Enumerable.Range(1, 100)
                    .Select(i => new Biotrackr.Chat.Api.Models.ChatMessage
                    {
                        Role = i % 2 == 1 ? "user" : "assistant",
                        Content = $"Message {i}"
                    })
                    .ToList()
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(fullConversation);

            var sut = CreateSut(new ConversationPolicyOptions { MaxMessagesPerConversation = 100 });
            var agent = new FakeAgent(new TextContent("Still answering."));
            var messages = CreateMessages("One more question");

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — no new messages should be persisted
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), It.IsAny<List<string>?>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldStillStreamAgentResponse_WhenConversationCapReached()
        {
            // Arrange
            var fullConversation = new ChatConversationDocument
            {
                SessionId = "full-session",
                Messages = Enumerable.Range(1, 100)
                    .Select(i => new Biotrackr.Chat.Api.Models.ChatMessage
                    {
                        Role = i % 2 == 1 ? "user" : "assistant",
                        Content = $"Message {i}"
                    })
                    .ToList()
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(fullConversation);

            var sut = CreateSut(new ConversationPolicyOptions { MaxMessagesPerConversation = 100 });
            var agent = new FakeAgent(new TextContent("Still answering."));
            var messages = CreateMessages("One more question");

            // Act — agent should still respond even though we won't persist
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — agent response is streamed + conversation cap notice appended
            updates.Should().HaveCountGreaterThanOrEqualTo(2);
            var lastUpdate = updates.Last();
            var capNotice = lastUpdate.Contents.OfType<TextContent>().FirstOrDefault();
            capNotice.Should().NotBeNull();
            capNotice!.Text.Should().Contain("maximum length");
        }

        [Fact]
        public async Task HandleAsync_ShouldLogWarning_WhenConversationCapReached()
        {
            // Arrange
            var fullConversation = new ChatConversationDocument
            {
                SessionId = "full-session",
                Messages = Enumerable.Range(1, 50)
                    .Select(i => new Biotrackr.Chat.Api.Models.ChatMessage
                    {
                        Role = i % 2 == 1 ? "user" : "assistant",
                        Content = $"Message {i}"
                    })
                    .ToList()
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(fullConversation);

            var sut = CreateSut(new ConversationPolicyOptions { MaxMessagesPerConversation = 50 });
            var agent = new FakeAgent(new TextContent("Response."));
            var messages = CreateMessages("Question");

            // Act
            await foreach (var _ in sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogWarning(
                It.Is<string>(s => s.Contains("Conversation cap"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldPersistNormally_WhenUnderConversationCap()
        {
            // Arrange — conversation has 10 messages, cap is 100
            var conversation = new ChatConversationDocument
            {
                SessionId = "normal-session",
                Messages = Enumerable.Range(1, 10)
                    .Select(i => new Biotrackr.Chat.Api.Models.ChatMessage
                    {
                        Role = i % 2 == 1 ? "user" : "assistant",
                        Content = $"Message {i}"
                    })
                    .ToList()
            };

            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(conversation);

            var agent = new FakeAgent(new TextContent("Response."));
            var messages = CreateMessages("Question");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — both user and assistant messages should be persisted
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null), Times.Once);
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldAssumeCapNotReached_WhenCapCheckFails()
        {
            // Arrange — GetConversationAsync throws on first call (cap check), returns null on second (hydration)
            var callCount = 0;
            _repositoryMock
                .Setup(r => r.GetConversationAsync(It.IsAny<string>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1) throw new InvalidOperationException("Cosmos DB error");
                    return null;
                });

            var agent = new FakeAgent(new TextContent("Response."));
            var messages = CreateMessages("Question");

            // Act — should not throw, should persist normally
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert — messages persisted since cap check failed gracefully
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null), Times.Once);
        }

        /// <summary>
        /// Concrete AIAgent subclass for testing that yields preconfigured content.
        /// </summary>
        private sealed class FakeAgent(params AIContent[] contents) : AIAgent
        {
            protected override Task<AgentResponse> RunCoreAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, contents)));
            }

            protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new AgentResponseUpdate(ChatRole.Assistant, contents);
                await Task.CompletedTask;
            }

            protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
                JsonElement sessionData,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
                AgentSession session,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<JsonElement>(JsonDocument.Parse("{}").RootElement);
            }
        }

        private sealed class FakeSession : AgentSession;

        /// <summary>
        /// Agent that captures the messages it receives for assertion, then yields preconfigured content.
        /// </summary>
        private sealed class MessageCapturingAgent(params AIContent[] contents) : AIAgent
        {
            public IEnumerable<ChatMessage>? CapturedMessages { get; private set; }

            protected override Task<AgentResponse> RunCoreAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                CancellationToken cancellationToken)
            {
                CapturedMessages = messages.ToList();
                return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, contents)));
            }

            protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                CapturedMessages = messages.ToList();
                yield return new AgentResponseUpdate(ChatRole.Assistant, contents);
                await Task.CompletedTask;
            }

            protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
                JsonElement sessionData,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
                AgentSession session,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<JsonElement>(JsonDocument.Parse("{}").RootElement);
            }
        }
    }
}

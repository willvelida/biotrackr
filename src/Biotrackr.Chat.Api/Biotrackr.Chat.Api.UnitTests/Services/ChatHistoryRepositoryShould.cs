using AutoFixture;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace Biotrackr.Chat.Api.UnitTests.Services
{
    public class ChatHistoryRepositoryShould
    {
        private readonly Mock<ICosmosClientFactory> _cosmosClientFactoryMock;
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<Container> _containerMock;
        private readonly Mock<ILogger<ChatHistoryRepository>> _loggerMock;
        private readonly ChatHistoryRepository _sut;

        public ChatHistoryRepositoryShould()
        {
            _cosmosClientFactoryMock = new Mock<ICosmosClientFactory>();
            _cosmosClientMock = new Mock<CosmosClient>();
            _containerMock = new Mock<Container>();
            _loggerMock = new Mock<ILogger<ChatHistoryRepository>>();

            var settings = new Settings
            {
                DatabaseName = "test-db",
                ConversationsContainerName = "conversations"
            };
            var options = Options.Create(settings);

            _cosmosClientFactoryMock.Setup(x => x.Create())
                .Returns(_cosmosClientMock.Object);

            _cosmosClientMock.Setup(x => x.GetContainer("test-db", "conversations"))
                .Returns(_containerMock.Object);

            _sut = new ChatHistoryRepository(_cosmosClientFactoryMock.Object, options, _loggerMock.Object);
        }

        [Fact]
        public async Task GetConversationAsync_ShouldReturnDocument_WhenFound()
        {
            // Arrange
            var fixture = new Fixture();
            var conversation = fixture.Create<ChatConversationDocument>();
            var sessionId = conversation.SessionId;

            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            responseMock.Setup(x => x.Resource).Returns(conversation);

            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _sut.GetConversationAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result!.SessionId.Should().Be(sessionId);
        }

        [Fact]
        public async Task GetConversationAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var sessionId = "nonexistent";
            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _sut.GetConversationAsync(sessionId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetConversationsAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            var fixture = new Fixture();
            var summaries = fixture.CreateMany<ChatConversationSummary>(3).ToList();
            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            // Mock count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { 3 }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            // Mock data query
            var dataFeedResponse = new Mock<FeedResponse<ChatConversationSummary>>();
            dataFeedResponse.Setup(x => x.GetEnumerator()).Returns(summaries.GetEnumerator());

            var dataIterator = new Mock<FeedIterator<ChatConversationSummary>>();
            dataIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            dataIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(dataFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ChatConversationSummary>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(dataIterator.Object);

            // Act
            var result = await _sut.GetConversationsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(20);
        }

        [Fact]
        public async Task DeleteConversationAsync_ShouldCallDeleteItem()
        {
            // Arrange
            var sessionId = "session-to-delete";
            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();

            _containerMock.Setup(x => x.DeleteItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            await _sut.DeleteConversationAsync(sessionId);

            // Assert
            _containerMock.Verify(x => x.DeleteItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default), Times.Once);
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldCreateNewConversation_WhenNotFound()
        {
            // Arrange
            var sessionId = "new-session";
            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _sut.SaveMessageAsync(sessionId, "user", "Hello");

            // Assert
            result.Should().NotBeNull();
            result.SessionId.Should().Be(sessionId);
            result.Messages.Should().HaveCount(1);
            result.Messages[0].Role.Should().Be("user");
            result.Messages[0].Content.Should().Be("Hello");
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldAutoTitle_FromFirstUserMessage()
        {
            // Arrange
            var sessionId = "new-session";
            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _sut.SaveMessageAsync(sessionId, "user", "What were my steps yesterday?");

            // Assert
            result.Title.Should().Be("What were my steps yesterday?");
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldTruncateTitle_WhenMessageIsLong()
        {
            // Arrange
            var sessionId = "new-session";
            var longMessage = new string('a', 100);
            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _sut.SaveMessageAsync(sessionId, "user", longMessage);

            // Assert
            result.Title.Should().HaveLength(53); // 50 chars + "..."
            result.Title.Should().EndWith("...");
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldAppendToExisting_WhenConversationExists()
        {
            // Arrange
            var sessionId = "existing-session";
            var existingConversation = new ChatConversationDocument
            {
                Id = sessionId,
                SessionId = sessionId,
                Title = "Existing conversation",
                Messages = [new ChatMessage { Role = "user", Content = "First message" }]
            };

            var readResponseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            readResponseMock.Setup(x => x.Resource).Returns(existingConversation);

            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ReturnsAsync(readResponseMock.Object);

            var upsertResponseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .ReturnsAsync(upsertResponseMock.Object);

            // Act
            var result = await _sut.SaveMessageAsync(sessionId, "assistant", "Here is your data.");

            // Assert
            result.Messages.Should().HaveCount(2);
            result.Messages[1].Role.Should().Be("assistant");
            result.Title.Should().Be("Existing conversation"); // Title should not change
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldSetTtl_FromSettings()
        {
            // Arrange
            var sessionId = "ttl-session";
            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            var responseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            ChatConversationDocument? capturedDocument = null;
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .Callback<ChatConversationDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>(
                    (doc, _, _, _) => capturedDocument = doc)
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _sut.SaveMessageAsync(sessionId, "user", "Hello");

            // Assert — TTL should match the default settings value (7,776,000 seconds = 90 days)
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Ttl.Should().Be(7_776_000);
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldRefreshTtl_WhenAppendingToExistingConversation()
        {
            // Arrange
            var sessionId = "existing-ttl-session";
            var existingConversation = new ChatConversationDocument
            {
                Id = sessionId,
                SessionId = sessionId,
                Title = "Existing conversation",
                Ttl = 1000, // Old TTL value
                Messages = [new ChatMessage { Role = "user", Content = "First message" }]
            };

            var readResponseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            readResponseMock.Setup(x => x.Resource).Returns(existingConversation);

            _containerMock.Setup(x => x.ReadItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId), null, default))
                .ReturnsAsync(readResponseMock.Object);

            ChatConversationDocument? capturedDocument = null;
            var upsertResponseMock = new Mock<ItemResponse<ChatConversationDocument>>();
            _containerMock.Setup(x => x.UpsertItemAsync(
                It.IsAny<ChatConversationDocument>(),
                It.IsAny<PartitionKey>(),
                null, default))
                .Callback<ChatConversationDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>(
                    (doc, _, _, _) => capturedDocument = doc)
                .ReturnsAsync(upsertResponseMock.Object);

            // Act
            await _sut.SaveMessageAsync(sessionId, "assistant", "Here is your data.");

            // Assert — TTL should be refreshed to the configured value, not the old value
            capturedDocument.Should().NotBeNull();
            capturedDocument!.Ttl.Should().Be(7_776_000);
        }
    }
}

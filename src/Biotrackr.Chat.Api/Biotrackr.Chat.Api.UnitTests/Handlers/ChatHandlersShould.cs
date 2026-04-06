using AutoFixture;
using Biotrackr.Chat.Api.Handlers;
using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Handlers
{
    public class ChatHandlersShould
    {
        private readonly Mock<IChatHistoryRepository> _repositoryMock;

        public ChatHandlersShould()
        {
            _repositoryMock = new Mock<IChatHistoryRepository>();
        }

        [Fact]
        public async Task GetConversations_ShouldReturnPaginatedResult()
        {
            // Arrange
            var fixture = new Fixture();
            var summaries = fixture.CreateMany<ChatConversationSummary>(5).ToList();
            var paginatedResponse = new PaginationResponse<ChatConversationSummary>
            {
                Items = summaries,
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 5
            };

            _repositoryMock.Setup(x => x.GetConversationsAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ChatHandlers.GetConversations(_repositoryMock.Object, 1, 20);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ChatConversationSummary>>>();
            var okResult = result as Ok<PaginationResponse<ChatConversationSummary>>;
            okResult!.Value!.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetConversations_ShouldUseDefaults_WhenNoParametersProvided()
        {
            // Arrange
            var paginatedResponse = new PaginationResponse<ChatConversationSummary>
            {
                Items = [],
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 0
            };

            _repositoryMock.Setup(x => x.GetConversationsAsync(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 20)))
                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ChatHandlers.GetConversations(_repositoryMock.Object, null, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ChatConversationSummary>>>();
            _repositoryMock.Verify(x => x.GetConversationsAsync(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetConversation_ShouldReturnOk_WhenConversationIsFound()
        {
            // Arrange
            var fixture = new Fixture();
            var conversation = fixture.Create<ChatConversationDocument>();
            var sessionId = conversation.SessionId;

            _repositoryMock.Setup(x => x.GetConversationAsync(sessionId))
                .ReturnsAsync(conversation);

            // Act
            var result = await ChatHandlers.GetConversation(_repositoryMock.Object, sessionId);

            // Assert
            result.Result.Should().BeOfType<Ok<ChatConversationDocument>>();
        }

        [Fact]
        public async Task GetConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
        {
            // Arrange
            var sessionId = "nonexistent-session";
            _repositoryMock.Setup(x => x.GetConversationAsync(sessionId))
                .ReturnsAsync((ChatConversationDocument?)null);

            // Act
            var result = await ChatHandlers.GetConversation(_repositoryMock.Object, sessionId);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task DeleteConversation_ShouldReturnNoContent()
        {
            // Arrange
            var sessionId = "session-to-delete";
            _repositoryMock.Setup(x => x.DeleteConversationAsync(sessionId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await ChatHandlers.DeleteConversation(_repositoryMock.Object, sessionId);

            // Assert
            result.Should().BeOfType<NoContent>();
            _repositoryMock.Verify(x => x.DeleteConversationAsync(sessionId), Times.Once);
        }
    }
}

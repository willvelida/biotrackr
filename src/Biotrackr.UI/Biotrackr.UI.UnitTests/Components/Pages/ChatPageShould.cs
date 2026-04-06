using Bunit;
using Moq;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Chat;
using Biotrackr.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Biotrackr.UI.Models.Chat.ChatMessage;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class ChatPageShould : BunitContext
    {
        private readonly Mock<IChatApiService> _mockChatService;

        public ChatPageShould()
        {
            _mockChatService = new Mock<IChatApiService>();
            Services.AddSingleton(_mockChatService.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderChatPageTitle()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("Conversations");
        }

        [Fact]
        public void RenderDisclaimer()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("not medical advice");
        }

        [Fact]
        public void RenderEmptyState_WhenNoMessages()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("Ask about your health data");
        }

        [Fact]
        public void RenderNoConversationsMessage_WhenEmpty()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("No conversations yet");
        }

        [Fact]
        public void RenderNewChatButton()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("+ New Chat");
        }

        [Fact]
        public void RenderConversationList_WhenConversationsExist()
        {
            var conversations = new PaginatedResponse<ChatConversationSummary>
            {
                Items =
                [
                    new ChatConversationSummary
                    {
                        SessionId = "session-1",
                        Title = "My first chat",
                        LastUpdated = new DateTime(2026, 3, 9, 10, 0, 0)
                    },
                    new ChatConversationSummary
                    {
                        SessionId = "session-2",
                        Title = "Steps question",
                        LastUpdated = new DateTime(2026, 3, 8, 15, 30, 0)
                    }
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 2,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(conversations);

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("My first chat");
            cut.Markup.Should().Contain("Steps question");
        }

        [Fact]
        public void RenderLoadMoreButton_WhenMoreConversationsAvailable()
        {
            var conversations = new PaginatedResponse<ChatConversationSummary>
            {
                Items =
                [
                    new ChatConversationSummary
                    {
                        SessionId = "session-1",
                        Title = "Chat 1",
                        LastUpdated = DateTime.UtcNow
                    }
                ],
                PageNumber = 1,
                TotalPages = 2,
                TotalCount = 25,
                HasPreviousPage = false,
                HasNextPage = true
            };

            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(conversations);

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("Load more");
        }

        [Fact]
        public void NotRenderLoadMoreButton_WhenNoMoreConversations()
        {
            var conversations = new PaginatedResponse<ChatConversationSummary>
            {
                Items =
                [
                    new ChatConversationSummary
                    {
                        SessionId = "session-1",
                        Title = "Chat 1",
                        LastUpdated = DateTime.UtcNow
                    }
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(conversations);

            var cut = Render<Chat>();

            cut.Markup.Should().NotContain("Load more");
        }

        [Fact]
        public void RenderSendButton()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("Send");
        }

        [Fact]
        public void RenderInputPlaceholder()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().Contain("Type a message...");
        }

        [Fact]
        public void RenderConversationDeleteButton()
        {
            var conversations = new PaginatedResponse<ChatConversationSummary>
            {
                Items =
                [
                    new ChatConversationSummary
                    {
                        SessionId = "session-1",
                        Title = "Test Chat",
                        LastUpdated = DateTime.UtcNow
                    }
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(conversations);

            var cut = Render<Chat>();

            cut.Find(".chat-sidebar-item-delete").Should().NotBeNull();
        }

        [Fact]
        public void RenderChatContainer()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Find(".chat-container").Should().NotBeNull();
            cut.Find(".chat-sidebar-panel").Should().NotBeNull();
            cut.Find(".chat-main").Should().NotBeNull();
        }

        [Fact]
        public void RenderMessageBubbles_WhenMessagesExist()
        {
            var conversation = new ChatConversationDocument
            {
                SessionId = "session-1",
                Title = "Test",
                Messages =
                [
                    new ChatMessage { Role = "user", Content = "How many steps?", Timestamp = DateTime.UtcNow },
                    new ChatMessage { Role = "assistant", Content = "You took 10,000 steps.", Timestamp = DateTime.UtcNow }
                ]
            };

            SetupConversationsWithLoad("session-1", conversation);

            var cut = Render<Chat>();

            cut.Find(".chat-sidebar-item-button").Click();

            cut.Markup.Should().Contain("How many steps?");
            cut.Markup.Should().Contain("You took 10,000 steps.");
        }

        [Fact]
        public void RenderToolCallBadges_WhenAssistantHasToolCalls()
        {
            var conversation = new ChatConversationDocument
            {
                SessionId = "session-1",
                Title = "Test",
                Messages =
                [
                    new ChatMessage { Role = "user", Content = "Get my activity", Timestamp = DateTime.UtcNow },
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content = "Here is your data.",
                        Timestamp = DateTime.UtcNow,
                        ToolCalls = ["GetActivity", "GetSteps"]
                    }
                ]
            };

            SetupConversationsWithLoad("session-1", conversation);

            var cut = Render<Chat>();

            cut.Find(".chat-sidebar-item-button").Click();

            cut.Markup.Should().Contain("GetActivity");
            cut.Markup.Should().Contain("GetSteps");
        }

        [Fact]
        public void RenderUserAndAgentMessageStyles()
        {
            var conversation = new ChatConversationDocument
            {
                SessionId = "session-1",
                Title = "Test",
                Messages =
                [
                    new ChatMessage { Role = "user", Content = "Hello", Timestamp = DateTime.UtcNow },
                    new ChatMessage { Role = "assistant", Content = "Hi there!", Timestamp = DateTime.UtcNow }
                ]
            };

            SetupConversationsWithLoad("session-1", conversation);

            var cut = Render<Chat>();

            cut.Find(".chat-sidebar-item-button").Click();

            cut.Find(".message-user").Should().NotBeNull();
            cut.Find(".message-agent").Should().NotBeNull();
        }

        [Fact]
        public void RenderMessageTimestamps()
        {
            var timestamp = new DateTime(2026, 3, 9, 14, 30, 0);
            var conversation = new ChatConversationDocument
            {
                SessionId = "session-1",
                Title = "Test",
                Messages =
                [
                    new ChatMessage { Role = "user", Content = "Test", Timestamp = timestamp }
                ]
            };

            SetupConversationsWithLoad("session-1", conversation);

            var cut = Render<Chat>();

            cut.Find(".chat-sidebar-item-button").Click();

            cut.Find(".message-timestamp").Should().NotBeNull();
            cut.Markup.Should().Contain("14:30");
        }

        [Fact]
        public void NotRenderReportProgressIndicator_WhenNotStreaming()
        {
            SetupEmptyConversations();

            var cut = Render<Chat>();

            cut.Markup.Should().NotContain("report-progress");
            cut.Markup.Should().NotContain("Generating report...");
        }

        private void SetupEmptyConversations()
        {
            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<ChatConversationSummary>
                {
                    Items = [],
                    PageNumber = 1,
                    TotalPages = 0,
                    TotalCount = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                });
        }

        private void SetupConversationsWithLoad(string sessionId, ChatConversationDocument document)
        {
            var conversations = new PaginatedResponse<ChatConversationSummary>
            {
                Items =
                [
                    new ChatConversationSummary
                    {
                        SessionId = sessionId,
                        Title = document.Title,
                        LastUpdated = DateTime.UtcNow
                    }
                ],
                PageNumber = 1,
                TotalPages = 1,
                TotalCount = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            _mockChatService.Setup(s => s.GetConversationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(conversations);
            _mockChatService.Setup(s => s.GetConversationAsync(sessionId))
                .ReturnsAsync(document);
        }
    }
}

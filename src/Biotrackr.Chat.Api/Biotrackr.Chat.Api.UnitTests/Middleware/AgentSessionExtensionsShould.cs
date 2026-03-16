using Biotrackr.Chat.Api.Middleware;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class AgentSessionExtensionsShould
    {
        private static ChatClientAgentRunOptions CreateRunOptions(string? threadId)
        {
            return new ChatClientAgentRunOptions
            {
                ChatOptions = new ChatOptions
                {
                    AdditionalProperties = new AdditionalPropertiesDictionary
                    {
                        ["ag_ui_thread_id"] = threadId
                    }
                }
            };
        }

        [Fact]
        public void ReturnThreadId_WhenPresentInRunOptions()
        {
            var options = CreateRunOptions("thread-abc-123");

            var result = options.GetConversationId();

            result.Should().Be("thread-abc-123");
        }

        [Fact]
        public void ReturnFallback_WhenOptionsAreNull()
        {
            AgentRunOptions? options = null;

            var result = options.GetConversationId("my-fallback");

            result.Should().Be("my-fallback");
        }

        [Fact]
        public void ReturnDefaultFallback_WhenOptionsAreNull()
        {
            AgentRunOptions? options = null;

            var result = options.GetConversationId();

            result.Should().Be("unknown");
        }

        [Fact]
        public void ReturnFallback_WhenOptionsAreNotChatClientAgentRunOptions()
        {
            var options = new AgentRunOptions();

            var result = options.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }

        [Fact]
        public void ReturnFallback_WhenThreadIdIsEmpty()
        {
            var options = CreateRunOptions("");

            var result = options.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }

        [Fact]
        public void ReturnFallback_WhenThreadIdIsNull()
        {
            var options = CreateRunOptions(null);

            var result = options.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }

        [Fact]
        public void ReturnFallback_WhenChatOptionsIsNull()
        {
            var options = new ChatClientAgentRunOptions { ChatOptions = null };

            var result = options.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }
    }
}

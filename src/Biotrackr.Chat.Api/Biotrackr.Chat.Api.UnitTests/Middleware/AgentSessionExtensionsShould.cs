using Biotrackr.Chat.Api.Middleware;
using FluentAssertions;
using Microsoft.Agents.AI;
using System.Reflection;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class AgentSessionExtensionsShould
    {
        /// <summary>
        /// Creates a <see cref="ChatClientAgentSession"/> via reflection because
        /// its constructors are internal to the Microsoft.Agents.AI package.
        /// </summary>
        private static ChatClientAgentSession CreateChatSession(string conversationId)
        {
            var ctor = typeof(ChatClientAgentSession).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(string), typeof(AgentSessionStateBag)]);

            return (ChatClientAgentSession)ctor!.Invoke([conversationId, new AgentSessionStateBag()]);
        }

        [Fact]
        public void ReturnConversationId_WhenSessionIsChatClientAgentSession()
        {
            var session = CreateChatSession("thread-abc-123");

            var result = session.GetConversationId();

            result.Should().Be("thread-abc-123");
        }

        [Fact]
        public void ReturnFallback_WhenSessionIsNull()
        {
            AgentSession? session = null;

            var result = session.GetConversationId("my-fallback");

            result.Should().Be("my-fallback");
        }

        [Fact]
        public void ReturnDefaultFallback_WhenSessionIsNull()
        {
            AgentSession? session = null;

            var result = session.GetConversationId();

            result.Should().Be("unknown");
        }

        [Fact]
        public void ReturnFallback_WhenSessionIsNotChatClientAgentSession()
        {
            var session = new FakeSession();

            var result = session.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }

        [Fact]
        public void ReturnFallback_WhenConversationIdIsEmpty()
        {
            var session = CreateChatSession("");

            var result = session.GetConversationId("fallback-id");

            result.Should().Be("fallback-id");
        }

        private sealed class FakeSession : AgentSession;
    }
}

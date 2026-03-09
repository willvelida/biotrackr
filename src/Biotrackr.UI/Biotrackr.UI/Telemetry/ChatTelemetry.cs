using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Biotrackr.UI.Telemetry
{
    public static class ChatTelemetry
    {
        private static readonly ActivitySource ActivitySource = new("Biotrackr.UI");
        private static readonly Meter Meter = new("Biotrackr.UI");

        // Counters
        public static readonly Counter<long> MessagesSent =
            Meter.CreateCounter<long>("chat.messages.sent", description: "Number of user messages sent to the chat agent");
        public static readonly Counter<long> MessagesReceived =
            Meter.CreateCounter<long>("chat.messages.received", description: "Number of completed agent responses");
        public static readonly Counter<long> StreamErrors =
            Meter.CreateCounter<long>("chat.stream.errors", description: "Number of SSE stream errors");
        public static readonly Counter<long> ToolCalls =
            Meter.CreateCounter<long>("chat.tool_calls", description: "Tool calls made by the chat agent");
        public static readonly Counter<long> ConversationsCreated =
            Meter.CreateCounter<long>("chat.conversations.created", description: "New conversations started");
        public static readonly Counter<long> ConversationsLoaded =
            Meter.CreateCounter<long>("chat.conversations.loaded", description: "Existing conversations loaded from sidebar");
        public static readonly Counter<long> ConversationsDeleted =
            Meter.CreateCounter<long>("chat.conversations.deleted", description: "Conversations deleted");

        // Histograms
        public static readonly Histogram<double> StreamDuration =
            Meter.CreateHistogram<double>("chat.stream.duration", "ms", "Total time from POST to RUN_FINISHED");
        public static readonly Histogram<double> TimeToFirstToken =
            Meter.CreateHistogram<double>("chat.stream.time_to_first_token", "ms", "Time from POST to first TEXT_MESSAGE_CONTENT event");
        public static readonly Histogram<int> TokenCount =
            Meter.CreateHistogram<int>("chat.stream.token_count", "{tokens}", "Number of TEXT_MESSAGE_CONTENT events per response");

        // Tracing
        public static Activity? StartSendMessage(string? sessionId, int messageLength)
        {
            var activity = ActivitySource.StartActivity("chat.send_message");
            activity?.SetTag("chat.session_id", sessionId ?? "new");
            activity?.SetTag("chat.is_new_conversation", sessionId is null);
            activity?.SetTag("chat.message_length", messageLength);
            return activity;
        }

        public static Activity? StartLoadConversation(string sessionId)
        {
            var activity = ActivitySource.StartActivity("chat.load_conversation");
            activity?.SetTag("chat.session_id", sessionId);
            return activity;
        }
    }
}

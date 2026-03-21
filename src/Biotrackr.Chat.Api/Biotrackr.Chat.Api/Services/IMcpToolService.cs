using Microsoft.Extensions.AI;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Manages MCP client lifecycle and provides access to MCP tools.
    /// </summary>
    public interface IMcpToolService
    {
        /// <summary>
        /// Whether the MCP Server is currently connected and tools are available.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Lists available tools from the connected MCP server.
        /// Returns empty list if not connected.
        /// </summary>
        Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default);
    }
}

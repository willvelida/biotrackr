using System.Text.Json;

namespace Biotrackr.Reporting.Api.Validation
{
    /// <summary>
    /// Validation helpers for report generation requests.
    /// </summary>
    internal static class SnapshotValidator
    {
        /// <summary>
        /// Checks whether the source data snapshot is effectively empty (null, empty object, or empty string).
        /// </summary>
        internal static bool IsEmpty(object snapshot)
        {
            if (snapshot is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => true,
                    JsonValueKind.Object => element.EnumerateObject().MoveNext() is false,
                    JsonValueKind.Array => element.GetArrayLength() == 0,
                    JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()),
                    _ => false
                };
            }

            var json = snapshot.ToString();
            return string.IsNullOrWhiteSpace(json) || json == "{}";
        }
    }
}

using Biotrackr.Sleep.Api.Models;

namespace Biotrackr.Sleep.Api.Services.Interfaces
{
    /// <summary>
    /// Translates a stored sleep document (version 1 or version 2) into the HTTP response shape
    /// defined by the C2 read contract.
    /// </summary>
    public interface ISleepDocumentTranslator
    {
        /// <summary>
        /// Version 1 documents pass through with <c>provider</c> <c>Fitbit</c>, <c>schemaVersion</c> 1 and no
        /// enrichment; version 2 documents are mapped from their raw Google payloads.
        /// </summary>
        /// <exception cref="InvalidOperationException">The document has an unsupported schema version or malformed Google data.</exception>
        SleepDocument Translate(SleepStoredDocument storedDocument);
    }
}

using Biotrackr.Activity.Api.Models;

namespace Biotrackr.Activity.Api.Services.Interfaces;

/// <summary>
/// Translates a stored activity document of any schema version into the HTTP response model.
/// </summary>
public interface IActivityDocumentTranslator
{
    /// <summary>
    /// Version 1 (no schemaVersion) passes through with provider Fitbit; version 2 is mapped from the
    /// raw Google Health payload per plan contract C2.
    /// </summary>
    /// <exception cref="InvalidOperationException">The document has an unsupported schema version.</exception>
    ActivityDocument Translate(ActivityStoredDocument storedDocument);
}

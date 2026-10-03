using AutoFixture;
using Biotrackr.Sleep.Api.Models;

namespace Biotrackr.Sleep.Api.UnitTests.TestData;

public static class StoredDocumentFixture
{
    /// <summary>
    /// AutoFixture that builds version 1 stored documents (no schema version, provider or Google payload),
    /// matching what Fitbit-era documents look like when read from Cosmos DB.
    /// </summary>
    public static IFixture Create()
    {
        var fixture = new Fixture();
        fixture.Customize<SleepStoredDocument>(composer => composer
            .With(document => document.SchemaVersion, (int?)null)
            .With(document => document.Provider, (string?)null)
            .Without(document => document.Google));
        return fixture;
    }
}

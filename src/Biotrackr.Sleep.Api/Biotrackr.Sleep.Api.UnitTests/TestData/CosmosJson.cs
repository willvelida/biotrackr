using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Biotrackr.Sleep.Api.UnitTests.TestData;

/// <summary>
/// Mirrors the settings the Cosmos SDK applies when <c>CosmosPropertyNamingPolicy.CamelCase</c>
/// is configured, so stored samples deserialise exactly as they do from the container.
/// </summary>
public static class CosmosJson
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Include,
        MaxDepth = 64
    });

    public static T Deserialize<T>(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        return Serializer.Deserialize<T>(reader)
            ?? throw new InvalidOperationException($"Sample did not deserialise to {typeof(T).Name}.");
    }
}

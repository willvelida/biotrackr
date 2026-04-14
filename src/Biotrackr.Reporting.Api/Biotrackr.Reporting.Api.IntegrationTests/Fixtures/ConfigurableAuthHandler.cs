using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biotrackr.Reporting.Api.IntegrationTests.Fixtures;

/// <summary>
/// Authentication handler for authorization policy tests that allows
/// configuring the azp claim value per request via a static property.
/// </summary>
public class ConfigurableAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The azp claim value to include in the authenticated identity.
    /// Set before making a request to control which caller identity is simulated.
    /// </summary>
    public static string AzpClaimValue { get; set; } = string.Empty;

    public ConfigurableAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-agent"),
            new("oid", "00000000-0000-0000-0000-000000000001"),
            new("tid", "test-tenant-id"),
        };

        if (!string.IsNullOrWhiteSpace(AzpClaimValue))
        {
            claims.Add(new Claim("azp", AzpClaimValue));
        }

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

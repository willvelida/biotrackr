using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Biotrackr.UI.Models;
using Microsoft.AspNetCore.Http;

namespace Biotrackr.UI.Services;

public class UserInfoService(ILogger<UserInfoService> logger) : IUserInfoService
{
    public UserInfo GetUserInfo(HttpContext httpContext)
    {
        var userInfo = new UserInfo();

        try
        {
            var principalHeader = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();

            if (!string.IsNullOrEmpty(principalHeader))
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeader));
                var principal = JsonSerializer.Deserialize<ClientPrincipal>(decoded);

                if (principal?.Claims is not null)
                {
                    userInfo.DisplayName = GetClaimValue(principal.Claims, "name") ?? "User";
                    userInfo.Email = GetClaimValue(principal.Claims, "preferred_username")
                        ?? GetClaimValue(principal.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                        ?? "";
                    userInfo.UserId = GetClaimValue(principal.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ?? "";
                    userInfo.IdentityProvider = principal.AuthType ?? "";

                    return userInfo;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse X-MS-CLIENT-PRINCIPAL header");
        }

        // Fallback to X-MS-CLIENT-PRINCIPAL-NAME
        var principalName = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault();
        if (!string.IsNullOrEmpty(principalName))
        {
            userInfo.DisplayName = principalName;
        }

        return userInfo;
    }

    private static string? GetClaimValue(List<ClientPrincipalClaim> claims, string claimType)
    {
        return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    private sealed record ClientPrincipal
    {
        [JsonPropertyName("auth_typ")]
        public string? AuthType { get; init; }

        [JsonPropertyName("claims")]
        public List<ClientPrincipalClaim>? Claims { get; init; }
    }

    private sealed record ClientPrincipalClaim
    {
        [JsonPropertyName("typ")]
        public string? Type { get; init; }

        [JsonPropertyName("val")]
        public string? Value { get; init; }
    }
}

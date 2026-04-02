using Biotrackr.Auth.Svc.Models;

namespace Biotrackr.Auth.Svc.Services.Interfaces
{
    public interface IWithingsRefreshTokenService
    {
        Task<WithingsTokenResponse> RefreshTokens();
        Task SaveTokens(WithingsTokenResponse response);
    }
}

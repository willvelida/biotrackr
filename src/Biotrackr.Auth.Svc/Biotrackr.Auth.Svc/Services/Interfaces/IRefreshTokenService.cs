using Biotrackr.Auth.Svc.Models;

namespace Biotrackr.Auth.Svc.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenResponse> RefreshTokens();
        Task SaveTokens(RefreshTokenResponse tokens);
    }
}

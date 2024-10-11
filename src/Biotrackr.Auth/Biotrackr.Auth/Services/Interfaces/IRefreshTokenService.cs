using Biotrackr.Auth.Models;

namespace Biotrackr.Auth.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenResponse> RefreshTokens();
        Task SaveTokens(RefreshTokenResponse tokens);
    }
}

using Biotrackr.UI.Models;
using Microsoft.AspNetCore.Http;

namespace Biotrackr.UI.Services;

public interface IUserInfoService
{
    UserInfo GetUserInfo(HttpContext httpContext);
}

using System.Text;
using System.Text.Json;
using Biotrackr.UI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.UI.UnitTests.Services
{
    public class UserInfoServiceShould
    {
        private readonly Mock<ILogger<UserInfoService>> _loggerMock;

        public UserInfoServiceShould()
        {
            _loggerMock = new Mock<ILogger<UserInfoService>>();
        }

        private UserInfoService CreateSut()
        {
            return new UserInfoService(_loggerMock.Object);
        }

        private static string CreateBase64Principal(string? name = null, string? email = null, string? nameIdentifier = null, string? authType = null)
        {
            var claims = new List<object>();

            if (name is not null)
                claims.Add(new { typ = "name", val = name });

            if (email is not null)
                claims.Add(new { typ = "preferred_username", val = email });

            if (nameIdentifier is not null)
                claims.Add(new { typ = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", val = nameIdentifier });

            var principal = new { auth_typ = authType ?? "aad", claims };
            var json = JsonSerializer.Serialize(principal);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        [Fact]
        public void GetUserInfo_ShouldReturnDisplayNameAndEmail_WhenClientPrincipalHeaderPresent()
        {
            var sut = CreateSut();
            var context = new DefaultHttpContext();
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = CreateBase64Principal(
                name: "Will Velida",
                email: "will@example.com");

            var result = sut.GetUserInfo(context);

            result.DisplayName.Should().Be("Will Velida");
            result.Email.Should().Be("will@example.com");
        }

        [Fact]
        public void GetUserInfo_ShouldFallbackToClientPrincipalName_WhenFullHeaderInvalid()
        {
            var sut = CreateSut();
            var context = new DefaultHttpContext();
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = "not-valid-base64!!!";
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"] = "Fallback User";

            var result = sut.GetUserInfo(context);

            result.DisplayName.Should().Be("Fallback User");
        }

        [Fact]
        public void GetUserInfo_ShouldReturnDefaults_WhenNoHeadersPresent()
        {
            var sut = CreateSut();
            var context = new DefaultHttpContext();

            var result = sut.GetUserInfo(context);

            result.DisplayName.Should().Be("User");
            result.Email.Should().Be("");
        }

        [Fact]
        public void GetUserInfo_ShouldHandleMalformedBase64_Gracefully()
        {
            var sut = CreateSut();
            var context = new DefaultHttpContext();
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = "!!!completely-garbage-data@@@";

            var act = () => sut.GetUserInfo(context);

            act.Should().NotThrow();
            var result = act();
            result.DisplayName.Should().Be("User");
        }

        [Fact]
        public void GetUserInfo_ShouldExtractUserId_WhenNameIdentifierClaimPresent()
        {
            var sut = CreateSut();
            var context = new DefaultHttpContext();
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] = CreateBase64Principal(
                name: "Will Velida",
                email: "will@example.com",
                nameIdentifier: "user-id-12345");

            var result = sut.GetUserInfo(context);

            result.UserId.Should().Be("user-id-12345");
            result.DisplayName.Should().Be("Will Velida");
            result.Email.Should().Be("will@example.com");
        }
    }
}

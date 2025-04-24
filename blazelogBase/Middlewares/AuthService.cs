using AutoMapper;
using Azure;
using blazelogBase.Shared.ErrorOr;
using blazelogBase.Shared.Tools;
using blazelogBase.Store.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Principal;

namespace blazelogBase.Middlewares
{
    public class AuthService:IN.IAuthService
    {
        ISender commander;
        IN.ITokenService tokener;
        IMapper mapper;
        IHttpContextAccessor context;
        public AuthService(IMediator sender,IN.ITokenService tokenservice,IMapper itmapper, IHttpContextAccessor accessor)
        {

            commander = sender;
            tokener = tokenservice;
            mapper = itmapper;
            context = accessor;
        }
        public async Task<ErrorOr<IPrincipal>> Authenticate(string userid,string password)
        {
            var user = await commander.Send(new GetUserQuery(userid));
            if (user == null)
                return Error.NotFound();
            var hasher = new PasswordHasher();
            var result = hasher.VerifyHashedPassword(user.EncPassword, password);
            if (result == PasswordVerificationResult.Success)
            {
                var authuser = mapper.Map<AuthUserModel>(user);
                var token = tokener.CreateToken(authuser);

                var userClaims = new List<Claim>
                {
                new Claim(ClaimTypes.NameIdentifier, userid),

                // specify custom claims
                new Claim("token", token) };

                var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);

                // creating the principal object with the identity
                var principal = new ClaimsPrincipal(identity);

                // settings for the authentication properties
                var authProperties = new AuthenticationProperties
                {

                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
                    // The time at which the authentication ticket expires. A
                    // value set here overrides the ExpireTimeSpan option of
                    // CookieAuthenticationOptions set with AddCookie.

                    IsPersistent = true,
                    // Whether the authentication session is persisted across
                    // multiple requests. When used with cookies, controls
                    // whether the cookie's lifetime is absolute (matching the
                    // lifetime of the authentication ticket) or session-based.

                    IssuedUtc = DateTimeOffset.UtcNow,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http
                    // redirect response value.
                };

                await context.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal!, authProperties);

                return principal;

            }


            return Error.Custom(1, "AuthFail", $"{userid} could not be found or Password invalid!");


        }
    }
}

using AutoMapper;
using Azure;
using blazelogBase.Shared.ErrorOr;
using blazelogBase.Shared.Tools;
using blazelogBase.Store.Commands;
using MediatR;
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
        public AuthService(IMediator sender,IN.ITokenService tokenservice,IMapper itmapper)
        {

            commander = sender;
            tokener = tokenservice;
            mapper = itmapper;
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

                return principal;

            }


            return Error.Custom(1, "AuthFail", $"{userid} could not be found or Password invalid!");


        }
    }
}

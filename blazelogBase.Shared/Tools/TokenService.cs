using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
//using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using blazelogBase.Shared.Models;
//using Microsoft.IdentityModel.JsonWebTokens;
using NPOI.POIFS.Crypt;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace blazelogBase.Shared.Tools
{
    public class TokenService: ITokenService
    {
        private string msecret { get; }
        private string issuer { get; }
        private string audience { get; }
        private ILogger<ITokenService> mlog { get; }

        public TokenService(IOptions<AuthSetting> setting,ILogger<ITokenService> itlogger)
        {
            msecret = string.IsNullOrEmpty( setting.Value.Secret) ? setting.Value.Secret : Setting.SecretKey;
            msecret = msecret + string.Join("", msecret.Reverse());
            msecret = msecret + msecret;
            issuer = string.IsNullOrEmpty( setting.Value.Issuer) ? setting.Value.Issuer : Setting.Issuer;
            audience = string.IsNullOrEmpty(setting.Value.Audience) ? setting.Value.Audience : Setting.Audience;
            
            mlog = itlogger;

        }
        private const int ExpirationMinutes = Setting.JWTExpirationInMins;
        public string CreateToken(IAuthResult user)
        {
            var expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
            var userClaims = CreateClaims(user);
            var token = CreateJwtToken(
                userClaims,
                CreateSigningCredentials(),
                expiration
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            
            return tokenHandler.WriteToken(token);
        }

        public IAuthResult? DecodeTokenToUser(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(msecret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,

                    // set clockskew to zero so token expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // Logging Purpose
                Console.WriteLine("Cookie was issued at " + jwtToken.IssuedAt);
                Console.WriteLine("Cookie was valid to " + jwtToken.ValidTo);

                string userid = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.userID)).Value;
                string username = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.userName)).Value;
                string email = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.email)).Value;
                
                string level = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.level)).Value;
                string post = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.post)).Value;
                bool isadmin =bool.Parse( jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.isadmin)).Value);
                string division = jwtToken.Claims.First(x => x.Type == nameof(IAuthResult.division)).Value;

                return new AuthUserModel
                {
                    userID = userid,
                    userName = username,
                    email = email,
                    level = level,
                    division = division,
                    post = post,
                    isadmin = isadmin,
                    

                };
            }
            catch (Exception ex)
            {
                mlog.LogDebug(ex, ex.Message);
                return null;
            }
        }


        private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials,
            DateTime expiration) =>
            new(
                issuer,
                audience,
                claims,
                expires: expiration,
                signingCredentials: credentials
            );
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private List<Claim> CreateClaims(IAuthResult user)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Iss, issuer),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
                    new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds().ToString()),
                    new Claim(JwtRegisteredClaimNames.Aud, audience),
                    new Claim(JwtRegisteredClaimNames.Sub, Setting.Subject),
                    new Claim(JwtRegisteredClaimNames.Jti, "HYD." + DateTime.Now.ToString("yyyyMMddhhmmss")),                                       
                    new Claim(nameof(IAuthResult.userName), user.userName),
                    new Claim(nameof(IAuthResult.userID), user.userID),
                    new Claim(nameof(IAuthResult.level),user.level),
                    new Claim(nameof(IAuthResult.post), user.post),
                    new Claim(nameof(IAuthResult.isadmin), user.isadmin.ToString(), ClaimValueTypes.Boolean),
                    new Claim(nameof(IAuthResult.division), user.division ?? ""),
                    new Claim(nameof(IAuthResult.email), user.email ?? ""),
                    
                    //new Claim(AuthClaims.DivisionAdminEnabled, user.IsDivisionAdmin.ToString()),
                    //new Claim(AuthClaims.DataAdminEnabled, user.IsDataAdmin.ToString()),
                    //new Claim(AuthClaims.ControlAdminEnabled, user.IsControlAdmin.ToString()),
                };
                return claims;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private SigningCredentials CreateSigningCredentials()
        {
            return new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(msecret)
                ),
                SecurityAlgorithms.HmacSha256
            );
        }
    }
}

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeddingAPI.Models;
using WeddingAPI.Services.Interfaces;

namespace WeddingAPI.Services;

public class TokenService : ITokenService
{
  private readonly JwtSettings _jwtSettings;

  public TokenService(JwtSettings jwtSettings)
  {
    _jwtSettings = jwtSettings;
  }

  public string GenerateClientToken(string clientId, string clientName)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
    var now = DateTimeOffset.UtcNow;

    var claims = new[]
    {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("client_id", clientId),
            new Claim("client_name", clientName),
            new Claim("scope", "rsvp:read rsvp:write")
        };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(claims),
      Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryHours),
      Issuer = _jwtSettings.Issuer,
      Audience = _jwtSettings.Audience,
      SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);

    return tokenHandler.WriteToken(token);
  }
}
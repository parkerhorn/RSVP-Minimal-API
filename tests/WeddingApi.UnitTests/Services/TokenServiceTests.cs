using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeddingAPI.Models;
using WeddingAPI.Services;
using Xunit;

namespace WeddingApi.UnitTests;

public class TokenServiceTests
{
  private readonly JwtSettings _jwtSettings;
  private readonly TokenService _tokenService;

  public TokenServiceTests()
  {
    _jwtSettings = new JwtSettings
    {
      SecretKey = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
      Issuer = "https://test-wedding-api.local",
      Audience = "https://test-wedding-api.local/api",
      ExpiryHours = 24
    };

    _tokenService = new TokenService(_jwtSettings);
  }

  [Fact]
  public void GenerateClientToken_WithValidParameters_ReturnsValidJwtToken()
  {
    var clientId = "test-client";
    var clientName = "Test Client";

    var token = _tokenService.GenerateClientToken(clientId, clientName);

    Assert.NotNull(token);
    Assert.NotEmpty(token);

    var tokenHandler = new JwtSecurityTokenHandler();
    Assert.True(tokenHandler.CanReadToken(token));
  }

  [Fact]
  public void GenerateClientToken_TokenContainsCorrectClaims()
  {
    var clientId = "test-client";
    var clientName = "Test Client";

    var token = _tokenService.GenerateClientToken(clientId, clientName);

    var tokenHandler = new JwtSecurityTokenHandler();
    var jsonToken = tokenHandler.ReadJwtToken(token);

    var subClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
    var jtiClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
    var iatClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat);
    var clientIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "client_id");
    var clientNameClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "client_name");
    var scopeClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "scope");

    Assert.NotNull(subClaim);
    Assert.Equal(clientId, subClaim.Value);

    Assert.NotNull(jtiClaim);
    Assert.True(Guid.TryParse(jtiClaim.Value, out _));

    Assert.NotNull(iatClaim);
    Assert.NotEmpty(iatClaim.Value);

    Assert.NotNull(clientIdClaim);
    Assert.Equal(clientId, clientIdClaim.Value);

    Assert.NotNull(clientNameClaim);
    Assert.Equal(clientName, clientNameClaim.Value);

    Assert.NotNull(scopeClaim);
    Assert.Equal("rsvp:read rsvp:write", scopeClaim.Value);
  }

  [Fact]
  public void GenerateClientToken_TokenHasCorrectIssuerAndAudience()
  {
    var clientId = "test-client";
    var clientName = "Test Client";

    var token = _tokenService.GenerateClientToken(clientId, clientName);

    var tokenHandler = new JwtSecurityTokenHandler();
    var jsonToken = tokenHandler.ReadJwtToken(token);

    Assert.Equal(_jwtSettings.Issuer, jsonToken.Issuer);
    Assert.Contains(_jwtSettings.Audience, jsonToken.Audiences);
  }

  [Fact]
  public void GenerateClientToken_TokenHasCorrectExpiration()
  {
    var clientId = "test-client";
    var clientName = "Test Client";
    var beforeGeneration = DateTime.UtcNow;

    var token = _tokenService.GenerateClientToken(clientId, clientName);

    var tokenHandler = new JwtSecurityTokenHandler();
    var jsonToken = tokenHandler.ReadJwtToken(token);

    var expectedExpiry = beforeGeneration.AddHours(_jwtSettings.ExpiryHours);
    var timeDifference = Math.Abs((jsonToken.ValidTo - expectedExpiry).TotalMinutes);

    Assert.True(timeDifference < 1, "Token expiry should be within 1 minute of expected time");
  }

  [Fact]
  public void GenerateClientToken_TokenIsValidlySignedWithSecretKey()
  {
    var clientId = "test-client";
    var clientName = "Test Client";

    var token = _tokenService.GenerateClientToken(clientId, clientName);

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey));

    var validationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = _jwtSettings.Issuer,
      ValidAudience = _jwtSettings.Audience,
      IssuerSigningKey = key,
      ClockSkew = TimeSpan.Zero
    };

    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

    Assert.NotNull(principal);
    Assert.NotNull(validatedToken);
    Assert.IsType<JwtSecurityToken>(validatedToken);
  }

  [Theory]
  [InlineData("", "Client Name")]
  [InlineData("client-id", "")]
  public void GenerateClientToken_WithEmptyParameters_StillGeneratesToken(string clientId, string clientName)
  {
    var token = _tokenService.GenerateClientToken(clientId, clientName);

    Assert.NotNull(token);
    Assert.NotEmpty(token);

    var tokenHandler = new JwtSecurityTokenHandler();
    Assert.True(tokenHandler.CanReadToken(token));
  }
}
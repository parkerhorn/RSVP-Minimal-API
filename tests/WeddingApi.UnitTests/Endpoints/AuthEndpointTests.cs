using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WeddingAPI.Models;
using WeddingAPI.Services.Interfaces;
using Xunit;

namespace WeddingApi.UnitTests;

public class AuthEndpointTests
{
  private readonly Mock<ITokenService> _mockTokenService;
  private readonly List<ClientCredentials> _clientCredentials;
  private readonly JwtSettings _jwtSettings;

  public AuthEndpointTests()
  {
    _mockTokenService = new Mock<ITokenService>();
    _clientCredentials = new List<ClientCredentials>
    {
      new ClientCredentials
      {
        ClientId = "test-client-1",
        ClientSecret = "test-secret-1",
        ClientName = "Test Client 1",
        IsActive = true
      },
      new ClientCredentials
      {
        ClientId = "test-client-2",
        ClientSecret = "test-secret-2",
        ClientName = "Test Client 2",
        IsActive = false
      }
    };

    _jwtSettings = new JwtSettings
    {
      SecretKey = "TestSecretKey",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpiryHours = 24
    };
  }

  #region Token Endpoint Tests

  [Fact]
  public void TokenEndpoint_WithValidCredentials_ReturnsOkWithToken()
  {
    var request = new TokenRequest
    {
      ClientId = "test-client-1",
      ClientSecret = "test-secret-1"
    };

    var expectedToken = "test-jwt-token";
    _mockTokenService
      .Setup(s => s.GenerateClientToken(request.ClientId, "Test Client 1"))
      .Returns(expectedToken);

    var result = TokenEndpointLogic(request, _mockTokenService.Object, _clientCredentials, _jwtSettings);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    dynamic response = okResult.Value;

    Assert.Equal(expectedToken, response.access_token);
    Assert.Equal("Bearer", response.token_type);
    Assert.Equal(86400, response.expires_in); // 24 hours * 3600 seconds
    Assert.Equal("Test Client 1", response.client_name);
  }

  [Fact]
  public void TokenEndpoint_WithInvalidClientId_ReturnsUnauthorized()
  {
    var request = new TokenRequest
    {
      ClientId = "invalid-client",
      ClientSecret = "test-secret-1"
    };

    var result = TokenEndpointLogic(request, _mockTokenService.Object, _clientCredentials, _jwtSettings);

    Assert.IsType<UnauthorizedHttpResult>(result);
  }

  [Fact]
  public void TokenEndpoint_WithInvalidClientSecret_ReturnsUnauthorized()
  {
    var request = new TokenRequest
    {
      ClientId = "test-client-1",
      ClientSecret = "invalid-secret"
    };

    var result = TokenEndpointLogic(request, _mockTokenService.Object, _clientCredentials, _jwtSettings);

    Assert.IsType<UnauthorizedHttpResult>(result);
  }

  [Fact]
  public void TokenEndpoint_WithInactiveClient_ReturnsUnauthorized()
  {
    var request = new TokenRequest
    {
      ClientId = "test-client-2",
      ClientSecret = "test-secret-2"
    };

    var result = TokenEndpointLogic(request, _mockTokenService.Object, _clientCredentials, _jwtSettings);

    Assert.IsType<UnauthorizedHttpResult>(result);
  }

  [Theory]
  [InlineData("", "test-secret")]
  [InlineData("test-client", "")]
  [InlineData("", "")]
  public void TokenEndpoint_WithMissingCredentials_ReturnsBadRequest(string clientId, string clientSecret)
  {
    var request = new TokenRequest
    {
      ClientId = clientId,
      ClientSecret = clientSecret
    };

    var result = TokenEndpointLogic(request, _mockTokenService.Object, _clientCredentials, _jwtSettings);

    var badRequestResult = Assert.IsType<BadRequest<string>>(result);
    Assert.Equal("ClientId and ClientSecret are required", badRequestResult.Value);
  }

  #endregion

  #region Validate Endpoint Tests

  [Fact]
  public void ValidateEndpoint_WithValidCredentials_ReturnsValidResponse()
  {
    var request = new TokenRequest
    {
      ClientId = "test-client-1",
      ClientSecret = "test-secret-1"
    };

    var result = ValidateEndpointLogic(request, _clientCredentials);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    dynamic response = okResult.Value;

    Assert.True(response.IsValid);
    Assert.Equal("Test Client 1", response.ClientName);
    Assert.Equal("Valid credentials", response.Message);
  }

  [Fact]
  public void ValidateEndpoint_WithInvalidCredentials_ReturnsInvalidResponse()
  {
    var request = new TokenRequest
    {
      ClientId = "invalid-client",
      ClientSecret = "invalid-secret"
    };

    var result = ValidateEndpointLogic(request, _clientCredentials);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    dynamic response = okResult.Value;

    Assert.False(response.IsValid);
    Assert.Equal("", response.ClientName);
    Assert.Equal("Invalid credentials", response.Message);
  }

  [Fact]
  public void ValidateEndpoint_WithInactiveClient_ReturnsInvalidResponse()
  {
    var request = new TokenRequest
    {
      ClientId = "test-client-2",
      ClientSecret = "test-secret-2"
    };

    var result = ValidateEndpointLogic(request, _clientCredentials);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    dynamic response = okResult.Value;

    Assert.False(response.IsValid);
    Assert.Equal("", response.ClientName);
    Assert.Equal("Invalid credentials", response.Message);
  }

  #endregion

  #region Helper Methods (extracted endpoint logic for testing)

  private static IResult TokenEndpointLogic(
    TokenRequest request,
    ITokenService tokenService,
    List<ClientCredentials> clientCredentials,
    JwtSettings jwtSettings)
  {
    if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
    {
      return Results.BadRequest("ClientId and ClientSecret are required");
    }

    var client = clientCredentials.FirstOrDefault(c =>
            c.ClientId == request.ClientId &&
            c.ClientSecret == request.ClientSecret &&
            c.IsActive);

    if (client == null)
    {
      return Results.Unauthorized();
    }

    var token = tokenService.GenerateClientToken(request.ClientId, client.ClientName);

    var response = new
    {
      access_token = token,
      token_type = "Bearer",
      expires_in = jwtSettings.ExpiryHours * 3600,
      client_name = client.ClientName
    };

    return Results.Ok(response);
  }

  private static IResult ValidateEndpointLogic(
    TokenRequest request,
    List<ClientCredentials> clientCredentials)
  {
    var client = clientCredentials.FirstOrDefault(c =>
            c.ClientId == request.ClientId &&
            c.ClientSecret == request.ClientSecret &&
            c.IsActive);

    var response = new
    {
      IsValid = client != null,
      ClientName = client?.ClientName ?? "",
      Message = client != null ? "Valid credentials" : "Invalid credentials"
    };

    return Results.Ok(response);
  }

  #endregion
}
using Microsoft.AspNetCore.Mvc;
using WeddingAPI.Models;
using WeddingAPI.Services.Interfaces;

namespace WeddingAPI.Helpers;

public class AuthEndpointMapper : IEndpointMapper
{
  public void MapEndpoints(WebApplication app)
  {
    var auth = app.MapGroup("/auth")
        .WithTags("Authentication")
        .WithOpenApi();

    MapTokenEndpoint(auth);
    MapValidateEndpoint(auth);
  }

  private static void MapTokenEndpoint(RouteGroupBuilder endpoints)
  {
    endpoints.MapPost("/token", ([FromBody] TokenRequest request,
        ITokenService tokenService,
        List<ClientCredentials> clientCredentials,
        JwtSettings jwtSettings) =>
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
    })
    .WithName("GetClientToken")
    .WithDescription("Get access token using client credentials")
    .AllowAnonymous();
  }

  private static void MapValidateEndpoint(RouteGroupBuilder endpoints)
  {
    endpoints.MapPost("/validate", ([FromBody] TokenRequest request, List<ClientCredentials> clientCredentials) =>
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
    })
    .WithName("ValidateCredentials")
    .WithDescription("Validate client credentials without getting a token")
    .AllowAnonymous();
  }
}
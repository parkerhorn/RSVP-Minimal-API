using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace WeddingApi.IntegrationTests;

public class AuthIntegrationTests : IDisposable
{
  private readonly HttpClient _client;
  private readonly string _baseUrl;
  private readonly ITestOutputHelper _output;

  public AuthIntegrationTests(ITestOutputHelper output)
  {
    _output = output;
    _baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? throw new InvalidOperationException("API_BASE_URL environment variable must be set");
    _client = new HttpClient();
    _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  }

  [Fact]
  public async Task TokenEndpoint_WithValidCredentials_ReturnsToken()
  {
    var tokenRequest = new
    {
      ClientId = "wedding-frontend",
      ClientSecret = "WeddingFrontend2024!SecretKey"
    };

    var json = JsonSerializer.Serialize(tokenRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PostAsync($"{_baseUrl}/auth/token", content);

    response.EnsureSuccessStatusCode();

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Token Response: {PrettyPrintJson(responseContent)}");

    var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

    Assert.True(tokenResponse.TryGetProperty("access_token", out var accessToken));
    Assert.False(string.IsNullOrEmpty(accessToken.GetString()));

    Assert.True(tokenResponse.TryGetProperty("token_type", out var tokenType));
    Assert.Equal("Bearer", tokenType.GetString());

    Assert.True(tokenResponse.TryGetProperty("expires_in", out var expiresIn));
    Assert.True(expiresIn.GetInt32() > 0);

    Assert.True(tokenResponse.TryGetProperty("client_name", out var clientName));
    Assert.Equal("Wedding Frontend Application", clientName.GetString());
  }

  [Fact]
  public async Task TokenEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
  {
    var tokenRequest = new
    {
      ClientId = "invalid-client",
      ClientSecret = "invalid-secret"
    };

    var json = JsonSerializer.Serialize(tokenRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PostAsync($"{_baseUrl}/auth/token", content);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task TokenEndpoint_WithMissingCredentials_ReturnsBadRequest()
  {
    var tokenRequest = new
    {
      ClientId = "",
      ClientSecret = ""
    };

    var json = JsonSerializer.Serialize(tokenRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PostAsync($"{_baseUrl}/auth/token", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("ClientId and ClientSecret are required", responseContent);
  }

  [Fact]
  public async Task ValidateEndpoint_WithValidCredentials_ReturnsValid()
  {
    var validateRequest = new
    {
      ClientId = "wedding-frontend",
      ClientSecret = "WeddingFrontend2024!SecretKey"
    };

    var json = JsonSerializer.Serialize(validateRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PostAsync($"{_baseUrl}/auth/validate", content);

    response.EnsureSuccessStatusCode();

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Validate Response: {PrettyPrintJson(responseContent)}");

    var validateResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

    Assert.True(validateResponse.TryGetProperty("IsValid", out var isValid));
    Assert.True(isValid.GetBoolean());

    Assert.True(validateResponse.TryGetProperty("ClientName", out var clientName));
    Assert.Equal("Wedding Frontend Application", clientName.GetString());

    Assert.True(validateResponse.TryGetProperty("Message", out var message));
    Assert.Equal("Valid credentials", message.GetString());
  }

  [Fact]
  public async Task ValidateEndpoint_WithInvalidCredentials_ReturnsInvalid()
  {
    var validateRequest = new
    {
      ClientId = "invalid-client",
      ClientSecret = "invalid-secret"
    };

    var json = JsonSerializer.Serialize(validateRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PostAsync($"{_baseUrl}/auth/validate", content);

    response.EnsureSuccessStatusCode();

    var responseContent = await response.Content.ReadAsStringAsync();

    var validateResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

    Assert.True(validateResponse.TryGetProperty("IsValid", out var isValid));
    Assert.False(isValid.GetBoolean());

    Assert.True(validateResponse.TryGetProperty("ClientName", out var clientName));
    Assert.Equal("", clientName.GetString());

    Assert.True(validateResponse.TryGetProperty("Message", out var message));
    Assert.Equal("Invalid credentials", message.GetString());
  }

  [Fact]
  public async Task EndToEnd_GetTokenAndAccessProtectedEndpoint()
  {
    // Step 1: Get token
    var tokenRequest = new
    {
      ClientId = "wedding-frontend",
      ClientSecret = "WeddingFrontend2024!SecretKey"
    };

    var json = JsonSerializer.Serialize(tokenRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var tokenResponse = await _client.PostAsync($"{_baseUrl}/auth/token", content);
    tokenResponse.EnsureSuccessStatusCode();

    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
    var tokenJson = JsonSerializer.Deserialize<JsonElement>(tokenContent);
    var accessToken = tokenJson.GetProperty("access_token").GetString();

    _output.WriteLine($"Received token: {accessToken?[..20]}...");

    // Step 2: Use token to access protected RSVP endpoint
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var rsvpResponse = await _client.GetAsync($"{_baseUrl}/rsvp");

    // Should succeed with valid token (or 404/empty if no RSVPs exist, but not 401)
    Assert.NotEqual(HttpStatusCode.Unauthorized, rsvpResponse.StatusCode);

    _output.WriteLine($"RSVP endpoint response status: {rsvpResponse.StatusCode}");
  }

  private string PrettyPrintJson(string json)
  {
    try
    {
      var jsonElement = JsonDocument.Parse(json).RootElement;
      return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
    }
    catch
    {
      return json;
    }
  }

  public void Dispose()
  {
    _client.Dispose();
  }
}
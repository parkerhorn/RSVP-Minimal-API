namespace WeddingAPI.Services.Interfaces;

public interface ITokenService
{
  string GenerateClientToken(string clientId, string clientName);
}
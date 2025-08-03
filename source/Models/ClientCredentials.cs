namespace WeddingAPI.Models;

public class ClientCredentials
{
  public string ClientId { get; set; } = string.Empty;
  public string ClientSecret { get; set; } = string.Empty;
  public string ClientName { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
}
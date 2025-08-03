using WeddingAPI.Repository;

namespace WeddingAPI.Helpers;

public class HealthEndpointMapper : IEndpointMapper
{
  public void MapEndpoints(WebApplication app)
  {
    app.MapGet("/", () => "Root endpoint for the API");

    app.MapGet("/health", async (ApplicationDbContext db) =>
    {
      try
      {
        await db.Database.CanConnectAsync();
        
        return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
      }
      catch (Exception ex)
      {
        return Results.Problem(statusCode: 503, detail: ex.Message);
      }
    });
  }
}
using WeddingAPI.Repository;

namespace WeddingAPI.Helpers;

public class HealthEndpointMapper : IEndpointMapper
{
  public void MapEndpoints(WebApplication app)
  {
    app.MapGet("/", () => "Lenny and Parker are getting married!");

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
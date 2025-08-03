using Microsoft.AspNetCore.Mvc;
using WeddingAPI.Models;
using WeddingAPI.Repository;
using WeddingAPI.Repository.Interfaces;
using WeddingAPI.Services.Interfaces;

namespace WeddingAPI.Helpers;

public class RSVPEndpointMapper : IEndpointMapper
{
  public void MapEndpoints(WebApplication app)
  {
    var rsvp = app.MapGroup("/rsvp").WithTags("RSVP").WithOpenApi().RequireAuthorization();

    rsvp.MapPost("/", async ([FromBody] IEnumerable<RSVP> rsvps, IGenericAsyncDataService<RSVP, ApplicationDbContext> service, IUnitOfWork<ApplicationDbContext> unitOfWork) =>
    {
      foreach (var r in rsvps)
      {
        await service.AddAsync(r);
      }

      await unitOfWork.SaveChangesAsync(new CancellationToken());

      return Results.Created("/rsvp", rsvps);
    });

    rsvp.MapGet("/", async (IGenericAsyncDataService<RSVP, ApplicationDbContext> service) =>
    {
      var result = await service.GetAllAsync();

      return result is null ? Results.NotFound() : Results.Ok(result);
    });

    rsvp.MapGet("/{id:guid}", async (Guid id, IGenericAsyncDataService<RSVP, ApplicationDbContext> service) =>
    {
      var result = await service.GetByIdAsync(id);

      return result is null ? Results.NotFound() : Results.Ok(result);
    });

    rsvp.MapPut("/{id:guid}", async (Guid id, RSVP rsvp, IGenericAsyncDataService<RSVP, ApplicationDbContext> service) =>
    {
      var item = await service.GetByIdAsync(id);

      if (item is null)
      {
        return Results.NotFound();
      }

      item.FirstName = rsvp.FirstName;
      item.LastName = rsvp.LastName;
      item.Email = rsvp.Email;
      item.IsAttending = rsvp.IsAttending;
      item.DietaryRestrictions = rsvp.DietaryRestrictions;
      item.AccessibilityRequirements = rsvp.AccessibilityRequirements;
      item.Pronouns = rsvp.Pronouns;
      item.Note = rsvp.Note;
      item.UpdatedAt = DateTime.UtcNow;

      return Results.Ok(await service.UpdateAndSaveAsync(item));
    });

    rsvp.MapDelete("/{id:guid}", async (Guid id, IGenericAsyncDataService<RSVP, ApplicationDbContext> service) =>
    {
      var item = await service.GetByIdAsync(id);

      if (item is null)
      {
        return Results.NotFound();
      }

      await service.DeleteAndSaveAsync(item);

      return Results.NoContent();
    });
  }
}
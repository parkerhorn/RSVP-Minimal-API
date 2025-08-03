using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WeddingAPI.Models;
using WeddingAPI.Repository;
using WeddingAPI.Repository.Interfaces;
using WeddingAPI.Services.Interfaces;
using Xunit;

namespace WeddingApi.UnitTests.Endpoints;

public class RSVPEndpointTests
{
  private readonly Mock<IGenericAsyncDataService<RSVP, ApplicationDbContext>> _mockService;
  private readonly Mock<IUnitOfWork<ApplicationDbContext>> _mockUnitOfWork;

  public RSVPEndpointTests()
  {
    _mockService = new Mock<IGenericAsyncDataService<RSVP, ApplicationDbContext>>();
    _mockUnitOfWork = new Mock<IUnitOfWork<ApplicationDbContext>>();
  }

  #region POST Tests

  [Fact]
  public async Task PostRSVPs_WithValidRSVPs_ReturnsCreated()
  {
    var rsvps = new List<RSVP>
    {
      new RSVP { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
      new RSVP { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
    };

    var result = await PostRSVPsLogic(rsvps, _mockService.Object, _mockUnitOfWork.Object);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic createdResult = result;
    Assert.Equal("/rsvp", createdResult.Location);
    Assert.Equal(rsvps, createdResult.Value);

    _mockService.Verify(s => s.AddAsync(It.IsAny<RSVP>()), Times.Exactly(2));
    _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task PostRSVPs_WithEmptyList_ReturnsCreated()
  {
    var rsvps = new List<RSVP>();

    var result = await PostRSVPsLogic(rsvps, _mockService.Object, _mockUnitOfWork.Object);

    Assert.IsAssignableFrom<IResult>(result);

    _mockService.Verify(s => s.AddAsync(It.IsAny<RSVP>()), Times.Never);
    _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion

  #region GET All Tests

  [Fact]
  public async Task GetAllRSVPs_WithExistingRSVPs_ReturnsOkWithRSVPs()
  {
    var rsvps = new List<RSVP>
    {
      new RSVP { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
      new RSVP { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" }
    };

    _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<RSVP>?)rsvps);

    var result = await GetAllRSVPsLogic(_mockService.Object);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    Assert.Equal(rsvps, okResult.Value);
  }

  [Fact]
  public async Task GetAllRSVPs_WithNoRSVPs_ReturnsNotFound()
  {
    _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<RSVP>?)null);

    var result = await GetAllRSVPsLogic(_mockService.Object);

    Assert.IsType<NotFound>(result);
  }

  #endregion

  #region GET by ID Tests

  [Fact]
  public async Task GetRSVPById_WithExistingId_ReturnsOkWithRSVP()
  {
    var id = Guid.NewGuid();
    var rsvp = new RSVP { Id = id, FirstName = "John", LastName = "Doe" };

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((RSVP?)rsvp);

    var result = await GetRSVPByIdLogic(id, _mockService.Object);

    Assert.IsAssignableFrom<IResult>(result);

    dynamic okResult = result;
    Assert.Equal(rsvp, okResult.Value);
  }

  [Fact]
  public async Task GetRSVPById_WithNonExistentId_ReturnsNotFound()
  {
    var id = Guid.NewGuid();

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((RSVP?)null);

    var result = await GetRSVPByIdLogic(id, _mockService.Object);

    Assert.IsType<NotFound>(result);
  }

  #endregion

  #region PUT Tests

  [Fact]
  public async Task PutRSVP_WithExistingId_ReturnsOkWithUpdatedRSVP()
  {
    var id = Guid.NewGuid();
    var existingRSVP = new RSVP
    {
      Id = id,
      FirstName = "John",
      LastName = "Doe",
      Email = "john@example.com",
      CreatedAt = DateTime.UtcNow.AddDays(-1)
    };

    var updateRSVP = new RSVP
    {
      FirstName = "Jane",
      LastName = "Smith",
      Email = "jane@example.com",
      IsAttending = true,
      DietaryRestrictions = "Vegetarian",
      AccessibilityRequirements = "Wheelchair access",
      Pronouns = "She/Her",
      Note = "Looking forward to it!"
    };

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(existingRSVP);
    _mockService.Setup(s => s.UpdateAndSaveAsync(It.IsAny<RSVP>())).ReturnsAsync(existingRSVP);

    var result = await PutRSVPLogic(id, updateRSVP, _mockService.Object);

    Assert.IsAssignableFrom<IResult>(result);

    // Verify the existing RSVP was updated with new values
    Assert.Equal("Jane", existingRSVP.FirstName);
    Assert.Equal("Smith", existingRSVP.LastName);
    Assert.Equal("jane@example.com", existingRSVP.Email);
    Assert.True(existingRSVP.IsAttending);
    Assert.Equal("Vegetarian", existingRSVP.DietaryRestrictions);
    Assert.Equal("Wheelchair access", existingRSVP.AccessibilityRequirements);
    Assert.Equal("She/Her", existingRSVP.Pronouns);
    Assert.Equal("Looking forward to it!", existingRSVP.Note);
    Assert.True(existingRSVP.UpdatedAt > existingRSVP.CreatedAt);

    _mockService.Verify(s => s.UpdateAndSaveAsync(existingRSVP), Times.Once);
  }

  [Fact]
  public async Task PutRSVP_WithNonExistentId_ReturnsNotFound()
  {
    var id = Guid.NewGuid();
    var updateRSVP = new RSVP { FirstName = "Jane", LastName = "Smith" };

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((RSVP?)null);

    var result = await PutRSVPLogic(id, updateRSVP, _mockService.Object);

    Assert.IsType<NotFound>(result);

    _mockService.Verify(s => s.UpdateAndSaveAsync(It.IsAny<RSVP>()), Times.Never);
  }

  #endregion

  #region DELETE Tests

  [Fact]
  public async Task DeleteRSVP_WithExistingId_ReturnsNoContent()
  {
    var id = Guid.NewGuid();
    var rsvp = new RSVP { Id = id, FirstName = "John", LastName = "Doe" };

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((RSVP?)rsvp);

    var result = await DeleteRSVPLogic(id, _mockService.Object);

    Assert.IsType<NoContent>(result);

    _mockService.Verify(s => s.DeleteAndSaveAsync(rsvp), Times.Once);
  }

  [Fact]
  public async Task DeleteRSVP_WithNonExistentId_ReturnsNotFound()
  {
    var id = Guid.NewGuid();

    _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((RSVP?)null);

    var result = await DeleteRSVPLogic(id, _mockService.Object);

    Assert.IsType<NotFound>(result);

    _mockService.Verify(s => s.DeleteAndSaveAsync(It.IsAny<RSVP>()), Times.Never);
  }

  #endregion

  #region Helper Methods (extracted endpoint logic for testing)

  private static async Task<IResult> PostRSVPsLogic(
    IEnumerable<RSVP> rsvps,
    IGenericAsyncDataService<RSVP, ApplicationDbContext> service,
    IUnitOfWork<ApplicationDbContext> unitOfWork)
  {
    foreach (var r in rsvps)
    {
      await service.AddAsync(r);
    }

    await unitOfWork.SaveChangesAsync(new CancellationToken());

    return Results.Created("/rsvp", rsvps);
  }

  private static async Task<IResult> GetAllRSVPsLogic(
    IGenericAsyncDataService<RSVP, ApplicationDbContext> service)
  {
    var result = await service.GetAllAsync();

    return result is null ? Results.NotFound() : Results.Ok(result);
  }

  private static async Task<IResult> GetRSVPByIdLogic(
    Guid id,
    IGenericAsyncDataService<RSVP, ApplicationDbContext> service)
  {
    var result = await service.GetByIdAsync(id);

    return result is null ? Results.NotFound() : Results.Ok(result);
  }

  private static async Task<IResult> PutRSVPLogic(
    Guid id,
    RSVP rsvp,
    IGenericAsyncDataService<RSVP, ApplicationDbContext> service)
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
  }

  private static async Task<IResult> DeleteRSVPLogic(
    Guid id,
    IGenericAsyncDataService<RSVP, ApplicationDbContext> service)
  {
    var item = await service.GetByIdAsync(id);

    if (item is null)
    {
      return Results.NotFound();
    }

    await service.DeleteAndSaveAsync(item);

    return Results.NoContent();
  }

  #endregion
}
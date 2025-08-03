using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WeddingAPI.Models;
using WeddingAPI.Repository;
using WeddingAPI.Services;

namespace WeddingApi.UnitTests;

public class RepositoryTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork<ApplicationDbContext> _unitOfWork;

    public RepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _unitOfWork = new UnitOfWork<ApplicationDbContext>(_context);
    }

    [Fact]
    public async Task Repository_CanAddAndRetrieveRSVP()
    {
        // Arrange
        var rsvp = new RSVP
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsAttending = true,
            DietaryRestrictions = "Gluten-free"
        };

        // Act
        await _unitOfWork.GetGenericAsyncRepository<RSVP>().AddAsync(rsvp);
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        // Assert
        var savedRsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Email == "test@example.com");
        Assert.NotNull(savedRsvp);
        Assert.Equal("Test", savedRsvp.FirstName);
        Assert.Equal("User", savedRsvp.LastName);
        Assert.True(savedRsvp.IsAttending);
        Assert.Equal("Gluten-free", savedRsvp.DietaryRestrictions);
    }

    [Fact]
    public async Task Repository_CanUpdateRSVP()
    {
        // Arrange
        var rsvp = new RSVP
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            IsAttending = true
        };

        await _unitOfWork.GetGenericAsyncRepository<RSVP>().AddAsync(rsvp);
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        // Act
        var savedRsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Email == "jane@example.com");
        savedRsvp.IsAttending = false;
        savedRsvp.Note = "Sorry, can't make it";
        savedRsvp.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.GetGenericAsyncRepository<RSVP>().Update(savedRsvp);
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        // Assert
        var updatedRsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Email == "jane@example.com");
        Assert.NotNull(updatedRsvp);
        Assert.False(updatedRsvp.IsAttending);
        Assert.Equal("Sorry, can't make it", updatedRsvp.Note);
        Assert.NotNull(updatedRsvp.UpdatedAt);
    }

    [Fact]
    public async Task Repository_CanDeleteRSVP()
    {
        // Arrange
        var rsvp = new RSVP
        {
            FirstName = "To",
            LastName = "Delete",
            Email = "delete@example.com",
            IsAttending = true
        };

        await _unitOfWork.GetGenericAsyncRepository<RSVP>().AddAsync(rsvp);
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        // Verify it was added
        var savedRsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Email == "delete@example.com");
        Assert.NotNull(savedRsvp);

        // Act
        _unitOfWork.GetGenericAsyncRepository<RSVP>().Delete(savedRsvp);
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        // Assert
        var deletedRsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Email == "delete@example.com");
        Assert.Null(deletedRsvp);
    }

    [Fact]
    public async Task Service_CanRetrieveAllRSVPs()
    {
        // Arrange
        var rsvps = new[]
        {
            new RSVP { FirstName = "Guest1", LastName = "One", Email = "guest1@example.com", IsAttending = true },
            new RSVP { FirstName = "Guest2", LastName = "Two", Email = "guest2@example.com", IsAttending = false },
            new RSVP { FirstName = "Guest3", LastName = "Three", Email = "guest3@example.com", IsAttending = true }
        };

        foreach (var rsvp in rsvps)
        {
            await _unitOfWork.GetGenericAsyncRepository<RSVP>().AddAsync(rsvp);
        }
        await _unitOfWork.SaveChangesAsync(new CancellationToken());

        var logger = new NullLogger<GenericAsyncDataService<RSVP, ApplicationDbContext>>();
        var service = new GenericAsyncDataService<RSVP, ApplicationDbContext>(_unitOfWork, logger);

        // Act
        var allRsvps = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, allRsvps.Count());
        Assert.Contains(allRsvps, r => r.Email == "guest1@example.com");
        Assert.Contains(allRsvps, r => r.Email == "guest2@example.com");
        Assert.Contains(allRsvps, r => r.Email == "guest3@example.com");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
using Microsoft.Extensions.Logging;
using Moq;
using WeddingAPI.Models;
using WeddingAPI.Repository;
using WeddingAPI.Repository.Interfaces;
using WeddingAPI.Services;

namespace WeddingApi.UnitTests;

public class RSVPServiceTests
{
    private readonly Mock<IUnitOfWork<ApplicationDbContext>> _mockUnitOfWork;
    private readonly Mock<ILogger<GenericAsyncDataService<RSVP, ApplicationDbContext>>> _mockLogger;
    private readonly Mock<IGenericAsyncRepository<RSVP>> _mockRepository;

    public RSVPServiceTests()
    {
        _mockRepository = new Mock<IGenericAsyncRepository<RSVP>>();
        _mockUnitOfWork = new Mock<IUnitOfWork<ApplicationDbContext>>();
        _mockLogger = new Mock<ILogger<GenericAsyncDataService<RSVP, ApplicationDbContext>>>();

        _mockUnitOfWork
            .Setup(uow => uow.GetGenericAsyncRepository<RSVP>())
            .Returns(_mockRepository.Object);
    }

    [Fact]
    public async Task AddAsync_CallsRepositoryAndSavesChanges()
    {
        // Arrange
        var service = new GenericAsyncDataService<RSVP, ApplicationDbContext>(
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        var rsvp = new RSVP
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsAttending = true
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RSVP>(), It.IsAny<CancellationToken>()))
            .Returns((RSVP entity, CancellationToken token) => default);

        // Act
        await service.AddAndSaveAsync(rsvp);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.Is<RSVP>(e => e.Email == "test@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRSVP_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rsvp = new RSVP
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<object>()))
            .ReturnsAsync(rsvp);

        var service = new GenericAsyncDataService<RSVP, ApplicationDbContext>(
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Act
        var result = await service.GetByIdAsync(id);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetAllAsync_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetListAsync(null, null, null, It.IsAny<CancellationToken>(), false, false))
            .ThrowsAsync(new Exception("Test exception"));

        var service = new GenericAsyncDataService<RSVP, ApplicationDbContext>(
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.GetAllAsync());

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to retrieve all entities")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
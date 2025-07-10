using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class RentReminderServiceTests
{
    [Fact]
    public async Task SendRemindersAsync_SendsSmsToTenantsWithUpcomingRent()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { FullName = "John Doe", Contact = "27821234567", RentDueDate = DateTime.Today.AddDays(3) }
        }.AsQueryable();

        var dbMock = new Mock<ApplicationDbContext>();
        dbMock.Setup(db => db.Tenants).Returns(DbSetMock.Create(tenants)); // Use a helper to mock DbSet

        var smsServiceMock = new Mock<ISmsService>();
        var service = new RentReminderService(null);

        // Act
        await service.SendRemindersAsync(dbMock.Object, smsServiceMock.Object);

        // Assert
        smsServiceMock.Verify(s => s.SendAsync("27821234567", It.IsAny<string>()), Times.Once);
    }
}
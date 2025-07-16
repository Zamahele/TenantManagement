using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Test.Infrastructure
{
    public class GenericRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GenericRepository<Room> _repository;

        public GenericRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GenericRepository<Room>(_context);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" }
            };
            _context.Rooms.AddRange(rooms);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.Number == "101");
            Assert.Contains(result, r => r.Number == "102");
        }

        [Fact]
        public async Task GetAllAsync_WithFilter_ReturnsFilteredEntities()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" },
                new Room { RoomId = 3, Number = "103", Type = "Single", Status = "Available" }
            };
            _context.Rooms.AddRange(rooms);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync(r => r.Type == "Single");

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, r => Assert.Equal("Single", r.Type));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsEntity()
        {
            // Arrange
            var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("101", result.Number);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_ValidEntity_AddsToDatabase()
        {
            // Arrange
            var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };

            // Act
            await _repository.AddAsync(room);

            // Assert
            var saved = await _context.Rooms.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("101", saved.Number);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesInDatabase()
        {
            // Arrange
            var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Act
            room.Status = "Occupied";
            await _repository.UpdateAsync(room);

            // Assert
            var updated = await _context.Rooms.FindAsync(1);
            Assert.Equal("Occupied", updated.Status);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
        {
            // Arrange
            var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(room);

            // Assert
            var deleted = await _context.Rooms.FindAsync(1);
            Assert.Null(deleted);
        }

        [Fact]
        public void Query_ReturnsQueryable()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" }
            };
            _context.Rooms.AddRange(rooms);
            _context.SaveChanges();

            // Act
            var query = _repository.Query();

            // Assert
            Assert.IsAssignableFrom<IQueryable<Room>>(query);
            Assert.Equal(2, query.Count());
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
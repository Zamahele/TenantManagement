using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data
{
  public static class DatabaseSeeder
  {
    public static void Seed(ApplicationDbContext context)
    {
      // Seed Manager (insert if does not exist)
      if (!context.Users.Any(u => u.Username == "Admin" && u.Role == "Manager"))
      {
        var manager = new User
        {
          Username = "Admin",
          PasswordHash = BCrypt.Net.BCrypt.HashPassword("01Pa$$w0rd2025#"),
          Role = "Manager"
        };
        context.Users.Add(manager);
        context.SaveChanges();
      }

      // Seed Tenants (Users) (insert if does not exist)
      var tenantUsers = new[]
      {
                new { Username = "tenant1", Password = "01Pa$$word", Role = "Tenant" },
                new { Username = "tenant2", Password = "01Pa$$word", Role = "Tenant" },
                new { Username = "tenant3", Password = "01Pa$$word", Role = "Tenant" },
                new { Username = "tenant4", Password = "01Pa$$word", Role = "Tenant" }
            };

      foreach (var t in tenantUsers)
      {
        if (!context.Users.Any(u => u.Username == t.Username && u.Role == t.Role))
        {
          context.Users.Add(new User
          {
            Username = t.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(t.Password),
            Role = t.Role
          });
        }
      }
      context.SaveChanges();

      // Seed Rooms (insert if does not exist)
      var rooms = new[]
      {
                new { Number = "001", Type = "Single", Status = "Occupied" },
                new { Number = "002", Type = "Single", Status = "Occupied" },
                new { Number = "003", Type = "Single", Status = "Occupied" },
                new { Number = "004", Type = "Single", Status = "Occupied" }
            };

      foreach (var r in rooms)
      {
        if (!context.Rooms.Any(room => room.Number == r.Number && room.Type == r.Type && room.Status == r.Status))
        {
          context.Rooms.Add(new Room
          {
            Number = r.Number,
            Type = r.Type,
            Status = r.Status,
            CottageId = null
          });
        }
      }
      context.SaveChanges();

      // Seed Tenants (Entities) (insert if does not exist)
      var userDict = context.Users.Where(u => u.Role == "Tenant").ToDictionary(u => u.Username, u => u.UserId);
      var roomDict = context.Rooms.ToDictionary(r => r.Number, r => r.RoomId);

      var tenants = new[]
      {
                new { FullName = "Njabulo Langa", Contact = "0821234567", Room = "001", User = "tenant1", EmergencyName = "Jane Doe", EmergencyNumber = "0831111111" },
                new { FullName = "Gcini", Contact = "0837654321", Room = "002", User = "tenant2", EmergencyName = "John Smith", EmergencyNumber = "0822222222" },
                new { FullName = "Nomthandazo Zungu", Contact = "0843334444", Room = "003", User = "tenant3", EmergencyName = "Bob Brown", EmergencyNumber = "0823334444" },
                new { FullName = "Thobeka", Contact = "0825556666", Room = "004", User = "tenant4", EmergencyName = "Alice White", EmergencyNumber = "0835556666" }
            };

      foreach (var t in tenants)
      {
        if (userDict.TryGetValue(t.User, out var userId) && roomDict.TryGetValue(t.Room, out var roomId))
        {
          if (!context.Tenants.Any(tenant =>
              tenant.UserId == userId &&
              tenant.RoomId == roomId &&
              tenant.FullName == t.FullName &&
              tenant.Contact == t.Contact &&
              tenant.EmergencyContactName == t.EmergencyName &&
              tenant.EmergencyContactNumber == t.EmergencyNumber))
          {
            context.Tenants.Add(new Tenant
            {
              FullName = t.FullName,
              Contact = t.Contact,
              RoomId = roomId,
              UserId = userId,
              EmergencyContactName = t.EmergencyName,
              EmergencyContactNumber = t.EmergencyNumber
            });
          }
        }
      }
      context.SaveChanges();
    }
  }
}
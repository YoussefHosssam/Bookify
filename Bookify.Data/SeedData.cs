using Bookify.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // ---------------------------
        // Seed Roles
        // ---------------------------
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        // ---------------------------
        // Seed Admin User
        // ---------------------------
        var adminEmail = "admin@bookify.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // ---------------------------
        // Seed Room Types
        // ---------------------------
        if (!await context.RoomTypes.AnyAsync())
        {
            var roomTypes = new[]
            {
                new RoomType
                {
                    Name = "Standard Room",
                    Description = "Comfortable standard room with basic amenities",
                    Capacity = 2,
                    BasePricePerNight = 100.00m,
                    Amenities = "Wi-Fi, TV, Air Conditioning, Mini Fridge",
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "Deluxe Room",
                    Description = "Spacious deluxe room with premium amenities",
                    Capacity = 3,
                    BasePricePerNight = 150.00m,
                    Amenities = "Wi-Fi, TV, Air Conditioning, Mini Fridge, Balcony, Room Service",
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "Suite",
                    Description = "Luxurious suite with separate living area",
                    Capacity = 4,
                    BasePricePerNight = 250.00m,
                    Amenities = "Wi-Fi, TV, Air Conditioning, Mini Fridge, Balcony, Room Service, Jacuzzi, Kitchenette",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.RoomTypes.AddRangeAsync(roomTypes);
            await context.SaveChangesAsync();
        }

        // ---------------------------
        // Seed Rooms
        // ---------------------------
        var roomTypesList = await context.RoomTypes.ToListAsync();
        var rooms = new List<Room>();

        foreach (var roomType in roomTypesList)
        {
            for (int i = 1; i <= 5; i++)
            {
                // Generate unique room number: first 3 letters of room type + room number
                var prefix = roomType.Name.Length >= 3 
                    ? roomType.Name.Substring(0, 3).ToUpper() 
                    : roomType.Name.ToUpper();
                var roomNumber = $"{prefix}{i:00}";

                // Only add if room doesn't exist
                if (!await context.Rooms.AnyAsync(r => r.RoomNumber == roomNumber))
                {
                    rooms.Add(new Room
                    {
                        RoomNumber = roomNumber,
                        RoomTypeId = roomType.Id,
                        Floor = i <= 2 ? 1 : 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        if (rooms.Any())
        {
            await context.Rooms.AddRangeAsync(rooms);
            await context.SaveChangesAsync();
        }

        // ---------------------------
        // Seed Room Images
        // ---------------------------
        var allRooms = await context.Rooms.ToListAsync();
        var roomImages = new List<RoomImage>();

        foreach (var room in allRooms)
        {
            // Only add image if room doesn't have any images
            if (!await context.RoomImages.AnyAsync(ri => ri.RoomId == room.Id))
            {
                roomImages.Add(new RoomImage
                {
                    RoomId = room.Id,
                    Url = "https://via.placeholder.com/800x600?text=Room+Image",
                    SortOrder = 1
                });
            }
        }

        if (roomImages.Any())
        {
            await context.RoomImages.AddRangeAsync(roomImages);
            await context.SaveChangesAsync();
        }
    }
}

using Bookify.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomImage> RoomImages { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RoomFeedback> RoomFeedbacks { get; set; }
    public DbSet<FavoriteRoom> FavoriteRooms { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RoomType
        builder.Entity<RoomType>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure Room
        builder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => e.RoomNumber).IsUnique();
            entity.HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure RoomImage
        builder.Entity<RoomImage>(entity =>
        {
            entity.HasOne(ri => ri.Room)
                .WithMany(r => r.Images)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Booking
        builder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.BookingNumber).IsUnique();
            entity.HasIndex(e => new { e.RoomId, e.CheckIn, e.CheckOut });
            entity.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.RoomType)
                .WithMany()
                .HasForeignKey(b => b.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Payment
        builder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure RoomFeedback
        builder.Entity<RoomFeedback>(entity =>
        {
            entity.HasIndex(e => new { e.RoomId, e.UserId });
            entity.HasOne(rf => rf.Room)
                .WithMany()
                .HasForeignKey(rf => rf.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FavoriteRoom
        builder.Entity<FavoriteRoom>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoomId }).IsUnique();
            entity.HasOne(fr => fr.Room)
                .WithMany()
                .HasForeignKey(fr => fr.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


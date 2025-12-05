namespace Bookify.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRoomRepository Rooms { get; }
    IBookingRepository Bookings { get; }
    IGenericRepository<Entities.RoomType> RoomTypes { get; }
    IGenericRepository<Entities.RoomImage> RoomImages { get; }
    IGenericRepository<Entities.Payment> Payments { get; }
    IGenericRepository<Entities.RoomFeedback> RoomFeedbacks { get; }
    IGenericRepository<Entities.FavoriteRoom> FavoriteRooms { get; }
    Task<int> CommitAsync();
}


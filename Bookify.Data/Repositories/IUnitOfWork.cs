namespace Bookify.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRoomRepository Rooms { get; }
    IBookingRepository Bookings { get; }
    IGenericRepository<Data.Entities.RoomType> RoomTypes { get; }
    IGenericRepository<Data.Entities.RoomImage> RoomImages { get; }
    IGenericRepository<Data.Entities.Payment> Payments { get; }
    Task<int> CommitAsync();
}


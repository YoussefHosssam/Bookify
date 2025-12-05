using Bookify.Data.Interfaces;

namespace Bookify.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IRoomRepository? _rooms;
    private IBookingRepository? _bookings;
    private IGenericRepository<Entities.RoomType>? _roomTypes;
    private IGenericRepository<Entities.RoomImage>? _roomImages;
    private IGenericRepository<Entities.Payment>? _payments;
    private IGenericRepository<Entities.RoomFeedback>? _roomFeedbacks;
    private IGenericRepository<Entities.FavoriteRoom>? _favoriteRooms;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRoomRepository Rooms
    {
        get
        {
            _rooms ??= new RoomRepository(_context);
            return _rooms;
        }
    }

    public IBookingRepository Bookings
    {
        get
        {
            _bookings ??= new BookingRepository(_context);
            return _bookings;
        }
    }

    public IGenericRepository<Entities.RoomType> RoomTypes
    {
        get
        {
            _roomTypes ??= new GenericRepository<Entities.RoomType>(_context);
            return _roomTypes;
        }
    }

    public IGenericRepository<Entities.RoomImage> RoomImages
    {
        get
        {
            _roomImages ??= new GenericRepository<Entities.RoomImage>(_context);
            return _roomImages;
        }
    }

    public IGenericRepository<Entities.Payment> Payments
    {
        get
        {
            _payments ??= new GenericRepository<Entities.Payment>(_context);
            return _payments;
        }
    }

    public IGenericRepository<Entities.RoomFeedback> RoomFeedbacks
    {
        get
        {
            _roomFeedbacks ??= new GenericRepository<Entities.RoomFeedback>(_context);
            return _roomFeedbacks;
        }
    }

    public IGenericRepository<Entities.FavoriteRoom> FavoriteRooms
    {
        get
        {
            _favoriteRooms ??= new GenericRepository<Entities.FavoriteRoom>(_context);
            return _favoriteRooms;
        }
    }

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


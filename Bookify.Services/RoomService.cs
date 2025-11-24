using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Repositories;
using Bookify.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<RoomDto>> SearchRoomsAsync(RoomSearchViewModel searchModel)
    {
        var checkIn = searchModel.CheckIn ?? DateTime.Today.AddDays(1);
        var checkOut = searchModel.CheckOut ?? DateTime.Today.AddDays(2);

        var rooms = await _unitOfWork.Rooms.GetAvailableRoomsAsync(checkIn, checkOut, searchModel);
        var orderedRooms = ApplySorting(rooms, searchModel.SortBy);

        var totalCount = orderedRooms.Count();
        var pagedRooms = orderedRooms
            .Skip((searchModel.Page - 1) * searchModel.PageSize)
            .Take(searchModel.PageSize)
            .ToList();

        var roomDtos = pagedRooms.Select(r => new RoomDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType.Name,
            Capacity = r.RoomType.Capacity,
            BasePricePerNight = r.RoomType.BasePricePerNight,
            Floor = r.Floor,
            IsActive = r.IsActive,
            Description = r.RoomType.Description,
            Amenities = r.RoomType.Amenities,
            ImageUrls = r.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).ToList()
        }).ToList();

        return new PagedResult<RoomDto>
        {
            Items = roomDtos,
            TotalCount = totalCount,
            Page = searchModel.Page,
            PageSize = searchModel.PageSize
        };
    }

    private static IEnumerable<Room> ApplySorting(IEnumerable<Room> rooms, string? sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "price-asc" => rooms.OrderBy(r => r.RoomType.BasePricePerNight),
            "price-desc" => rooms.OrderByDescending(r => r.RoomType.BasePricePerNight),
            "rating" => rooms.OrderByDescending(r => r.RoomType.Capacity),
            _ => rooms.OrderByDescending(r => r.Bookings.Count)
        };
    }

    public async Task<RoomDto?> GetRoomByIdAsync(int id)
    {
        var room = await _unitOfWork.Rooms.GetWithImagesAsync(id);
        if (room == null) return null;

        return new RoomDto
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.RoomType.Name,
            Capacity = room.RoomType.Capacity,
            BasePricePerNight = room.RoomType.BasePricePerNight,
            Floor = room.Floor,
            IsActive = room.IsActive,
            Description = room.RoomType.Description,
            Amenities = room.RoomType.Amenities,
            ImageUrls = room.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).ToList()
        };
    }

    public async Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
    {
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        return roomTypes.Select(rt => new RoomTypeDto
        {
            Id = rt.Id,
            Name = rt.Name,
            Description = rt.Description,
            Capacity = rt.Capacity,
            BasePricePerNight = rt.BasePricePerNight,
            Amenities = rt.Amenities,
            CreatedAt = rt.CreatedAt
        }).ToList();
    }

    public async Task<IEnumerable<RoomDto>> GetFeaturedRoomsAsync(int count = 6)
    {
        var rooms = await _unitOfWork.Rooms.GetFeaturedRoomsAsync(count);
        return rooms.Select(r => new RoomDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType.Name,
            Capacity = r.RoomType.Capacity,
            BasePricePerNight = r.RoomType.BasePricePerNight,
            Floor = r.Floor,
            IsActive = r.IsActive,
            Description = r.RoomType.Description,
            Amenities = r.RoomType.Amenities,
            ImageUrls = r.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).ToList()
        });
    }
}


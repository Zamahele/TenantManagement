using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class RoomApplicationService : IRoomApplicationService
{
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public RoomApplicationService(
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<RoomDto>>> GetAllRoomsAsync()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync();
            var roomDtos = _mapper.Map<IEnumerable<RoomDto>>(rooms);
            return ServiceResult<IEnumerable<RoomDto>>.Success(roomDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<RoomDto>>.Failure($"Error retrieving rooms: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<RoomWithTenantsDto>>> GetAllRoomsWithTenantsAsync()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync(null, r => r.Tenants);
            var roomDtos = _mapper.Map<IEnumerable<RoomWithTenantsDto>>(rooms);
            return ServiceResult<IEnumerable<RoomWithTenantsDto>>.Success(roomDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<RoomWithTenantsDto>>.Failure($"Error retrieving rooms with tenants: {ex.Message}");
        }
    }

    public async Task<ServiceResult<RoomDto>> GetRoomByIdAsync(int id)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<RoomDto>.Failure("Room not found");
            }

            var roomDto = _mapper.Map<RoomDto>(room);
            return ServiceResult<RoomDto>.Success(roomDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<RoomDto>.Failure($"Error retrieving room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<RoomWithTenantsDto>> GetRoomWithTenantsByIdAsync(int id)
    {
        try
        {
            var room = await _roomRepository.Query()
                .Include(r => r.Tenants)
                .FirstOrDefaultAsync(r => r.RoomId == id);
            
            if (room == null)
            {
                return ServiceResult<RoomWithTenantsDto>.Failure("Room not found");
            }

            var roomDto = _mapper.Map<RoomWithTenantsDto>(room);
            return ServiceResult<RoomWithTenantsDto>.Success(roomDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<RoomWithTenantsDto>.Failure($"Error retrieving room with tenants: {ex.Message}");
        }
    }

    public async Task<ServiceResult<RoomDto>> CreateRoomAsync(CreateRoomDto createRoomDto)
    {
        try
        {
            var room = _mapper.Map<Room>(createRoomDto);
            await _roomRepository.AddAsync(room);
            var roomDto = _mapper.Map<RoomDto>(room);
            return ServiceResult<RoomDto>.Success(roomDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<RoomDto>.Failure($"Error creating room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<RoomDto>> UpdateRoomAsync(int id, UpdateRoomDto updateRoomDto)
    {
        try
        {
            var existingRoom = await _roomRepository.GetByIdAsync(id);
            if (existingRoom == null)
            {
                return ServiceResult<RoomDto>.Failure("Room not found");
            }

            _mapper.Map(updateRoomDto, existingRoom);
            await _roomRepository.UpdateAsync(existingRoom);
            var roomDto = _mapper.Map<RoomDto>(existingRoom);
            return ServiceResult<RoomDto>.Success(roomDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<RoomDto>.Failure($"Error updating room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteRoomAsync(int id)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<bool>.Failure("Room not found");
            }

            await _roomRepository.DeleteAsync(room);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<RoomDto>>> GetAvailableRoomsAsync()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync(r => r.Status == "Available");
            var roomDtos = _mapper.Map<IEnumerable<RoomDto>>(rooms);
            return ServiceResult<IEnumerable<RoomDto>>.Success(roomDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<RoomDto>>.Failure($"Error retrieving available rooms: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<RoomDto>>> GetOccupiedRoomsAsync()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync(r => r.Status == "Occupied");
            var roomDtos = _mapper.Map<IEnumerable<RoomDto>>(rooms);
            return ServiceResult<IEnumerable<RoomDto>>.Success(roomDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<RoomDto>>.Failure($"Error retrieving occupied rooms: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateRoomStatusAsync(int roomId, string status)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null)
            {
                return ServiceResult<bool>.Failure("Room not found");
            }

            room.Status = status;
            await _roomRepository.UpdateAsync(room);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error updating room status: {ex.Message}");
        }
    }
}
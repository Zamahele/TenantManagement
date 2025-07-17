using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services;

public interface IRoomApplicationService
{
    Task<ServiceResult<IEnumerable<RoomDto>>> GetAllRoomsAsync();
    Task<ServiceResult<IEnumerable<RoomWithTenantsDto>>> GetAllRoomsWithTenantsAsync();
    Task<ServiceResult<RoomDto>> GetRoomByIdAsync(int id);
    Task<ServiceResult<RoomWithTenantsDto>> GetRoomWithTenantsByIdAsync(int id);
    Task<ServiceResult<RoomDto>> CreateRoomAsync(CreateRoomDto createRoomDto);
    Task<ServiceResult<RoomDto>> UpdateRoomAsync(int id, UpdateRoomDto updateRoomDto);
    Task<ServiceResult<bool>> DeleteRoomAsync(int id);
    Task<ServiceResult<IEnumerable<RoomDto>>> GetAvailableRoomsAsync();
    Task<ServiceResult<IEnumerable<RoomDto>>> GetOccupiedRoomsAsync();
    Task<ServiceResult<bool>> UpdateRoomStatusAsync(int roomId, string status);
}
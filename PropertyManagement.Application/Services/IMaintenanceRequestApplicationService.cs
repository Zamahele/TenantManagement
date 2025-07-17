using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services;

public interface IMaintenanceRequestApplicationService
{
    Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetAllMaintenanceRequestsAsync();
    Task<ServiceResult<MaintenanceRequestDto>> GetMaintenanceRequestByIdAsync(int id);
    Task<ServiceResult<MaintenanceRequestDto>> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDto createMaintenanceRequestDto);
    Task<ServiceResult<MaintenanceRequestDto>> UpdateMaintenanceRequestAsync(int id, UpdateMaintenanceRequestDto updateMaintenanceRequestDto);
    Task<ServiceResult<bool>> DeleteMaintenanceRequestAsync(int id);
    Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetPendingMaintenanceRequestsAsync();
    Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetCompletedMaintenanceRequestsAsync();
    Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequestsByRoomIdAsync(int roomId);
    Task<ServiceResult<bool>> AssignMaintenanceRequestAsync(int id, string assignedTo);
    Task<ServiceResult<bool>> CompleteMaintenanceRequestAsync(int id);
}
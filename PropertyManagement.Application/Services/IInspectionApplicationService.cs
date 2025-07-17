using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services;

public interface IInspectionApplicationService
{
    Task<ServiceResult<IEnumerable<InspectionDto>>> GetAllInspectionsAsync();
    Task<ServiceResult<InspectionDto>> GetInspectionByIdAsync(int id);
    Task<ServiceResult<InspectionDto>> CreateInspectionAsync(CreateInspectionDto createInspectionDto);
    Task<ServiceResult<InspectionDto>> UpdateInspectionAsync(int id, UpdateInspectionDto updateInspectionDto);
    Task<ServiceResult<bool>> DeleteInspectionAsync(int id);
    Task<ServiceResult<IEnumerable<InspectionDto>>> GetInspectionsByRoomIdAsync(int roomId);
    Task<ServiceResult<IEnumerable<InspectionDto>>> GetInspectionsByDateRangeAsync(DateTime startDate, DateTime endDate);
}
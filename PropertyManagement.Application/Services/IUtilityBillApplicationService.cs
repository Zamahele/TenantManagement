using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services;

public interface IUtilityBillApplicationService
{
    Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetAllUtilityBillsAsync();
    Task<ServiceResult<UtilityBillDto>> GetUtilityBillByIdAsync(int id);
    Task<ServiceResult<UtilityBillDto>> CreateUtilityBillAsync(CreateUtilityBillDto createUtilityBillDto);
    Task<ServiceResult<UtilityBillDto>> UpdateUtilityBillAsync(int id, UpdateUtilityBillDto updateUtilityBillDto);
    Task<ServiceResult<bool>> DeleteUtilityBillAsync(int id);
    Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetUtilityBillsByRoomIdAsync(int roomId);
    Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetUtilityBillsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<ServiceResult<decimal>> GetTotalUtilityBillsForRoomAsync(int roomId, DateTime startDate, DateTime endDate);
}
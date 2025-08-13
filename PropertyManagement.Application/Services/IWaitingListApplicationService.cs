using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Common;

namespace PropertyManagement.Application.Services;

public interface IWaitingListApplicationService
{
    // Basic CRUD operations
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetAllWaitingListEntriesAsync();
    Task<ServiceResult<WaitingListEntryDto>> GetWaitingListEntryByIdAsync(int id);
    Task<ServiceResult<WaitingListEntryDto>> CreateWaitingListEntryAsync(CreateWaitingListEntryDto createDto);
    Task<ServiceResult<WaitingListEntryDto>> UpdateWaitingListEntryAsync(int id, UpdateWaitingListEntryDto updateDto);
    Task<ServiceResult<bool>> DeleteWaitingListEntryAsync(int id);
    
    // Query operations
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetActiveWaitingListEntriesAsync();
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByStatusAsync(string status);
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByRoomTypeAsync(string roomType);
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByBudgetRangeAsync(decimal minBudget, decimal maxBudget);
    
    // Notification operations
    Task<ServiceResult<IEnumerable<WaitingListNotificationDto>>> GetNotificationHistoryAsync(int waitingListId);
    Task<ServiceResult<IEnumerable<WaitingListNotificationDto>>> GetAllNotificationsAsync();
    Task<ServiceResult<bool>> SendNotificationAsync(int waitingListId, string message, int? roomId = null);
    Task<ServiceResult<bool>> SendBulkNotificationAsync(IEnumerable<int> waitingListIds, string message, int? roomId = null);
    
    // Smart matching operations
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> FindMatchingEntriesForRoomAsync(int roomId);
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> FindMatchingEntriesForRoomTypeAsync(string roomType, decimal monthlyRent);
    
    // Analytics and reporting
    Task<ServiceResult<WaitingListSummaryDto>> GetWaitingListSummaryAsync();
    Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetRecentRegistrationsAsync(int days = 7);
    
    // Validation operations
    Task<ServiceResult<bool>> ValidatePhoneNumberAsync(string phoneNumber, int? excludeId = null);
    Task<ServiceResult<bool>> UpdateNotificationResponseAsync(int notificationId, string response);
}
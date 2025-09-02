using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class WaitingListApplicationService : IWaitingListApplicationService
{
    private readonly IGenericRepository<WaitingListEntry> _waitingListRepository;
    private readonly IGenericRepository<WaitingListNotification> _notificationRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public WaitingListApplicationService(
        IGenericRepository<WaitingListEntry> waitingListRepository,
        IGenericRepository<WaitingListNotification> notificationRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _waitingListRepository = waitingListRepository;
        _notificationRepository = notificationRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetAllWaitingListEntriesAsync()
    {
        try
        {
            var entries = await _waitingListRepository.GetAllAsync(null, w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving waiting list entries: {ex.Message}");
        }
    }

    public async Task<ServiceResult<WaitingListEntryDto>> GetWaitingListEntryByIdAsync(int id)
    {
        try
        {
            var entry = await _waitingListRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return ServiceResult<WaitingListEntryDto>.Failure("Waiting list entry not found");
            }

            // Load notifications separately if needed
            var allEntries = await _waitingListRepository.GetAllAsync(w => w.WaitingListId == id, w => w.Notifications);
            var entryWithNotifications = allEntries.FirstOrDefault();

            var entryDto = _mapper.Map<WaitingListEntryDto>(entryWithNotifications ?? entry);
            return ServiceResult<WaitingListEntryDto>.Success(entryDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<WaitingListEntryDto>.Failure($"Error retrieving waiting list entry: {ex.Message}");
        }
    }

    public async Task<ServiceResult<WaitingListEntryDto>> CreateWaitingListEntryAsync(CreateWaitingListEntryDto createDto)
    {
        try
        {
            // Validate phone number uniqueness
            var phoneValidation = await ValidatePhoneNumberAsync(createDto.PhoneNumber);
            if (!phoneValidation.IsSuccess || !phoneValidation.Data)
            {
                return ServiceResult<WaitingListEntryDto>.Failure("Phone number already exists in waiting list");
            }

            var entry = _mapper.Map<WaitingListEntry>(createDto);
            entry.RegisteredDate = DateTime.UtcNow;
            entry.Status = "Active";
            entry.IsActive = true;

            await _waitingListRepository.AddAsync(entry);
            var entryDto = _mapper.Map<WaitingListEntryDto>(entry);
            return ServiceResult<WaitingListEntryDto>.Success(entryDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<WaitingListEntryDto>.Failure($"Error creating waiting list entry: {ex.Message}");
        }
    }

    public async Task<ServiceResult<WaitingListEntryDto>> UpdateWaitingListEntryAsync(int id, UpdateWaitingListEntryDto updateDto)
    {
        try
        {
            var entry = await _waitingListRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return ServiceResult<WaitingListEntryDto>.Failure("Waiting list entry not found");
            }

            // Validate phone number uniqueness if changed
            if (entry.PhoneNumber != updateDto.PhoneNumber)
            {
                var phoneValidation = await ValidatePhoneNumberAsync(updateDto.PhoneNumber, id);
                if (!phoneValidation.IsSuccess || !phoneValidation.Data)
                {
                    return ServiceResult<WaitingListEntryDto>.Failure("Phone number already exists in waiting list");
                }
            }

            _mapper.Map(updateDto, entry);
            await _waitingListRepository.UpdateAsync(entry);
            
            var entryDto = _mapper.Map<WaitingListEntryDto>(entry);
            return ServiceResult<WaitingListEntryDto>.Success(entryDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<WaitingListEntryDto>.Failure($"Error updating waiting list entry: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteWaitingListEntryAsync(int id)
    {
        try
        {
            var entry = await _waitingListRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return ServiceResult<bool>.Failure("Waiting list entry not found");
            }

            await _waitingListRepository.DeleteAsync(entry);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting waiting list entry: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetActiveWaitingListEntriesAsync()
    {
        try
        {
            var entries = await _waitingListRepository.GetAllAsync(w => w.IsActive && w.Status == "Active", w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving active waiting list entries: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByStatusAsync(string status)
    {
        try
        {
            var entries = await _waitingListRepository.GetAllAsync(w => w.Status == status, w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving waiting list entries by status: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByRoomTypeAsync(string roomType)
    {
        try
        {
            var entries = await _waitingListRepository.GetAllAsync(
                w => w.IsActive && (w.PreferredRoomType == roomType || w.PreferredRoomType == "Any"),
                w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving waiting list entries by room type: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetWaitingListEntriesByBudgetRangeAsync(decimal minBudget, decimal maxBudget)
    {
        try
        {
            var entries = await _waitingListRepository.GetAllAsync(
                w => w.IsActive && (w.MaxBudget == null || (w.MaxBudget >= minBudget && w.MaxBudget <= maxBudget)),
                w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving waiting list entries by budget range: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListNotificationDto>>> GetNotificationHistoryAsync(int waitingListId)
    {
        try
        {
            var notifications = await _notificationRepository.GetAllAsync(n => n.WaitingListId == waitingListId, n => n.Room);
            var sortedNotifications = notifications.OrderByDescending(n => n.SentDate).ToList();
            var notificationDtos = _mapper.Map<IEnumerable<WaitingListNotificationDto>>(sortedNotifications);
            return ServiceResult<IEnumerable<WaitingListNotificationDto>>.Success(notificationDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListNotificationDto>>.Failure($"Error retrieving notification history: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListNotificationDto>>> GetAllNotificationsAsync()
    {
        try
        {
            var notifications = await _notificationRepository.GetAllAsync(null, n => n.WaitingListEntry, n => n.Room);
            var sortedNotifications = notifications.OrderByDescending(n => n.SentDate).ToList();
            var notificationDtos = _mapper.Map<IEnumerable<WaitingListNotificationDto>>(sortedNotifications);
            return ServiceResult<IEnumerable<WaitingListNotificationDto>>.Success(notificationDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListNotificationDto>>.Failure($"Error retrieving all notifications: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> SendNotificationAsync(int waitingListId, string message, int? roomId = null)
    {
        try
        {
            var entry = await _waitingListRepository.GetByIdAsync(waitingListId);
            if (entry == null)
            {
                return ServiceResult<bool>.Failure("Waiting list entry not found");
            }

            var notification = new WaitingListNotification
            {
                WaitingListId = waitingListId,
                RoomId = roomId,
                MessageContent = message,
                SentDate = DateTime.UtcNow,
                Status = "Sent"
            };

            await _notificationRepository.AddAsync(notification);

            // Update entry notification count and last notified date
            entry.NotificationCount++;
            entry.LastNotified = DateTime.UtcNow;
            await _waitingListRepository.UpdateAsync(entry);

            // Note: SMS functionality has been removed. Notifications are logged in the database only.
            // Implement your preferred notification method here (email, push notifications, etc.)

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error sending notification: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> SendBulkNotificationAsync(IEnumerable<int> waitingListIds, string message, int? roomId = null)
    {
        try
        {
            var notifications = new List<WaitingListNotification>();
            var entriesToUpdate = new List<WaitingListEntry>();

            foreach (var id in waitingListIds)
            {
                var entry = await _waitingListRepository.GetByIdAsync(id);
                if (entry != null)
                {
                    notifications.Add(new WaitingListNotification
                    {
                        WaitingListId = id,
                        RoomId = roomId,
                        MessageContent = message,
                        SentDate = DateTime.UtcNow,
                        Status = "Sent"
                    });

                    entry.NotificationCount++;
                    entry.LastNotified = DateTime.UtcNow;
                    entriesToUpdate.Add(entry);
                }
            }

            // Add all notifications
            foreach (var notification in notifications)
            {
                await _notificationRepository.AddAsync(notification);
            }

            // Update all entries
            foreach (var entry in entriesToUpdate)
            {
                await _waitingListRepository.UpdateAsync(entry);
            }

            // Note: SMS functionality has been removed. Notifications are logged in the database only.
            // Implement your preferred notification method here (email, push notifications, etc.)

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error sending bulk notifications: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> FindMatchingEntriesForRoomAsync(int roomId)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null)
            {
                return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure("Room not found");
            }

            // For now, use a default rent value since Room doesn't have MonthlyRent property
        // This will be enhanced in Phase 2 when room pricing is properly integrated
        return await FindMatchingEntriesForRoomTypeAsync(room.Type, 1000m);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error finding matching entries: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> FindMatchingEntriesForRoomTypeAsync(string roomType, decimal monthlyRent)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Don't notify if already notified in last 7 days
            
            var entries = await _waitingListRepository.GetAllAsync(
                w => w.IsActive && 
                           w.Status == "Active" &&
                           (w.PreferredRoomType == "Any" || w.PreferredRoomType == roomType) &&
                           (w.MaxBudget == null || w.MaxBudget >= monthlyRent) &&
                           (w.LastNotified == null || w.LastNotified < cutoffDate),
                w => w.Notifications);
            
            var sortedEntries = entries.OrderBy(w => w.RegisteredDate).ToList(); // First come, first served

            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error finding matching entries: {ex.Message}");
        }
    }

    public async Task<ServiceResult<WaitingListSummaryDto>> GetWaitingListSummaryAsync()
    {
        try
        {
            var allEntries = await _waitingListRepository.GetAllAsync(null, w => w.Notifications);
            var allNotifications = await _notificationRepository.GetAllAsync();

            var summary = new WaitingListSummaryDto
            {
                TotalEntries = allEntries.Count(),
                ActiveEntries = allEntries.Count(w => w.IsActive && w.Status == "Active"),
                NotifiedThisWeek = allEntries.Count(w => w.LastNotified >= DateTime.UtcNow.AddDays(-7)),
                ConvertedThisMonth = allEntries.Count(w => w.Status == "Converted" && w.RegisteredDate >= DateTime.UtcNow.AddDays(-30)),
                TotalNotificationsSent = allNotifications.Count(),
                NewRegistrationsThisWeek = allEntries.Count(w => w.RegisteredDate >= DateTime.UtcNow.AddDays(-7)),
                MostRequestedRoomType = allEntries
                    .Where(w => !string.IsNullOrEmpty(w.PreferredRoomType) && w.PreferredRoomType != "Any")
                    .GroupBy(w => w.PreferredRoomType)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "None",
                AverageMaxBudget = allEntries
                    .Where(w => w.MaxBudget.HasValue)
                    .Select(w => w.MaxBudget.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            // Calculate conversion rate
            if (summary.TotalEntries > 0)
            {
                summary.ConversionRate = (double)summary.ConvertedThisMonth / summary.TotalEntries * 100;
            }

            return ServiceResult<WaitingListSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return ServiceResult<WaitingListSummaryDto>.Failure($"Error generating summary: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<WaitingListEntryDto>>> GetRecentRegistrationsAsync(int days = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var entries = await _waitingListRepository.GetAllAsync(w => w.RegisteredDate >= cutoffDate, w => w.Notifications);
            var sortedEntries = entries.OrderByDescending(w => w.RegisteredDate).ToList();
            var entryDtos = _mapper.Map<IEnumerable<WaitingListEntryDto>>(sortedEntries);
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entryDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure($"Error retrieving recent registrations: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ValidatePhoneNumberAsync(string phoneNumber, int? excludeId = null)
    {
        try
        {
            var existing = await _waitingListRepository.GetAllAsync(
                w => w.PhoneNumber == phoneNumber && (excludeId == null || w.WaitingListId != excludeId));
            return ServiceResult<bool>.Success(!existing.Any());
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error validating phone number: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateNotificationResponseAsync(int notificationId, string response)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return ServiceResult<bool>.Failure("Notification not found");
            }

            notification.Response = response;
            notification.ResponseDate = DateTime.UtcNow;
            notification.Status = "Responded";

            await _notificationRepository.UpdateAsync(notification);

            // Update waiting list entry status based on response
            var entry = await _waitingListRepository.GetByIdAsync(notification.WaitingListId);
            if (entry != null)
            {
                if (response.ToLower().Contains("not interested") || response.ToLower().Contains("stop"))
                {
                    entry.Status = "OptedOut";
                    entry.IsActive = false;
                }
                else if (response.ToLower().Contains("interested") || response.ToLower().Contains("yes"))
                {
                    entry.Status = "Interested";
                }
                
                await _waitingListRepository.UpdateAsync(entry);
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error updating notification response: {ex.Message}");
        }
    }
}
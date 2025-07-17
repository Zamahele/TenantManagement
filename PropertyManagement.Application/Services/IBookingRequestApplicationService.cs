using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services;

public interface IBookingRequestApplicationService
{
    Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetAllBookingRequestsAsync();
    Task<ServiceResult<BookingRequestDto>> GetBookingRequestByIdAsync(int id);
    Task<ServiceResult<BookingRequestDto>> CreateBookingRequestAsync(CreateBookingRequestDto createBookingRequestDto);
    Task<ServiceResult<BookingRequestDto>> UpdateBookingRequestAsync(int id, UpdateBookingRequestDto updateBookingRequestDto);
    Task<ServiceResult<bool>> DeleteBookingRequestAsync(int id);
    Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetPendingBookingRequestsAsync();
    Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetConfirmedBookingRequestsAsync();
    Task<ServiceResult<bool>> UpdateBookingRequestStatusAsync(int id, BookingRequestStatusDto statusDto);
    Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetBookingRequestsByRoomIdAsync(int roomId);
}
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class BookingRequestApplicationService : IBookingRequestApplicationService
{
    private readonly IGenericRepository<BookingRequest> _bookingRequestRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public BookingRequestApplicationService(
        IGenericRepository<BookingRequest> bookingRequestRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _bookingRequestRepository = bookingRequestRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetAllBookingRequestsAsync()
    {
        try
        {
            var bookingRequests = await _bookingRequestRepository.GetAllAsync(null, br => br.Room);
            var bookingRequestDtos = _mapper.Map<IEnumerable<BookingRequestDto>>(bookingRequests);
            return ServiceResult<IEnumerable<BookingRequestDto>>.Success(bookingRequestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<BookingRequestDto>>.Failure($"Error retrieving booking requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BookingRequestDto>> GetBookingRequestByIdAsync(int id)
    {
        try
        {
            var bookingRequest = await _bookingRequestRepository.Query()
                .Include(br => br.Room)
                .FirstOrDefaultAsync(br => br.BookingRequestId == id);
            
            if (bookingRequest == null)
            {
                return ServiceResult<BookingRequestDto>.Failure("Booking request not found");
            }

            var bookingRequestDto = _mapper.Map<BookingRequestDto>(bookingRequest);
            return ServiceResult<BookingRequestDto>.Success(bookingRequestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<BookingRequestDto>.Failure($"Error retrieving booking request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BookingRequestDto>> CreateBookingRequestAsync(CreateBookingRequestDto createBookingRequestDto)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(createBookingRequestDto.RoomId);
            if (room == null)
            {
                return ServiceResult<BookingRequestDto>.Failure("Room not found");
            }

            var bookingRequest = _mapper.Map<BookingRequest>(createBookingRequestDto);
            bookingRequest.RequestDate = DateTime.Now;
            bookingRequest.Status = "Pending";

            await _bookingRequestRepository.AddAsync(bookingRequest);
            var bookingRequestDto = _mapper.Map<BookingRequestDto>(bookingRequest);
            return ServiceResult<BookingRequestDto>.Success(bookingRequestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<BookingRequestDto>.Failure($"Error creating booking request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BookingRequestDto>> UpdateBookingRequestAsync(int id, UpdateBookingRequestDto updateBookingRequestDto)
    {
        try
        {
            var existingBookingRequest = await _bookingRequestRepository.GetByIdAsync(id);
            if (existingBookingRequest == null)
            {
                return ServiceResult<BookingRequestDto>.Failure("Booking request not found");
            }

            _mapper.Map(updateBookingRequestDto, existingBookingRequest);
            await _bookingRequestRepository.UpdateAsync(existingBookingRequest);
            var bookingRequestDto = _mapper.Map<BookingRequestDto>(existingBookingRequest);
            return ServiceResult<BookingRequestDto>.Success(bookingRequestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<BookingRequestDto>.Failure($"Error updating booking request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteBookingRequestAsync(int id)
    {
        try
        {
            var bookingRequest = await _bookingRequestRepository.GetByIdAsync(id);
            if (bookingRequest == null)
            {
                return ServiceResult<bool>.Failure("Booking request not found");
            }

            await _bookingRequestRepository.DeleteAsync(bookingRequest);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting booking request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetPendingBookingRequestsAsync()
    {
        try
        {
            var bookingRequests = await _bookingRequestRepository.GetAllAsync(br => br.Status == "Pending", br => br.Room);
            var bookingRequestDtos = _mapper.Map<IEnumerable<BookingRequestDto>>(bookingRequests);
            return ServiceResult<IEnumerable<BookingRequestDto>>.Success(bookingRequestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<BookingRequestDto>>.Failure($"Error retrieving pending booking requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetConfirmedBookingRequestsAsync()
    {
        try
        {
            var bookingRequests = await _bookingRequestRepository.GetAllAsync(br => br.Status == "Confirmed", br => br.Room);
            var bookingRequestDtos = _mapper.Map<IEnumerable<BookingRequestDto>>(bookingRequests);
            return ServiceResult<IEnumerable<BookingRequestDto>>.Success(bookingRequestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<BookingRequestDto>>.Failure($"Error retrieving confirmed booking requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateBookingRequestStatusAsync(int id, BookingRequestStatusDto statusDto)
    {
        try
        {
            var bookingRequest = await _bookingRequestRepository.GetByIdAsync(id);
            if (bookingRequest == null)
            {
                return ServiceResult<bool>.Failure("Booking request not found");
            }

            bookingRequest.Status = statusDto.Status;
            bookingRequest.Note = statusDto.Note;
            await _bookingRequestRepository.UpdateAsync(bookingRequest);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error updating booking request status: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<BookingRequestDto>>> GetBookingRequestsByRoomIdAsync(int roomId)
    {
        try
        {
            var bookingRequests = await _bookingRequestRepository.GetAllAsync(br => br.RoomId == roomId, br => br.Room);
            var bookingRequestDtos = _mapper.Map<IEnumerable<BookingRequestDto>>(bookingRequests);
            return ServiceResult<IEnumerable<BookingRequestDto>>.Success(bookingRequestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<BookingRequestDto>>.Failure($"Error retrieving booking requests for room: {ex.Message}");
        }
    }
}
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class MaintenanceRequestApplicationService : IMaintenanceRequestApplicationService
{
    private readonly IGenericRepository<MaintenanceRequest> _maintenanceRequestRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public MaintenanceRequestApplicationService(
        IGenericRepository<MaintenanceRequest> maintenanceRequestRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _maintenanceRequestRepository = maintenanceRequestRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetAllMaintenanceRequestsAsync()
    {
        try
        {
            var requests = await _maintenanceRequestRepository.GetAllAsync(null, mr => mr.Room);
            var requestDtos = _mapper.Map<IEnumerable<MaintenanceRequestDto>>(requests);
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Failure($"Error retrieving maintenance requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> GetMaintenanceRequestByIdAsync(int id)
    {
        try
        {
            var request = await _maintenanceRequestRepository.Query()
                .Include(mr => mr.Room)
                .FirstOrDefaultAsync(mr => mr.MaintenanceRequestId == id);
            
            if (request == null)
            {
                return ServiceResult<MaintenanceRequestDto>.Failure("Maintenance request not found");
            }

            var requestDto = _mapper.Map<MaintenanceRequestDto>(request);
            return ServiceResult<MaintenanceRequestDto>.Success(requestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure($"Error retrieving maintenance request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDto createMaintenanceRequestDto)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(createMaintenanceRequestDto.RoomId);
            if (room == null)
            {
                return ServiceResult<MaintenanceRequestDto>.Failure("Room not found");
            }

            var request = _mapper.Map<MaintenanceRequest>(createMaintenanceRequestDto);
            request.RequestDate = DateTime.Now;
            request.Status = "Pending";

            await _maintenanceRequestRepository.AddAsync(request);
            var requestDto = _mapper.Map<MaintenanceRequestDto>(request);
            return ServiceResult<MaintenanceRequestDto>.Success(requestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure($"Error creating maintenance request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> UpdateMaintenanceRequestAsync(int id, UpdateMaintenanceRequestDto updateMaintenanceRequestDto)
    {
        try
        {
            var existingRequest = await _maintenanceRequestRepository.GetByIdAsync(id);
            if (existingRequest == null)
            {
                return ServiceResult<MaintenanceRequestDto>.Failure("Maintenance request not found");
            }

            _mapper.Map(updateMaintenanceRequestDto, existingRequest);
            await _maintenanceRequestRepository.UpdateAsync(existingRequest);
            var requestDto = _mapper.Map<MaintenanceRequestDto>(existingRequest);
            return ServiceResult<MaintenanceRequestDto>.Success(requestDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure($"Error updating maintenance request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteMaintenanceRequestAsync(int id)
    {
        try
        {
            var request = await _maintenanceRequestRepository.GetByIdAsync(id);
            if (request == null)
            {
                return ServiceResult<bool>.Failure("Maintenance request not found");
            }

            await _maintenanceRequestRepository.DeleteAsync(request);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting maintenance request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetPendingMaintenanceRequestsAsync()
    {
        try
        {
            var requests = await _maintenanceRequestRepository.GetAllAsync(mr => mr.Status == "Pending", mr => mr.Room);
            var requestDtos = _mapper.Map<IEnumerable<MaintenanceRequestDto>>(requests);
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Failure($"Error retrieving pending maintenance requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetCompletedMaintenanceRequestsAsync()
    {
        try
        {
            var requests = await _maintenanceRequestRepository.GetAllAsync(mr => mr.Status == "Completed", mr => mr.Room);
            var requestDtos = _mapper.Map<IEnumerable<MaintenanceRequestDto>>(requests);
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Failure($"Error retrieving completed maintenance requests: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequestsByRoomIdAsync(int roomId)
    {
        try
        {
            var requests = await _maintenanceRequestRepository.GetAllAsync(mr => mr.RoomId == roomId, mr => mr.Room);
            var requestDtos = _mapper.Map<IEnumerable<MaintenanceRequestDto>>(requests);
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requestDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<MaintenanceRequestDto>>.Failure($"Error retrieving maintenance requests for room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> AssignMaintenanceRequestAsync(int id, string assignedTo)
    {
        try
        {
            var request = await _maintenanceRequestRepository.GetByIdAsync(id);
            if (request == null)
            {
                return ServiceResult<bool>.Failure("Maintenance request not found");
            }

            request.AssignedTo = assignedTo;
            request.Status = "In Progress";
            await _maintenanceRequestRepository.UpdateAsync(request);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error assigning maintenance request: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> CompleteMaintenanceRequestAsync(int id)
    {
        try
        {
            var request = await _maintenanceRequestRepository.GetByIdAsync(id);
            if (request == null)
            {
                return ServiceResult<bool>.Failure("Maintenance request not found");
            }

            request.Status = "Completed";
            request.CompletedDate = DateTime.Now;
            await _maintenanceRequestRepository.UpdateAsync(request);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error completing maintenance request: {ex.Message}");
        }
    }
}
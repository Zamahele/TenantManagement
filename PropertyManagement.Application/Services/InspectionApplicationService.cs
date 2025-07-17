using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class InspectionApplicationService : IInspectionApplicationService
{
    private readonly IGenericRepository<Inspection> _inspectionRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public InspectionApplicationService(
        IGenericRepository<Inspection> inspectionRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _inspectionRepository = inspectionRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<InspectionDto>>> GetAllInspectionsAsync()
    {
        try
        {
            var inspections = await _inspectionRepository.GetAllAsync(null, i => i.Room);
            var inspectionDtos = _mapper.Map<IEnumerable<InspectionDto>>(inspections);
            return ServiceResult<IEnumerable<InspectionDto>>.Success(inspectionDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<InspectionDto>>.Failure($"Error retrieving inspections: {ex.Message}");
        }
    }

    public async Task<ServiceResult<InspectionDto>> GetInspectionByIdAsync(int id)
    {
        try
        {
            var inspection = await _inspectionRepository.Query()
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.InspectionId == id);
            
            if (inspection == null)
            {
                return ServiceResult<InspectionDto>.Failure("Inspection not found");
            }

            var inspectionDto = _mapper.Map<InspectionDto>(inspection);
            return ServiceResult<InspectionDto>.Success(inspectionDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<InspectionDto>.Failure($"Error retrieving inspection: {ex.Message}");
        }
    }

    public async Task<ServiceResult<InspectionDto>> CreateInspectionAsync(CreateInspectionDto createInspectionDto)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(createInspectionDto.RoomId);
            if (room == null)
            {
                return ServiceResult<InspectionDto>.Failure("Room not found");
            }

            var inspection = _mapper.Map<Inspection>(createInspectionDto);
            await _inspectionRepository.AddAsync(inspection);
            var inspectionDto = _mapper.Map<InspectionDto>(inspection);
            return ServiceResult<InspectionDto>.Success(inspectionDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<InspectionDto>.Failure($"Error creating inspection: {ex.Message}");
        }
    }

    public async Task<ServiceResult<InspectionDto>> UpdateInspectionAsync(int id, UpdateInspectionDto updateInspectionDto)
    {
        try
        {
            var existingInspection = await _inspectionRepository.GetByIdAsync(id);
            if (existingInspection == null)
            {
                return ServiceResult<InspectionDto>.Failure("Inspection not found");
            }

            _mapper.Map(updateInspectionDto, existingInspection);
            await _inspectionRepository.UpdateAsync(existingInspection);
            var inspectionDto = _mapper.Map<InspectionDto>(existingInspection);
            return ServiceResult<InspectionDto>.Success(inspectionDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<InspectionDto>.Failure($"Error updating inspection: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteInspectionAsync(int id)
    {
        try
        {
            var inspection = await _inspectionRepository.GetByIdAsync(id);
            if (inspection == null)
            {
                return ServiceResult<bool>.Failure("Inspection not found");
            }

            await _inspectionRepository.DeleteAsync(inspection);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting inspection: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<InspectionDto>>> GetInspectionsByRoomIdAsync(int roomId)
    {
        try
        {
            var inspections = await _inspectionRepository.GetAllAsync(i => i.RoomId == roomId, i => i.Room);
            var inspectionDtos = _mapper.Map<IEnumerable<InspectionDto>>(inspections);
            return ServiceResult<IEnumerable<InspectionDto>>.Success(inspectionDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<InspectionDto>>.Failure($"Error retrieving inspections for room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<InspectionDto>>> GetInspectionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var inspections = await _inspectionRepository.GetAllAsync(
                i => i.Date >= startDate && i.Date <= endDate, 
                i => i.Room);
            var inspectionDtos = _mapper.Map<IEnumerable<InspectionDto>>(inspections);
            return ServiceResult<IEnumerable<InspectionDto>>.Success(inspectionDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<InspectionDto>>.Failure($"Error retrieving inspections by date range: {ex.Message}");
        }
    }
}
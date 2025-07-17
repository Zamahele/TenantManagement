using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class UtilityBillApplicationService : IUtilityBillApplicationService
{
    private readonly IGenericRepository<UtilityBill> _utilityBillRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public UtilityBillApplicationService(
        IGenericRepository<UtilityBill> utilityBillRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _utilityBillRepository = utilityBillRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetAllUtilityBillsAsync()
    {
        try
        {
            var utilityBills = await _utilityBillRepository.GetAllAsync(null, ub => ub.Room);
            var utilityBillDtos = _mapper.Map<IEnumerable<UtilityBillDto>>(utilityBills);
            return ServiceResult<IEnumerable<UtilityBillDto>>.Success(utilityBillDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<UtilityBillDto>>.Failure($"Error retrieving utility bills: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UtilityBillDto>> GetUtilityBillByIdAsync(int id)
    {
        try
        {
            var utilityBill = await _utilityBillRepository.Query()
                .Include(ub => ub.Room)
                .FirstOrDefaultAsync(ub => ub.UtilityBillId == id);
            
            if (utilityBill == null)
            {
                return ServiceResult<UtilityBillDto>.Failure("Utility bill not found");
            }

            var utilityBillDto = _mapper.Map<UtilityBillDto>(utilityBill);
            return ServiceResult<UtilityBillDto>.Success(utilityBillDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<UtilityBillDto>.Failure($"Error retrieving utility bill: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UtilityBillDto>> CreateUtilityBillAsync(CreateUtilityBillDto createUtilityBillDto)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(createUtilityBillDto.RoomId);
            if (room == null)
            {
                return ServiceResult<UtilityBillDto>.Failure("Room not found");
            }

            var utilityBill = _mapper.Map<UtilityBill>(createUtilityBillDto);
            await _utilityBillRepository.AddAsync(utilityBill);
            var utilityBillDto = _mapper.Map<UtilityBillDto>(utilityBill);
            return ServiceResult<UtilityBillDto>.Success(utilityBillDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<UtilityBillDto>.Failure($"Error creating utility bill: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UtilityBillDto>> UpdateUtilityBillAsync(int id, UpdateUtilityBillDto updateUtilityBillDto)
    {
        try
        {
            var existingUtilityBill = await _utilityBillRepository.GetByIdAsync(id);
            if (existingUtilityBill == null)
            {
                return ServiceResult<UtilityBillDto>.Failure("Utility bill not found");
            }

            _mapper.Map(updateUtilityBillDto, existingUtilityBill);
            await _utilityBillRepository.UpdateAsync(existingUtilityBill);
            var utilityBillDto = _mapper.Map<UtilityBillDto>(existingUtilityBill);
            return ServiceResult<UtilityBillDto>.Success(utilityBillDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<UtilityBillDto>.Failure($"Error updating utility bill: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteUtilityBillAsync(int id)
    {
        try
        {
            var utilityBill = await _utilityBillRepository.GetByIdAsync(id);
            if (utilityBill == null)
            {
                return ServiceResult<bool>.Failure("Utility bill not found");
            }

            await _utilityBillRepository.DeleteAsync(utilityBill);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting utility bill: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetUtilityBillsByRoomIdAsync(int roomId)
    {
        try
        {
            var utilityBills = await _utilityBillRepository.GetAllAsync(ub => ub.RoomId == roomId, ub => ub.Room);
            var utilityBillDtos = _mapper.Map<IEnumerable<UtilityBillDto>>(utilityBills);
            return ServiceResult<IEnumerable<UtilityBillDto>>.Success(utilityBillDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<UtilityBillDto>>.Failure($"Error retrieving utility bills for room: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<UtilityBillDto>>> GetUtilityBillsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var utilityBills = await _utilityBillRepository.GetAllAsync(
                ub => ub.BillingDate >= startDate && ub.BillingDate <= endDate, 
                ub => ub.Room);
            var utilityBillDtos = _mapper.Map<IEnumerable<UtilityBillDto>>(utilityBills);
            return ServiceResult<IEnumerable<UtilityBillDto>>.Success(utilityBillDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<UtilityBillDto>>.Failure($"Error retrieving utility bills by date range: {ex.Message}");
        }
    }

    public async Task<ServiceResult<decimal>> GetTotalUtilityBillsForRoomAsync(int roomId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var utilityBills = await _utilityBillRepository.GetAllAsync(
                ub => ub.RoomId == roomId && ub.BillingDate >= startDate && ub.BillingDate <= endDate);
            
            var total = utilityBills.Sum(ub => ub.TotalAmount);
            return ServiceResult<decimal>.Success(total);
        }
        catch (Exception ex)
        {
            return ServiceResult<decimal>.Failure($"Error calculating total utility bills: {ex.Message}");
        }
    }
}
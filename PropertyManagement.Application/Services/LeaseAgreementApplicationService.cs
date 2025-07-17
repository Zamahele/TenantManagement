using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class LeaseAgreementApplicationService : ILeaseAgreementApplicationService
{
    private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public LeaseAgreementApplicationService(
        IGenericRepository<LeaseAgreement> leaseAgreementRepository,
        IGenericRepository<Tenant> tenantRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _leaseAgreementRepository = leaseAgreementRepository;
        _tenantRepository = tenantRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetAllLeaseAgreementsAsync()
    {
        try
        {
            var agreements = await _leaseAgreementRepository.Query()
                .Include(l => l.Tenant)
                    .ThenInclude(t => t.Room)
                .Include(l => l.Room)
                .ToListAsync();

            var agreementDtos = _mapper.Map<IEnumerable<LeaseAgreementDto>>(agreements);
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(agreementDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Failure($"Error retrieving lease agreements: {ex.Message}");
        }
    }

    public async Task<ServiceResult<LeaseAgreementDto>> GetLeaseAgreementByIdAsync(int id)
    {
        try
        {
            var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
            if (agreement == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Lease agreement not found");
            }

            var agreementDto = _mapper.Map<LeaseAgreementDto>(agreement);
            return ServiceResult<LeaseAgreementDto>.Success(agreementDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<LeaseAgreementDto>.Failure($"Error retrieving lease agreement: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetLeaseAgreementsByTenantIdAsync(int tenantId)
    {
        try
        {
            var agreements = await _leaseAgreementRepository.GetAllAsync(la => la.TenantId == tenantId, la => la.Tenant, la => la.Room);
            var agreementDtos = _mapper.Map<IEnumerable<LeaseAgreementDto>>(agreements);
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(agreementDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Failure($"Error retrieving lease agreements for tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<LeaseAgreementDto>> CreateLeaseAgreementAsync(CreateLeaseAgreementDto createLeaseAgreementDto)
    {
        try
        {
            // Business rule: Validate end date is after start date
            if (createLeaseAgreementDto.EndDate <= createLeaseAgreementDto.StartDate)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("End Date must be after Start Date");
            }

            // Business rule: Validate tenant exists
            var tenant = await _tenantRepository.GetByIdAsync(createLeaseAgreementDto.TenantId);
            if (tenant == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Selected tenant does not exist");
            }

            // Business rule: Validate room exists
            var room = await _roomRepository.GetByIdAsync(createLeaseAgreementDto.RoomId);
            if (room == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Selected room does not exist");
            }

            // Business rule: Validate rent amount
            if (createLeaseAgreementDto.RentAmount <= 0)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Rent amount must be greater than zero");
            }

            // Business rule: Validate expected rent day
            if (createLeaseAgreementDto.ExpectedRentDay < 1 || createLeaseAgreementDto.ExpectedRentDay > 31)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Expected rent day must be between 1 and 31");
            }

            var agreement = new LeaseAgreement
            {
                TenantId = createLeaseAgreementDto.TenantId,
                RoomId = createLeaseAgreementDto.RoomId,
                StartDate = createLeaseAgreementDto.StartDate,
                EndDate = createLeaseAgreementDto.EndDate,
                RentAmount = createLeaseAgreementDto.RentAmount,
                ExpectedRentDay = createLeaseAgreementDto.ExpectedRentDay,
                FilePath = createLeaseAgreementDto.FilePath
            };

            await _leaseAgreementRepository.AddAsync(agreement);

            var agreementDto = _mapper.Map<LeaseAgreementDto>(agreement);
            return ServiceResult<LeaseAgreementDto>.Success(agreementDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<LeaseAgreementDto>.Failure($"Error creating lease agreement: {ex.Message}");
        }
    }

    public async Task<ServiceResult<LeaseAgreementDto>> UpdateLeaseAgreementAsync(int id, UpdateLeaseAgreementDto updateLeaseAgreementDto)
    {
        try
        {
            var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
            if (agreement == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Lease agreement not found");
            }

            // Business rule: Validate end date is after start date
            if (updateLeaseAgreementDto.EndDate <= updateLeaseAgreementDto.StartDate)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("End Date must be after Start Date");
            }

            // Business rule: Validate tenant exists
            var tenant = await _tenantRepository.GetByIdAsync(updateLeaseAgreementDto.TenantId);
            if (tenant == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Selected tenant does not exist");
            }

            // Business rule: Validate room exists
            var room = await _roomRepository.GetByIdAsync(updateLeaseAgreementDto.RoomId);
            if (room == null)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Selected room does not exist");
            }

            // Business rule: Validate rent amount
            if (updateLeaseAgreementDto.RentAmount <= 0)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Rent amount must be greater than zero");
            }

            // Business rule: Validate expected rent day
            if (updateLeaseAgreementDto.ExpectedRentDay < 1 || updateLeaseAgreementDto.ExpectedRentDay > 31)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("Expected rent day must be between 1 and 31");
            }

            // Update properties manually to avoid navigation property issues
            agreement.TenantId = updateLeaseAgreementDto.TenantId;
            agreement.RoomId = updateLeaseAgreementDto.RoomId;
            agreement.StartDate = updateLeaseAgreementDto.StartDate;
            agreement.EndDate = updateLeaseAgreementDto.EndDate;
            agreement.RentAmount = updateLeaseAgreementDto.RentAmount;
            agreement.ExpectedRentDay = updateLeaseAgreementDto.ExpectedRentDay;
            
            if (!string.IsNullOrEmpty(updateLeaseAgreementDto.FilePath))
            {
                agreement.FilePath = updateLeaseAgreementDto.FilePath;
            }

            await _leaseAgreementRepository.UpdateAsync(agreement);

            var agreementDto = _mapper.Map<LeaseAgreementDto>(agreement);
            return ServiceResult<LeaseAgreementDto>.Success(agreementDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<LeaseAgreementDto>.Failure($"Error updating lease agreement: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteLeaseAgreementAsync(int id)
    {
        try
        {
            var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
            if (agreement == null)
            {
                return ServiceResult<bool>.Failure("Lease agreement not found");
            }

            await _leaseAgreementRepository.DeleteAsync(agreement);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting lease agreement: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetExpiringLeaseAgreementsAsync(int daysAhead = 30)
    {
        try
        {
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(daysAhead);

            var agreements = await _leaseAgreementRepository.Query()
                .Where(a => a.EndDate > now && a.EndDate <= futureDate)
                .Include(l => l.Tenant)
                    .ThenInclude(t => t.Room)
                .Include(l => l.Room)
                .ToListAsync();

            var agreementDtos = _mapper.Map<IEnumerable<LeaseAgreementDto>>(agreements);
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(agreementDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Failure($"Error retrieving expiring lease agreements: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetOverdueLeaseAgreementsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            var agreements = await _leaseAgreementRepository.Query()
                .Where(a => a.EndDate < now)
                .Include(l => l.Tenant)
                    .ThenInclude(t => t.Room)
                .Include(l => l.Room)
                .ToListAsync();

            var agreementDtos = _mapper.Map<IEnumerable<LeaseAgreementDto>>(agreements);
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(agreementDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<LeaseAgreementDto>>.Failure($"Error retrieving overdue lease agreements: {ex.Message}");
        }
    }

    public async Task<ServiceResult<int>> GetRoomIdByTenantIdAsync(int tenantId)
    {
        try
        {
            var roomId = await _tenantRepository.Query()
                .Where(t => t.TenantId == tenantId)
                .Select(t => t.RoomId)
                .FirstOrDefaultAsync();

            return ServiceResult<int>.Success(roomId);
        }
        catch (Exception ex)
        {
            return ServiceResult<int>.Failure($"Error retrieving room ID for tenant: {ex.Message}");
        }
    }
}
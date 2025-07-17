using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Common;

namespace PropertyManagement.Application.Services;

public interface ILeaseAgreementApplicationService
{
    Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetAllLeaseAgreementsAsync();
    Task<ServiceResult<LeaseAgreementDto>> GetLeaseAgreementByIdAsync(int id);
    Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetLeaseAgreementsByTenantIdAsync(int tenantId);
    Task<ServiceResult<LeaseAgreementDto>> CreateLeaseAgreementAsync(CreateLeaseAgreementDto createLeaseAgreementDto);
    Task<ServiceResult<LeaseAgreementDto>> UpdateLeaseAgreementAsync(int id, UpdateLeaseAgreementDto updateLeaseAgreementDto);
    Task<ServiceResult<bool>> DeleteLeaseAgreementAsync(int id);
    Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetExpiringLeaseAgreementsAsync(int daysAhead = 30);
    Task<ServiceResult<IEnumerable<LeaseAgreementDto>>> GetOverdueLeaseAgreementsAsync();
    Task<ServiceResult<int>> GetRoomIdByTenantIdAsync(int tenantId);
}
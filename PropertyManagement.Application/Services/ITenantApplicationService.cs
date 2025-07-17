using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Common;

namespace PropertyManagement.Application.Services;

public interface ITenantApplicationService
{
    Task<ServiceResult<IEnumerable<TenantDto>>> GetAllTenantsAsync();
    Task<ServiceResult<TenantDto>> GetTenantByIdAsync(int id);
    Task<ServiceResult<TenantDto>> GetTenantByUserIdAsync(int userId);
    Task<ServiceResult<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto);
    Task<ServiceResult<TenantDto>> UpdateTenantAsync(int id, UpdateTenantDto updateTenantDto);
    Task<ServiceResult<bool>> DeleteTenantAsync(int id);
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username, int? excludeUserId = null);
    Task<ServiceResult<bool>> ValidateContactAsync(string contact, int? excludeTenantId = null);
    Task<ServiceResult<UserDto>> AuthenticateAsync(string username, string password);
    Task<ServiceResult<TenantDto>> RegisterTenantAsync(RegisterTenantDto registerTenantDto);
    Task<ServiceResult<bool>> ChangePasswordAsync(int tenantId, string currentPassword, string newPassword);
    Task<ServiceResult<TenantDto>> UpdateProfileAsync(int tenantId, UpdateProfileDto updateProfileDto);
}
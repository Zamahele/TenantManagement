using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Services
{
    public interface ILeaseGenerationService
    {
        Task<ServiceResult<string>> GenerateLeaseHtmlAsync(int leaseAgreementId, int? templateId = null);
        Task<ServiceResult<string>> GenerateLeasePdfAsync(int leaseAgreementId, string htmlContent);
        Task<ServiceResult<LeaseAgreementSigningDto>> GetLeaseForSigningAsync(int leaseAgreementId, int tenantId);
        Task<ServiceResult<DigitalSignatureDto>> SignLeaseAsync(SignLeaseDto signLeaseDto);
        Task<ServiceResult<bool>> SendLeaseToTenantAsync(int leaseAgreementId);
        Task<ServiceResult<byte[]>> DownloadSignedLeaseAsync(int leaseAgreementId, int tenantId);
        Task<ServiceResult<IEnumerable<LeaseTemplateDto>>> GetLeaseTemplatesAsync();
        Task<ServiceResult<LeaseTemplateDto>> CreateLeaseTemplateAsync(CreateLeaseTemplateDto createTemplateDto);
        Task<ServiceResult<LeaseTemplateDto>> UpdateLeaseTemplateAsync(int id, UpdateLeaseTemplateDto updateTemplateDto);
        Task<ServiceResult<bool>> DeleteLeaseTemplateAsync(int id);
        Task<ServiceResult<LeaseTemplateDto>> GetDefaultLeaseTemplateAsync();
    }
}
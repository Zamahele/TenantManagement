using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Common;

namespace PropertyManagement.Application.Services;

public interface IPaymentApplicationService
{
    Task<ServiceResult<IEnumerable<PaymentDto>>> GetAllPaymentsAsync();
    Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(int id);
    Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsByTenantIdAsync(int tenantId);
    Task<ServiceResult<PaymentDto>> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
    Task<ServiceResult<PaymentDto>> UpdatePaymentAsync(int id, UpdatePaymentDto updatePaymentDto);
    Task<ServiceResult<bool>> DeletePaymentAsync(int id);
    Task<ServiceResult<decimal>> GetOutstandingBalanceAsync(int tenantId);
    Task<ServiceResult<IEnumerable<TenantOutstandingDto>>> GetOutstandingBalancesAsync();
    Task<ServiceResult<PaymentReceiptDto>> GeneratePaymentReceiptAsync(int paymentId);
}
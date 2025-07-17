using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class PaymentApplicationService : IPaymentApplicationService
{
    private readonly IGenericRepository<Payment> _paymentRepository;
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
    private readonly IMapper _mapper;

    public PaymentApplicationService(
        IGenericRepository<Payment> paymentRepository,
        IGenericRepository<Tenant> tenantRepository,
        IGenericRepository<LeaseAgreement> leaseAgreementRepository,
        IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _tenantRepository = tenantRepository;
        _leaseAgreementRepository = leaseAgreementRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<PaymentDto>>> GetAllPaymentsAsync()
    {
        try
        {
            var payments = await _paymentRepository.GetAllAsync(null, p => p.Tenant, p => p.LeaseAgreement);
            var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);
            return ServiceResult<IEnumerable<PaymentDto>>.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<PaymentDto>>.Failure($"Error retrieving payments: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(int id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return ServiceResult<PaymentDto>.Failure("Payment not found");
            }

            var paymentDto = _mapper.Map<PaymentDto>(payment);
            return ServiceResult<PaymentDto>.Success(paymentDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentDto>.Failure($"Error retrieving payment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsByTenantIdAsync(int tenantId)
    {
        try
        {
            var payments = await _paymentRepository.GetAllAsync(p => p.TenantId == tenantId, p => p.Tenant, p => p.LeaseAgreement);
            var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);
            return ServiceResult<IEnumerable<PaymentDto>>.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<PaymentDto>>.Failure($"Error retrieving payments for tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentDto>> CreatePaymentAsync(CreatePaymentDto createPaymentDto)
    {
        try
        {
            // Business rule: Validate tenant exists
            var tenant = await _tenantRepository.GetByIdAsync(createPaymentDto.TenantId);
            if (tenant == null)
            {
                return ServiceResult<PaymentDto>.Failure("Tenant not found");
            }

            // Business rule: Validate payment amount
            if (createPaymentDto.Amount <= 0)
            {
                return ServiceResult<PaymentDto>.Failure("Payment amount must be greater than zero");
            }

            // Business rule: Validate payment date
            if (createPaymentDto.PaymentDate > DateTime.Now)
            {
                return ServiceResult<PaymentDto>.Failure("Payment date cannot be in the future");
            }

            // Business rule: Find associated lease agreement
            var leaseAgreement = await _leaseAgreementRepository.Query()
                .FirstOrDefaultAsync(la => la.TenantId == createPaymentDto.TenantId);

            var payment = new Payment
            {
                TenantId = createPaymentDto.TenantId,
                LeaseAgreementId = leaseAgreement?.LeaseAgreementId,
                Amount = createPaymentDto.Amount,
                Date = createPaymentDto.PaymentDate,
                Type = createPaymentDto.Type,
                ReceiptPath = createPaymentDto.ReceiptPath,
                PaymentMonth = createPaymentDto.PaymentMonth,
                PaymentYear = createPaymentDto.PaymentYear
            };

            await _paymentRepository.AddAsync(payment);

            var paymentDto = _mapper.Map<PaymentDto>(payment);
            return ServiceResult<PaymentDto>.Success(paymentDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentDto>.Failure($"Error creating payment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentDto>> UpdatePaymentAsync(int id, UpdatePaymentDto updatePaymentDto)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return ServiceResult<PaymentDto>.Failure("Payment not found");
            }

            // Business rule: Validate payment amount
            if (updatePaymentDto.Amount <= 0)
            {
                return ServiceResult<PaymentDto>.Failure("Payment amount must be greater than zero");
            }

            // Business rule: Validate payment date
            if (updatePaymentDto.PaymentDate > DateTime.Now)
            {
                return ServiceResult<PaymentDto>.Failure("Payment date cannot be in the future");
            }

            // Update payment properties
            payment.Amount = updatePaymentDto.Amount;
            payment.Date = updatePaymentDto.PaymentDate;
            payment.Type = updatePaymentDto.Type;
            payment.ReceiptPath = updatePaymentDto.ReceiptPath;
            payment.PaymentMonth = updatePaymentDto.PaymentMonth;
            payment.PaymentYear = updatePaymentDto.PaymentYear;

            await _paymentRepository.UpdateAsync(payment);

            var paymentDto = _mapper.Map<PaymentDto>(payment);
            return ServiceResult<PaymentDto>.Success(paymentDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentDto>.Failure($"Error updating payment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeletePaymentAsync(int id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return ServiceResult<bool>.Failure("Payment not found");
            }

            await _paymentRepository.DeleteAsync(payment);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting payment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<decimal>> GetOutstandingBalanceAsync(int tenantId)
    {
        try
        {
            // Business rule: Calculate outstanding balance
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<decimal>.Failure("Tenant not found");
            }

            var leaseAgreement = await _leaseAgreementRepository.Query()
                .FirstOrDefaultAsync(la => la.TenantId == tenantId);

            if (leaseAgreement == null)
            {
                return ServiceResult<decimal>.Success(0);
            }

            var payments = await _paymentRepository.GetAllAsync(p => p.TenantId == tenantId);
            var totalPaid = payments.Sum(p => p.Amount);

            // Calculate months since lease start
            var monthsSinceStart = ((DateTime.Now.Year - leaseAgreement.StartDate.Year) * 12) + 
                                  (DateTime.Now.Month - leaseAgreement.StartDate.Month);

            if (monthsSinceStart <= 0)
            {
                return ServiceResult<decimal>.Success(0);
            }

            var expectedTotal = monthsSinceStart * leaseAgreement.RentAmount;
            var outstandingBalance = Math.Max(0, expectedTotal - totalPaid);

            return ServiceResult<decimal>.Success(outstandingBalance);
        }
        catch (Exception ex)
        {
            return ServiceResult<decimal>.Failure($"Error calculating outstanding balance: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<TenantOutstandingDto>>> GetOutstandingBalancesAsync()
    {
        try
        {
            var tenants = await _tenantRepository.GetAllAsync(null, t => t.Room);
            var outstandingBalances = new List<TenantOutstandingDto>();

            foreach (var tenant in tenants)
            {
                var balanceResult = await GetOutstandingBalanceAsync(tenant.TenantId);
                if (balanceResult.IsSuccess && balanceResult.Data > 0)
                {
                    var lastPayment = await _paymentRepository.Query()
                        .Where(p => p.TenantId == tenant.TenantId)
                        .OrderByDescending(p => p.Date)
                        .FirstOrDefaultAsync();

                    outstandingBalances.Add(new TenantOutstandingDto
                    {
                        TenantId = tenant.TenantId,
                        FullName = tenant.FullName,
                        Contact = tenant.Contact,
                        RoomNumber = tenant.Room?.Number ?? "Unknown",
                        OutstandingBalance = balanceResult.Data,
                        LastPaymentDate = lastPayment?.Date ?? DateTime.MinValue
                    });
                }
            }

            return ServiceResult<IEnumerable<TenantOutstandingDto>>.Success(outstandingBalances);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<TenantOutstandingDto>>.Failure($"Error retrieving outstanding balances: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentReceiptDto>> GeneratePaymentReceiptAsync(int paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetAllAsync(p => p.PaymentId == paymentId, p => p.Tenant, p => p.Tenant.Room);
            var paymentData = payment.FirstOrDefault();

            if (paymentData == null)
            {
                return ServiceResult<PaymentReceiptDto>.Failure("Payment not found");
            }

            var receipt = new PaymentReceiptDto
            {
                PaymentId = paymentData.PaymentId,
                TenantName = paymentData.Tenant.FullName,
                RoomNumber = paymentData.Tenant.Room?.Number ?? "Unknown",
                Amount = paymentData.Amount,
                PaymentDate = paymentData.Date,
                Type = paymentData.Type ?? "",
                PaymentPeriod = $"{paymentData.PaymentMonth:D2}/{paymentData.PaymentYear}",
                ReceiptNumber = $"REC-{paymentData.PaymentId:D6}-{paymentData.PaymentYear}"
            };

            return ServiceResult<PaymentReceiptDto>.Success(receipt);
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentReceiptDto>.Failure($"Error generating payment receipt: {ex.Message}");
        }
    }
}
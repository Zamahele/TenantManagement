using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Test.Infrastructure
{
    /// <summary>
    /// Centralized mapping profiles for test projects to ensure consistency
    /// and reduce duplication across test files.
    /// </summary>
    public static class TestMappingProfiles
    {
        public static IMapper GetTestMapper()
        {
            var expr = new MapperConfigurationExpression();
            
            // Core Entity to DTO mappings
            expr.CreateMap<Tenant, TenantDto>().ReverseMap();
            expr.CreateMap<Room, RoomDto>().ReverseMap();
            expr.CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Date))
                .ReverseMap()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate));
            expr.CreateMap<LeaseAgreement, LeaseAgreementDto>().ReverseMap();
            expr.CreateMap<User, UserDto>().ReverseMap();
            expr.CreateMap<UtilityBill, UtilityBillDto>().ReverseMap();
            expr.CreateMap<MaintenanceRequest, MaintenanceRequestDto>().ReverseMap();
            expr.CreateMap<BookingRequest, BookingRequestDto>().ReverseMap();
            expr.CreateMap<Inspection, InspectionDto>().ReverseMap();
            expr.CreateMap<DigitalSignature, DigitalSignatureDto>().ReverseMap();
            expr.CreateMap<LeaseTemplate, LeaseTemplateDto>().ReverseMap();

            // Waiting List mappings
            expr.CreateMap<WaitingListEntry, WaitingListEntryDto>().ReverseMap();
            expr.CreateMap<WaitingListNotification, WaitingListNotificationDto>().ReverseMap();
            expr.CreateMap<WaitingListSummaryDto, WaitingListSummaryViewModel>().ReverseMap();

            // DTO to ViewModel mappings
            expr.CreateMap<TenantDto, TenantViewModel>().ReverseMap();
            expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
            expr.CreateMap<PaymentDto, PaymentViewModel>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
                .ReverseMap()
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Date));
            expr.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>().ReverseMap();
            expr.CreateMap<UserDto, UserViewModel>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));
            expr.CreateMap<UtilityBillDto, UtilityBillViewModel>().ReverseMap();
            expr.CreateMap<UtilityBillDto, UtilityBillFormViewModel>().ReverseMap();
            expr.CreateMap<MaintenanceRequestDto, MaintenanceRequestViewModel>().ReverseMap();
            expr.CreateMap<BookingRequestDto, BookingRequestViewModel>().ReverseMap();
            expr.CreateMap<InspectionDto, InspectionViewModel>().ReverseMap();
            expr.CreateMap<DigitalSignatureDto, DigitalSignatureViewModel>().ReverseMap();
            expr.CreateMap<LeaseTemplateDto, LeaseTemplateViewModel>().ReverseMap();

            // Waiting List DTO to ViewModel mappings
            expr.CreateMap<WaitingListEntryDto, WaitingListEntryViewModel>().ReverseMap();
            expr.CreateMap<WaitingListNotificationDto, WaitingListNotificationViewModel>().ReverseMap();

            // ViewModel to Create/Update DTO mappings
            expr.CreateMap<TenantViewModel, CreateTenantDto>();
            expr.CreateMap<TenantViewModel, UpdateTenantDto>();
            expr.CreateMap<RoomViewModel, CreateRoomDto>();
            expr.CreateMap<RoomViewModel, UpdateRoomDto>();
            expr.CreateMap<RoomFormViewModel, CreateRoomDto>();
            expr.CreateMap<RoomFormViewModel, UpdateRoomDto>();
            expr.CreateMap<PaymentViewModel, CreatePaymentDto>()
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Date));
            expr.CreateMap<PaymentViewModel, UpdatePaymentDto>()
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Date));
            expr.CreateMap<LeaseAgreementViewModel, CreateLeaseAgreementDto>();
            expr.CreateMap<LeaseAgreementViewModel, UpdateLeaseAgreementDto>();
            expr.CreateMap<UtilityBillViewModel, CreateUtilityBillDto>();
            expr.CreateMap<UtilityBillViewModel, UpdateUtilityBillDto>();
            expr.CreateMap<MaintenanceRequestViewModel, CreateMaintenanceRequestDto>();
            expr.CreateMap<MaintenanceRequestViewModel, UpdateMaintenanceRequestDto>();
            expr.CreateMap<BookingRequestViewModel, CreateBookingRequestDto>();
            expr.CreateMap<BookingRequestViewModel, UpdateBookingRequestDto>();
            expr.CreateMap<InspectionViewModel, CreateInspectionDto>();
            expr.CreateMap<InspectionViewModel, UpdateInspectionDto>();
            expr.CreateMap<LeaseTemplateViewModel, CreateLeaseTemplateDto>();
            expr.CreateMap<LeaseTemplateViewModel, UpdateLeaseTemplateDto>();

            // Waiting List ViewModel to Create/Update DTO mappings
            expr.CreateMap<WaitingListEntryViewModel, CreateWaitingListEntryDto>();
            expr.CreateMap<WaitingListEntryViewModel, UpdateWaitingListEntryDto>();
            expr.CreateMap<QuickAddWaitingListViewModel, CreateWaitingListEntryDto>();
            expr.CreateMap<WaitingListEntryDto, UpdateWaitingListEntryDto>();

            // Special mappings for complex scenarios
            expr.CreateMap<RegisterTenantDto, TenantDto>();
            expr.CreateMap<UpdateProfileDto, TenantDto>();

            var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }
    }
}
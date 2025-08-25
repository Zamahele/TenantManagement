using AutoMapper;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Web.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // UtilityBill mappings
            CreateMap<UtilityBillDto, UtilityBillViewModel>()
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
                .ReverseMap();
            CreateMap<UtilityBillDto, UtilityBillFormViewModel>().ReverseMap();
            // MaintenanceRequest mappings
            CreateMap<MaintenanceRequestDto, MaintenanceRequestViewModel>()
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
                .ReverseMap();
            CreateMap<MaintenanceRequestDto, MaintenanceRequestFormViewModel>().ReverseMap();
            // LeaseAgreement mappings
            CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
                .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
                .ForMember(dest => dest.RentDueDate, opt => opt.Ignore())
                .ReverseMap();
            // Room mappings
            CreateMap<RoomDto, RoomViewModel>().ReverseMap();
            CreateMap<RoomDto, RoomFormViewModel>().ReverseMap();
            // Tenant mappings
            CreateMap<TenantDto, TenantViewModel>()
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
                .ReverseMap();
            // Payment mappings
            CreateMap<PaymentDto, PaymentViewModel>().ReverseMap();
            // BookingRequest mappings
            CreateMap<BookingRequestDto, BookingRequestViewModel>().ReverseMap();
            // Inspection mappings
            CreateMap<InspectionDto, InspectionViewModel>().ReverseMap();
            // DigitalSignature mappings
            CreateMap<DigitalSignatureDto, DigitalSignatureViewModel>().ReverseMap();
            // LeaseTemplate mappings
            CreateMap<LeaseTemplateDto, LeaseTemplateViewModel>().ReverseMap();
            // WaitingList mappings
            CreateMap<WaitingListEntryDto, WaitingListEntryViewModel>().ReverseMap();
            CreateMap<WaitingListNotificationDto, WaitingListNotificationViewModel>().ReverseMap();
            CreateMap<WaitingListSummaryDto, WaitingListSummaryViewModel>().ReverseMap();
        }
    }
}

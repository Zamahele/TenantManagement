using AutoMapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.ViewModels;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
    CreateMap<PaymentViewModel, Payment>()
        .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.PaymentId ?? 0))
        .ForMember(dest => dest.Date, opt => opt.Ignore()); // Set in controller
           
        CreateMap<Payment, PaymentViewModel>();
    }
}
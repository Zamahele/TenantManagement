using System;
using Microsoft.AspNetCore.Http;

namespace PropertyManagement.Web.ViewModels
{
  public class LeaseAgreementViewModel
  {
    public int LeaseAgreementId { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
    public IFormFile? File { get; set; }
    public DateTime? RentDueDate { get; set; }
    public TenantViewModel? Tenant { get; set; }
    public RoomViewModel? Room { get; set; }
  }
}
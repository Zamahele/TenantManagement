using System.Collections.Generic;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels
{
  public class TenantOutstandingViewModel
  {
    public int TenantId { get; set; }
    public string FullName { get; set; }
    public Room? Room { get; set; }
    public List<LeaseAgreement> LeaseAgreements { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
  }
}
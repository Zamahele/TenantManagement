using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels
{
  public class RoomFormViewModel
  {
    public int RoomId { get; set; }
    public int? CottageId { get; set; } 
    public string? Number { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public IEnumerable<SelectListItem> ? StatusOptions { get; set; }
  }
}

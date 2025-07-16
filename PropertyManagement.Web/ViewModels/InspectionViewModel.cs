using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels
{
    public class InspectionViewModel
    {
        public int InspectionId { get; set; }

        [Required]
        public int RoomId { get; set; }

        public RoomViewModel? Room { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [MaxLength(200)]
        public string? Result { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
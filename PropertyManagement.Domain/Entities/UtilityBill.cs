using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Domain.Entities
{
    public class UtilityBill
    {
        public int UtilityBillId { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BillingDate { get; set; }

        [Required]
        public decimal WaterUsage { get; set; }

        [Required]
        public decimal ElectricityUsage { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int BranchId { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string Note { get; set; } = "";
        public string Reference { get; set; } = "";
        
        public int? ShipmentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

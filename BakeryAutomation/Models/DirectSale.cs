using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class DirectSale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string Note { get; set; } = "";
        public List<DirectSaleItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

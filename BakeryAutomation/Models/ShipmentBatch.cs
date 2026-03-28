using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{


    public sealed class ShipmentBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string BatchNo { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public int BranchId { get; set; }
        public string Notes { get; set; } = "";

        // Batch-level discount (iskonto %)
        public decimal BatchDiscountPercent { get; set; }

        public List<ShipmentItem> Items { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

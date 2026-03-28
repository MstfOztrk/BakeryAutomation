using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class ReturnReceipt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string ReturnNo { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public int BranchId { get; set; }
        public string Notes { get; set; } = "";

        public List<ReturnReceiptItem> Items { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class Branch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public BranchType Type { get; set; } = BranchType.Branch;
        public string Address { get; set; } = "";

        public string TypeDisplay => Type switch
        {
            BranchType.Branch => "Sube",
            BranchType.Market => "Market",
            BranchType.Grocery => "Bakkal",
            _ => Type.ToString()
        };

        public string ContactName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string PaymentTerms { get; set; } = "";
        public int? PaymentDayOfMonth { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class PriceChange
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }
        
        public DateTime At { get; set; } = DateTime.Now;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string Note { get; set; } = "";
    }
}

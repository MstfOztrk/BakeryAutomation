using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public UnitType UnitType { get; set; } = UnitType.Piece;
        public bool IsActive { get; set; } = true;

        public string UnitTypeDisplay => UnitType switch
        {
            UnitType.Piece => "Adet",
            UnitType.Kilogram => "Kg",
            UnitType.Tray => "Tava",
            _ => UnitType.ToString()
        };

        public decimal DefaultUnitPrice { get; set; }
        public List<PriceChange> PriceHistory { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

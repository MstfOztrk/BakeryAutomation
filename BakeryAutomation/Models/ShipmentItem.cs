using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryAutomation.Models
{
    public sealed class ShipmentItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public UnitType UnitType { get; set; } = UnitType.Piece;

        private decimal _quantitySent;
        public decimal QuantitySent { get => _quantitySent; set => Set(ref _quantitySent, value); }
        
        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }

        private decimal _itemDiscountPercent;
        public decimal ItemDiscountPercent { get => _itemDiscountPercent; set => Set(ref _itemDiscountPercent, value); }

        private decimal _quantityReturned;
        public decimal QuantityReturned { get => _quantityReturned; set => Set(ref _quantityReturned, value); }
        
        private decimal _quantityWasted;
        public decimal QuantityWasted { get => _quantityWasted; set => Set(ref _quantityWasted, value); }

        [NotMapped]
        public string UnitTypeDisplay => UnitType switch
        {
            UnitType.Piece => "Adet",
            UnitType.Kilogram => "Kg",
            UnitType.Tray => "Tava",
            _ => UnitType.ToString()
        };

        [NotMapped]
        public decimal TotalLinePrice => QuantitySent * UnitPrice;
    }
}

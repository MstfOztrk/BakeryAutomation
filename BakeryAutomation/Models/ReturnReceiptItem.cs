using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace BakeryAutomation.Models
{
    public sealed class ReturnReceiptItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        public int ReturnReceiptId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public UnitType UnitType { get; set; } = UnitType.Piece;

        private decimal _quantity;
        public decimal Quantity { get => _quantity; set => Set(ref _quantity, value); }

        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }

        public int? SourceShipmentId { get; set; }
        public int? SourceShipmentItemId { get; set; }

        [NotMapped]
        public string UnitTypeDisplay => UnitType switch
        {
            UnitType.Piece => "Adet",
            UnitType.Kilogram => "Kg",
            UnitType.Tray => "Tava",
            _ => UnitType.ToString()
        };

        [NotMapped]
        public decimal TotalLinePrice => Quantity * UnitPrice;

        [NotMapped]
        public string SourceShipmentDisplay { get; set; } = "";
    }
}

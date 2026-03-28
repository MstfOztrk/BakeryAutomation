using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BakeryAutomation.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; private set; } = new LocalizationService();

        private readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new();

        private string _currentCulture = "tr";
        public string CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture == value) return;
                _currentCulture = value;
                OnPropertyChanged();
                OnPropertyChanged("Item[]");
            }
        }

        public string this[string key] => GetString(key);

        public LocalizationService()
        {
            _dictionaries["tr"] = new Dictionary<string, string>
            {
                { "ConfirmDelete", "Bu kaydi silmek istediginize emin misiniz?" },
                { "ConfirmDeleteShipment", "Bu sevkiyat fisini silmek istediginize emin misiniz?" },
                { "ConfirmDeletePayment", "Bu tahsilati silmek istediginize emin misiniz?" },
                { "ConfirmDeleteRow", "Secili satiri silmek istediginize emin misiniz?" },
                { "Confirm", "Onay" },
                { "Yes", "Evet" },
                { "No", "Hayir" },
                { "Save", "Kaydet" },
                { "Delete", "Sil" },
                { "Cancel", "Iptal" },
                { "New", "Yeni" },
                { "Refresh", "Yenile" },
                { "Search", "Ara..." },
                { "Active", "Aktif" },
                { "Date", "Tarih" },
                { "Note", "Not" },
                { "Total", "Toplam" },
                { "Amount", "Tutar" },
                { "ProductsTitle", "Urunler" },
                { "Product", "Urun" },
                { "Category", "Kategori" },
                { "Unit", "Birim" },
                { "UnitPrice", "Birim Fiyat" },
                { "PriceHistory", "Fiyat Gecmisi" },
                { "OldPrice", "Eski Fiyat" },
                { "NewPrice", "Yeni Fiyat" },
                { "BranchesTitle", "Subeler / Cariler" },
                { "Branch", "Sube" },
                { "Type", "Tip" },
                { "Address", "Adres" },
                { "Contact", "Yetkili" },
                { "Phone", "Telefon" },
                { "CreditLimit", "Kredi Limiti" },
                { "PaymentTerms", "Vade (Gun)" },
                { "PriceOverrides", "Fiyat Tanimlari" },
                { "ViewStatement", "Ekstre Gor" },
                { "ShipmentsTitle", "Sevkiyat Fisleri" },
                { "ShipmentNo", "Fis No" },
                { "Recalculate", "Hesapla" },
                { "Print", "Yazdir" },
                { "Export", "Disa Aktar" },
                { "Sent", "Gonder" },
                { "Returned", "Iade" },
                { "Wasted", "Zayi" },
                { "Discount", "Iskonto" },
                { "GeneralTotal", "Genel Toplam" },
                { "SubTotal", "Ara Toplam" },
                { "TotalDiscount", "Toplam Iskonto" },
                { "AddToShipment", "Urun Ekle" },
                { "PaymentsTitle", "Tahsilat" },
                { "PaymentRecord", "Tahsilat Kaydi" },
                { "Method", "Yontem" },
                { "Reference", "Referans" },
                { "IsShipmentPayment", "Fise Bagli Tahsilat" },
                { "SettingsTitle", "Ayarlar" },
                { "DatabaseSettings", "Veritabani Ayarlari" },
                { "Backup", "Yedek Al" },
                { "Restore", "Yedekten Don" },
                { "OpenDataFolder", "Veri Klasorunu Ac" },
                { "Language", "Dil / Language" },
                { "Unit_Piece", "Adet" },
                { "Unit_Kg", "Kg" },
                { "Unit_Tray", "Tava" },
                { "BranchType_Branch", "Sube" },
                { "BranchType_Market", "Market" },
                { "BranchType_Grocery", "Bakkal" },
                { "PaymentMethod_Cash", "Nakit" },
                { "PaymentMethod_CreditCard", "Kredi Karti" },
                { "PaymentMethod_BankTransfer", "Havale / EFT" },
                { "PaymentMethod_Other", "Diger" },
            };

            _dictionaries["en"] = new Dictionary<string, string>
            {
                { "ConfirmDelete", "Are you sure you want to delete this record?" },
                { "ConfirmDeleteShipment", "Are you sure you want to delete this shipment?" },
                { "ConfirmDeletePayment", "Are you sure you want to delete this payment?" },
                { "ConfirmDeleteRow", "Are you sure you want to delete this row?" },
                { "Confirm", "Confirm" },
                { "Yes", "Yes" },
                { "No", "No" },
                { "Save", "Save" },
                { "Delete", "Delete" },
                { "Cancel", "Cancel" },
                { "New", "New" },
                { "Refresh", "Refresh" },
                { "Search", "Search..." },
                { "Active", "Active" },
                { "Date", "Date" },
                { "Note", "Note" },
                { "Total", "Total" },
                { "Amount", "Amount" },
                { "ProductsTitle", "Products" },
                { "Product", "Product" },
                { "Category", "Category" },
                { "Unit", "Unit" },
                { "UnitPrice", "Unit Price" },
                { "PriceHistory", "Price History" },
                { "OldPrice", "Old Price" },
                { "NewPrice", "New Price" },
                { "BranchesTitle", "Branches / Accounts" },
                { "Branch", "Branch" },
                { "Type", "Type" },
                { "Address", "Address" },
                { "Contact", "Contact" },
                { "Phone", "Phone" },
                { "CreditLimit", "Credit Limit" },
                { "PaymentTerms", "Payment Terms (Days)" },
                { "PriceOverrides", "Price Overrides" },
                { "ViewStatement", "View Statement" },
                { "ShipmentsTitle", "Shipments" },
                { "ShipmentNo", "Shipment No" },
                { "Recalculate", "Recalculate" },
                { "Print", "Print" },
                { "Export", "Export" },
                { "Sent", "Sent" },
                { "Returned", "Return" },
                { "Wasted", "Waste" },
                { "Discount", "Discount" },
                { "GeneralTotal", "Grand Total" },
                { "SubTotal", "Subtotal" },
                { "TotalDiscount", "Total Discount" },
                { "AddToShipment", "Add Product" },
                { "PaymentsTitle", "Payments" },
                { "PaymentRecord", "Payment Record" },
                { "Method", "Method" },
                { "Reference", "Reference" },
                { "IsShipmentPayment", "Payment for Shipment" },
                { "SettingsTitle", "Settings" },
                { "DatabaseSettings", "Database Settings" },
                { "Backup", "Backup" },
                { "Restore", "Restore" },
                { "OpenDataFolder", "Open Data Folder" },
                { "Language", "Dil / Language" },
                { "Unit_Piece", "Piece" },
                { "Unit_Kg", "Kg" },
                { "Unit_Tray", "Tray" },
                { "BranchType_Branch", "Branch" },
                { "BranchType_Market", "Market" },
                { "BranchType_Grocery", "Grocery" },
                { "PaymentMethod_Cash", "Cash" },
                { "PaymentMethod_CreditCard", "Credit Card" },
                { "PaymentMethod_BankTransfer", "Bank Transfer" },
                { "PaymentMethod_Other", "Other" },
            };
        }

        public string GetString(string key)
        {
            if (_dictionaries.TryGetValue(CurrentCulture, out var dict) && dict.TryGetValue(key, out var value))
            {
                return value;
            }

            if (_currentCulture != "tr" &&
                _dictionaries.TryGetValue("tr", out var trDict) &&
                trDict.TryGetValue(key, out var trValue))
            {
                return trValue;
            }

            return $"[{key}]";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

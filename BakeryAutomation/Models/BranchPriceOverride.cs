using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Models
{
    [PrimaryKey(nameof(BranchId), nameof(ProductId))]
    public sealed class BranchPriceOverride
    {
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

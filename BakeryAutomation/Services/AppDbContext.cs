using Microsoft.EntityFrameworkCore;
using BakeryAutomation.Models;
using System;
using System.IO;

namespace BakeryAutomation.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ShipmentBatch> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }
        public DbSet<PriceChange> PriceChanges { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<DirectSale> DirectSales { get; set; }
        public DbSet<DirectSaleItem> DirectSaleItems { get; set; }
        public DbSet<BranchPriceOverride> BranchPriceOverrides { get; set; }
        public DbSet<ReturnReceipt> ReturnReceipts { get; set; }
        public DbSet<ReturnReceiptItem> ReturnReceiptItems { get; set; }

        public string DbPath { get; }

        public AppDbContext(string? dbPath = null)
        {
            DbPath = ResolveDatabasePath(dbPath);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite($"Data Source={DbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasMany(p => p.PriceHistory)
                .WithOne()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipmentBatch>()
                .HasMany(s => s.Items)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipmentBatch>()
                .HasIndex(s => new { s.BranchId, s.Date });

            modelBuilder.Entity<ShipmentBatch>()
                .HasIndex(s => s.BatchNo)
                .HasDatabaseName("UX_Shipments_BatchNo")
                .IsUnique();

            modelBuilder.Entity<DirectSale>()
                .HasMany(s => s.Items)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReturnReceipt>()
                .HasMany(r => r.Items)
                .WithOne()
                .HasForeignKey(i => i.ReturnReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReturnReceipt>()
                .HasIndex(r => new { r.BranchId, r.Date });

            modelBuilder.Entity<ReturnReceipt>()
                .HasIndex(r => r.ReturnNo)
                .HasDatabaseName("UX_ReturnReceipts_ReturnNo")
                .IsUnique();

            modelBuilder.Entity<ReturnReceiptItem>()
                .HasIndex(i => i.SourceShipmentId);

            modelBuilder.Entity<ReturnReceiptItem>()
                .HasIndex(i => i.SourceShipmentItemId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.BranchId, p.Date });

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ShipmentId);
        }

        private static string ResolveDatabasePath(string? dbPath)
        {
            var resolvedPath = string.IsNullOrWhiteSpace(dbPath)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BakeryAutomation",
                    "bakery.db")
                : Path.GetFullPath(dbPath);

            var folder = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return resolvedPath;
        }
    }
}

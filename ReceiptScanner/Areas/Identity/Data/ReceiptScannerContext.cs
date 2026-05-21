using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReceiptScanner.Areas.Identity.Data;
using ReceiptScanner.Models;

namespace ReceiptScanner.Data;

public class ReceiptScannerContext : IdentityDbContext<ReceiptScannerUser>
{
    public ReceiptScannerContext(DbContextOptions<ReceiptScannerContext> options)
        : base(options)
    {
    }

    public DbSet<ReceiptModel> Receipts { get; set; }

    public DbSet<RItemModel> ReceiptItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ReceiptModel>()
            .HasKey(r => r.ReceiptId);

        builder.Entity<ReceiptModel>()
            .HasMany(r => r.Items)
            .WithOne(i => i.Receipt)
            .HasForeignKey(i => i.ReceiptId);

        builder.Entity<ReceiptModel>()
            .HasOne(r => r.User)
            .WithMany(u => u.Receipts)
            .HasForeignKey(r => r.UserId);

        builder.Entity<RItemModel>()
            .HasKey(i => i.ItemId);
    }
}
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
    public DbSet<CategoryModel> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RItemModel>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);


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

        builder.Entity<CategoryModel>().HasData(
            new CategoryModel { CategoryId = "1", Name = "Месо" },
            new CategoryModel { CategoryId = "2", Name = "Напитки" },
            new CategoryModel { CategoryId = "3", Name = "Плодове" },
            new CategoryModel { CategoryId = "4", Name = "Зеленчуци" },
            new CategoryModel { CategoryId = "5", Name = "Млечни" },
            new CategoryModel { CategoryId = "6", Name = "Хляб и тестени" },
            new CategoryModel { CategoryId = "7", Name = "Замразени храни" },
            new CategoryModel { CategoryId = "8", Name = "Сладки и десерти" },
            new CategoryModel { CategoryId = "9", Name = "Снаксове" },
            new CategoryModel { CategoryId = "10", Name = "Консерви" },
            new CategoryModel { CategoryId = "11", Name = "Подправки и сосове" },
            new CategoryModel { CategoryId = "12", Name = "Готови храни" },
            new CategoryModel { CategoryId = "13", Name = "Домашни потреби" },
            new CategoryModel { CategoryId = "14", Name = "Козметика и хигиена" },
            new CategoryModel { CategoryId = "15", Name = "Алкохол" },
            new CategoryModel { CategoryId = "16", Name = "Други" }
);
    }
}
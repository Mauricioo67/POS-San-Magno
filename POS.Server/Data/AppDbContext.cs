using Microsoft.EntityFrameworkCore;
using POS.Shared.Entities;

namespace POS.Server.Data
{
    public class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<CashClosing> CashClosing => Set<CashClosing>();
        public DbSet<Category> Category => Set<Category>();
        public DbSet<Product> Product => Set<Product>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Dispatch> Dispatches => Set<Dispatch>();
        public DbSet<DispatchDetail> DispatchDetails => Set<DispatchDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            });
            modelBuilder.Entity<SaleDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(d => d.Sale)
                    .WithMany(s => s.SaleDetails)
                    .HasForeignKey(d => d.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d =>d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CashClosing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InitialBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCash).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCard).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalTransfer).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAdjustments).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FinalBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FinalCash).HasColumnType("decimal(18,2)");
            });
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DocumentNumber).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.Address).HasMaxLength(250);
            });
            modelBuilder.Entity<Dispatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DispatchNumber);
                entity.Property(e => e.DispatchNumber).HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DispatchDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(d => d.Dispatch)
                    .WithMany(s => s.DispatchDetails)
                    .HasForeignKey(d => d.DispatchId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RealTimeTradingPlatform.Api.Models;

namespace RealTimeTradingPlatform.Api.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(
        DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trade> Trades =>
        Set<Trade>();

    public DbSet<Order> Orders =>
        Set<Order>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Trade)
            .WithOne(t => t.Order)
            .HasForeignKey<Trade>(t => t.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(o => o.Quantity)
            .HasPrecision(18, 8);

        modelBuilder.Entity<Order>()
            .Property(o => o.Price)
            .HasPrecision(18, 8);

        modelBuilder.Entity<Trade>()
            .Property(t => t.Quantity)
            .HasPrecision(18, 8);

        modelBuilder.Entity<Trade>()
            .Property(t => t.Price)
            .HasPrecision(18, 8);

        modelBuilder.Entity<Trade>()
            .Property(t => t.TotalValue)
            .HasPrecision(18, 8);
    }
}
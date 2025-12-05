using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DataAccess.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
    public AppDbContext(){}
    
    DbSet<Customer> Customers { get; set; }
    DbSet<Procedure> Procedures { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = new ConfigurationBuilder().
                SetBasePath(Directory.GetCurrentDirectory()).
                AddJsonFile("appSettings.json").Build();

            optionsBuilder.UseLazyLoadingProxies()
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Procedure>().ToTable("Procedures");

        modelBuilder.Entity<Procedure>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.Procedures)
            .HasForeignKey(p => p.CustomerId);
    }
}
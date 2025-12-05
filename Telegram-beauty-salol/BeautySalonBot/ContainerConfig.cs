using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BeautySalonBot;

public class ContainerConfig
{
    public static IContainer Configure()
    {
        var serviceProvider = new ServiceCollection();
        var builder = new ContainerBuilder();
        
        builder.Register(c =>
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true).Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString).Options;
            return new PooledDbContextFactory<AppDbContext>(options);
        })
        .As<IDbContextFactory<AppDbContext>>()
        .SingleInstance();
        
        return builder.Build();
    }
}
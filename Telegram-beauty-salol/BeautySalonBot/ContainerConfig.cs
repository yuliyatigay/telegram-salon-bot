using Autofac;
using Autofac.Extensions.DependencyInjection;
using BeautySalonBot.ClientBotHandler;
using DataAccess;
using Domain.DataAccessInterfaces;
using Domain.DataAccessInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

public class ContainerConfig
{
    public static IContainer Configure()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        var builder = new ContainerBuilder();

        builder.Populate(services);
        
        builder.RegisterInstance(
                new TelegramBotClient("8465915888:AAHFVd1WReQoY-JqANX4zYovp8idN-gRj8A"))
            .As<TelegramBotClient>()
            .SingleInstance();
        
        builder.Register(c =>
                new ProcedureRepository(
                    configuration.GetConnectionString("DefaultConnection")!
                ))
            .As<IProcedureRepository>()
            .SingleInstance();

        builder.RegisterType<ClientStartHandler>()
            .As<IUpdateHandler>()
            .SingleInstance();

        builder.RegisterType<ClientBot>()
            .SingleInstance();

        return builder.Build();
    }
}
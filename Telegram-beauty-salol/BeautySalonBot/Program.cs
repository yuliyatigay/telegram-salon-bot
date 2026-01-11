using System.ComponentModel;
using Autofac;

namespace BeautySalonBot;
internal class Program
{
    public static void Main(string[] args)
    {
        var containerConfig = ContainerConfig.Configure();
        var bot = containerConfig.Resolve<ClientBot>();
        bot.Start();
        Thread.Sleep(Timeout.Infinite);
    }
}
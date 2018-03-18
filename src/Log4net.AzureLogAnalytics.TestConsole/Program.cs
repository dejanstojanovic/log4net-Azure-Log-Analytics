using log4net;
using System;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = @"log4net.config", Watch = true)]
namespace Log4net.AzureLogAnalytics.TestConsole
{
    class Program
    {
        private static ILog log = LogManager.GetLogger("TestLogger");

        static void Main(string[] args)
        {
            log.Info("Helo from log");
            Console.ReadKey();

        }
    }
}

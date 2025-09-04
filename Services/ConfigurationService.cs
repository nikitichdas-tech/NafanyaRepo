using Microsoft.Extensions.Configuration;
using Nafanya.Models;

namespace Nafanya.Services
{
    public class ConfigurationService
    {
        public static BotConfiguration LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var botConfig = configuration.GetSection("BotConfiguration");

            return new BotConfiguration
            {
                BotToken = Environment.GetEnvironmentVariable("BotToken")
                          ?? botConfig["BotToken"],
                SqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
                                     ?? botConfig["SqlConnectionString"],
                SourceChannelUsername = botConfig["SourceChannelUsername"],
                DestinationChannelUsername = botConfig["DestinationChannelUsername"],
                TargetMessage = botConfig["TargetMessage"],
                AllowedIps = botConfig["AllowedIps"]?.Split(',')
            };
        }
    }
}
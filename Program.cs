using Nafanya.Services;
using Nafanya.Handlers;
using Nafanya.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Nafanya
{
    class Program
    {
        static async Task Main()
        {
            // Загрузка конфигурации
            var config = ConfigurationService.LoadConfiguration();

            if (string.IsNullOrEmpty(config.BotToken))
            {
                Console.WriteLine("Ошибка!: BotToken не выявлен!");
                return;
            }
            Console.WriteLine($"BotToken: {!string.IsNullOrEmpty(config.BotToken)}");
            Console.WriteLine($"Канал, откуда брать информацию: {config.SourceChannelUsername}");
            Console.WriteLine($"Канал, куда информация будет транслироваться: {config.DestinationChannelUsername}");

            if (string.IsNullOrEmpty(config.SqlConnectionString) ||
                config.SqlConnectionString.Contains("your_connection_string"))
            {
                Console.WriteLine("Ошибка! Соединение с сервером SQL не настроено должным образом!");
                Console.WriteLine("💡 Run: setx SqlConnectionString \"Server=.\\SQLEXPRESS;...\"");
                return;
            }

            var botClient = new TelegramBotClient(config.BotToken);

            // Инициализация сервисов
            var databaseService = new DatabaseService(config.SqlConnectionString, config.AllowedIps);
            var telegramService = new TelegramService(botClient, config, databaseService);
            var permissionsService = new BotPermissionsService(botClient, config.DestinationChannelUsername);
            var updateHandler = new UpdateHandler(telegramService, databaseService, permissionsService, config);

            try
            {
                var me = await botClient.GetMe();
                Console.WriteLine($"Запущен бот: @{me.Username}");

                // Проверки прав бота
                await permissionsService.CheckBotPermissions();

                // Проверка доступа к исходному каналу
                if (!string.IsNullOrEmpty(config.SourceChannelUsername))
                {
                    await permissionsService.CheckSourceChannelAccess(config.SourceChannelUsername);
                }

                // Проверка подключения к базе данных
                if (!string.IsNullOrEmpty(config.SqlConnectionString))
                {
                    await DatabaseService.TestLocalConnection();
                }

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = [UpdateType.ChannelPost, UpdateType.Message]
                };

                botClient.StartReceiving(
                    updateHandler.HandleUpdateAsync,
                    UpdateHandler.HandleErrorAsync,
                    receiverOptions,
                    CancellationToken.None
                );

                Console.WriteLine($"Мониторинг обновлений...");
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска бота: {ex.Message}");
            }
        }
    }
}
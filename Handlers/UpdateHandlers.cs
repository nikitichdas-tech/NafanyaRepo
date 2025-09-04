using Telegram.Bot;
using Telegram.Bot.Types;
using Nafanya.Services;
using Nafanya.Models;

namespace Nafanya.Handlers
{
    public class UpdateHandler(
        TelegramService telegramService,
        DatabaseService databaseService,
        BotPermissionsService permissionsService,
        BotConfiguration config)
    {
        private readonly TelegramService _telegramService = telegramService;
        private readonly DatabaseService _databaseService = databaseService;
        private readonly BotPermissionsService _permissionsService = permissionsService;
        private readonly BotConfiguration _config = config;

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"\nИсточник получения обновления: {update.Type}");
                Console.WriteLine($"ID обновления: {update.Id}");

                if (update.Message != null)
                {
                    Console.WriteLine($"Сообщение от: {update.Message.From?.Username}");
                    Console.WriteLine($"Текст сообщения: {update.Message.Text}");
                    Console.WriteLine($"Тип чата: {update.Message.Chat.Type}");
                }

                if (update.ChannelPost != null)
                {
                    Console.WriteLine($"Отправлено из канала: {update.ChannelPost.Chat.Title}");
                    Console.WriteLine($"Username канала: @{update.ChannelPost.Chat.Username}");
                }

                if (update.ChannelPost != null)
                {
                    var channelPost = update.ChannelPost;

                    Console.WriteLine($"Мониторинг: @{_config.SourceChannelUsername}");
                    Console.WriteLine($"Кодовая фраза: {_config.TargetMessage}");
                    Console.WriteLine($"Отправляю в канал: @{_config.DestinationChannelUsername}");

                    if (!string.IsNullOrEmpty(_config.SourceChannelUsername))
                    {
                        Console.WriteLine($"Ожидаемый Username: @{_config.SourceChannelUsername}");
                        bool usernameMatches = string.Equals(channelPost.Chat.Username, _config.SourceChannelUsername, StringComparison.OrdinalIgnoreCase);
                        Console.WriteLine($"Username совпадает: {usernameMatches}");
                    }

                    bool isCorrectChannel = !string.IsNullOrEmpty(_config.SourceChannelUsername) &&
                                          string.Equals(channelPost.Chat.Username, _config.SourceChannelUsername, StringComparison.OrdinalIgnoreCase);

                    Console.WriteLine($"Правильный канал: {isCorrectChannel}");

                    if (isCorrectChannel)
                    {
                        bool containsTarget = (!string.IsNullOrEmpty(channelPost.Text) &&
                                             channelPost.Text.Contains(_config.TargetMessage!, StringComparison.OrdinalIgnoreCase)) ||
                                             (!string.IsNullOrEmpty(channelPost.Caption) &&
                                             channelPost.Caption.Contains(_config.TargetMessage!, StringComparison.OrdinalIgnoreCase));

                        Console.WriteLine($"Кодовая фраза: '{_config.TargetMessage}'");
                        Console.WriteLine($"Содержит кодовую фразу: {containsTarget}");

                        if (containsTarget)
                        {
                            Console.WriteLine($"Пересылка сообщения...");
                            await _telegramService.ForwardMessageToChannel(channelPost);
                            Console.WriteLine("Сообщение успешно отправлено");
                        }
                        else
                        {
                            Console.WriteLine($"Сообщение не содержит кодовую фразу");
                            if (!string.IsNullOrEmpty(channelPost.Text))
                            {
                                var similarPhrases = new[] { "allautoparts.ru" };
                                foreach (var phrase in similarPhrases)
                                {
                                    if (channelPost.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"Найдена схожая кодовая фраза: '{phrase}'");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Не из канала для трансляции сообщений");
                    }

                    Console.WriteLine($"==================\n");
                }

                if (update.Message != null && update.Message.Text != null)
                {
                    Console.WriteLine($"Обработка сообщения от пользователя: {update.Message.Text}");
                    await HandleUserMessage(update.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки метода HandleUpdateAsync: {ex.Message}");
            }
        }

        public async Task HandleUserMessage(Message message)
        {
            if (message.Text == null) return;

            Console.WriteLine($"Соощение от пользователя: {message.Text}");
            // Проверяем, является ли это ответом на запрос PartnerID
            if (message.ReplyToMessage != null &&
                message.ReplyToMessage.Text != null &&
                message.ReplyToMessage.Text.Contains("PartnerID"))
            {
                Console.WriteLine($"Получен входящий PartnerID: {message.Text}");
                await _telegramService.SendPartnerStats(message.Chat.Id, message.Text);
                return;
            }


            // Обработка обычных команд
            switch (message.Text)
            {
                case "/start":
                    Console.WriteLine("Получена команда /start");
                    await _telegramService.SendWelcomeMessage(message.Chat.Id, isStartCommand: true);
                    break;

                case "/help":
                    Console.WriteLine("Получена команда /help");
                    await _telegramService.SendWelcomeMessage(message.Chat.Id, isStartCommand: false);
                    break;

                case "/channel":
                    Console.WriteLine("Получена команда /channel");
                    await _telegramService.ShowChannelInfo(message.Chat.Id);
                    break;

                case "/buttons":
                    Console.WriteLine("Получена команда /buttons");
                    await _telegramService.ShowButtonsOnly(message.Chat.Id);
                    break;

                case "Партнёры - запросы 📊":
                    Console.WriteLine("Нажата кнопка Партнёры - запросы");
                    await _telegramService.SendPartnerRequestsInfo(message.Chat.Id);
                    break;

                case "Партнёры - заказы 🛒":
                    Console.WriteLine("Нажата кнопка Партнёры - заказы");
                    await _telegramService.SendPartnerOrdersInfo(message.Chat.Id);
                    break;

                case "Статистика по PartnerID 🔍":
                    Console.WriteLine("Нажата кнопка Статистика по PartnerID");
                    await _telegramService.AskForPartnerId(message.Chat.Id);
                    break;

                case "Канал 📢":
                    Console.WriteLine("Нажата кнопка Канал");
                    await _telegramService.ShowChannelInfo(message.Chat.Id);
                    break;

                default:
                    Console.WriteLine($"Неизвестная команда: {message.Text}");
                    await _telegramService.SendTextMessage(
                        chatId: message.Chat.Id,
                        text: "Неизвестная команда. Используйте /help для списка команд."
                    );
                    break;
            }
            
            {
                Console.WriteLine($"Обработка входящего PartnerID: {message.Text}");
                await _telegramService.SendPartnerStats(message.Chat.Id, message.Text);
                return;
            }
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
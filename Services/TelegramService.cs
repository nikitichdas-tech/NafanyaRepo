using Nafanya.Models;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Nafanya.Services
{
    public class TelegramService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly BotConfiguration _config;
        private readonly DatabaseService _databaseService;

        public TelegramService(ITelegramBotClient botClient, BotConfiguration config, DatabaseService databaseService)
        {
            _botClient = botClient;
            _config = config;
            _databaseService = databaseService;
        }

        // 1. 📊 ТОП партнеров по запросам
        public async Task<string> GetTopPartnersFromSql()
        {
            try
            {
                return await _databaseService.GetTopPartnersFromSql();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка получения данных по топу партнёров: {ex.Message}");
                return "❌ Ошибка при получении данных по топу партнёров";
            }
        }

        // 2. 🛒 ТОП партнеров по заказам  
        public async Task<string> GetTopPartnersOrdersFromSql()
        {
            try
            {
                return await _databaseService.GetTopPartnersOrdersFromSql();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка получения данных о заказах: {ex.Message}");
                return "❌ Ошибка при получении данных о заказах";
            }
        }

        // 3. 🔍 Статистика по конкретному PartnerID
        public async Task<string> GetPartnerStatisticsFromSql(string partnerId)
        {
            try
            {
                return await _databaseService.GetPartnerStatisticsFromSql(partnerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении статистики для PartnerID : {ex.Message}");
                return $"❌ Ошибка при получении статистики для PartnerID {partnerId}";
            }
        }

        // 📨 Методы отправки сообщений
        public async Task SendWelcomeMessage(long chatId, bool isStartCommand = false)
        {
            var welcomeText = @"🤖 Что умеет этот бот?

1️⃣ Транслировать информацию о блокировках из канала ABCP Alert
2️⃣ При нажатии на кнопку 'Партнёры - запросы' выводить информацию о 'ТОП партнеров по количеству запросов в секунду'
3️⃣ При нажатии на кнопку 'Партнёры - заказы' выводить информацию о 'Количестве заказов через ABCP'
4️⃣ Показывать статистику по конкретному PartnerID

📢 Канал с уведомлениями:
https://t.me/Check_for_block_abcp

💡 Команды:
/start - запуск бота
/help - показать это сообщение
/channel - информация о канале с уведомлениями
/buttons - показать кнопки управления";

            if (isStartCommand)
            {
                welcomeText = "✅ Бот успешно запущен!\n\n" + welcomeText;
            }

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Партнёры - запросы 📊") },
                new[] { new KeyboardButton("Партнёры - заказы 🛒") },
                new[] { new KeyboardButton("Статистика по PartnerID 🔍") },
                new[] { new KeyboardButton("Канал 📢") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeText,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None
            );
        }

        public async Task ShowChannelInfo(long chatId)
        {
            var channelInfo = @"📢 Канал с транслируемыми сообщениями:

🔔 Autostels Alert Monitor(ABCP)
Сообщения о блокировках поставщика Autostels автоматически транслируются в этот канал.

📎 Ссылка: https://t.me/Check_for_block_abcp

💡 Подпишитесь, чтобы быть в курсе всех блокировок!";

            await _botClient.SendMessage(
                chatId: chatId,
                text: channelInfo,
                cancellationToken: CancellationToken.None
            );
        }

        public async Task ShowButtonsOnly(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Партнёры - запросы 📊") },
                new[] { new KeyboardButton("Партнёры - заказы 🛒") },
                new[] { new KeyboardButton("Статистика по PartnerID 🔍") },
                new[] { new KeyboardButton("Канал 📢") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendMessage(
                chatId: chatId,
                text: "🎛️ Кнопки управления ботом:\n\nВыберите нужную опцию:",
                //parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None
            );
        }

        public async Task ForwardMessageToChannel(Message channelPost)
        {
            try
            {
                await _botClient.ForwardMessage(
                    chatId: $"@{_config.DestinationChannelUsername}",
                    fromChatId: channelPost.Chat.Id,
                    messageId: channelPost.MessageId,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка пересылки сообщения: {ex.Message}");
            }
        }

        public async Task SendPartnerRequestsInfo(long chatId)
        {
            try
            {
                await _botClient.SendChatAction(
                    chatId: chatId,
                    action: ChatAction.Typing,
                    cancellationToken: CancellationToken.None
                );

                var partnerData = await GetTopPartnersFromSql();

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: partnerData,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных о запросах: {ex.Message}");
                await SendTextMessage(chatId, "❌ Ошибка при получении данных о запросах");
            }
        }

        public async Task SendPartnerOrdersInfo(long chatId)
        {
            try
            {
                await _botClient.SendChatAction(
                    chatId: chatId,
                    action: ChatAction.Typing,
                    cancellationToken: CancellationToken.None
                );

                var ordersData = await GetTopPartnersOrdersFromSql();

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: ordersData,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных о заказах: {ex.Message}");
                await SendTextMessage(chatId, "❌ Ошибка при получении данных о заказах");
            }
        }

        public async Task AskForPartnerId(long chatId)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Введите PartnerID для получения статистики:",
                    replyMarkup: new ForceReplyMarkup
                    {
                        InputFieldPlaceholder = "Например: 12345",
                        Selective = true
                    },
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запроса по PartnerID: {ex.Message}");
            }
        }

        public async Task SendPartnerStats(long chatId, string partnerId)
        {
            // Защита от текста кнопок
            if (partnerId.Contains("Статистика по PartnerID") ||
                partnerId.Contains("🔍") ||
                partnerId.Contains("📊") ||
                partnerId.Contains("🛒") ||
                partnerId.Contains("📢"))
            {
                Console.WriteLine("🤖 Skipping button text processing");
                return;
            }
            if (string.IsNullOrWhiteSpace(partnerId) || !partnerId.All(char.IsDigit))
            {
                await SendTextMessage(chatId, "❌ Неверный формат PartnerID. Введите числовой идентификатор.");
                return;
            }

            try
            {
                await _botClient.SendChatAction(
                    chatId: chatId,
                    action: ChatAction.Typing,
                    cancellationToken: CancellationToken.None
                );

                var statsData = await GetPartnerStatisticsFromSql(partnerId);

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: statsData,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки статистики для PartnerID: {ex.Message}");
                await SendTextMessage(chatId, $"❌ Ошибка отправки статистики для PartnerID {partnerId}");
            }
        }

        public async Task SendTextMessage(long chatId, string text)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }
    }
}
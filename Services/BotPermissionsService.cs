using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Nafanya.Services
{
    public class BotPermissionsService(ITelegramBotClient botClient, string DestinationChannelUsername)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly string _destinationChannelName = DestinationChannelUsername;

        public async Task CheckBotPermissions()
        {
            try
            {
                Console.WriteLine("Проверка разрешений для бота...");

                var me = await _botClient.GetMe();
                Console.WriteLine($"Бот: @{me.Username} (ID: {me.Id})");

                // Проверяем права в целевом канале по username
                var chatMember = await _botClient.GetChatMember(
                    chatId: $"@{_destinationChannelName}",
                    userId: me.Id
                );

                Console.WriteLine($"Статус бота в канале для трансляции сообщений: {chatMember.Status}");

                if (chatMember.Status != ChatMemberStatus.Administrator &&
                    chatMember.Status != ChatMemberStatus.Creator)
                {
                    Console.WriteLine("Внимание!Бот не является администратором в канале для трансляции сообщений!");
                }
                else
                {
                    Console.WriteLine("Бот обладает всеми необходимыми разрешениями в канале для трансляции сообщений.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения разрешений: {ex.Message}");
            }
        }

        public async Task<bool> CanBotSendMessagesAsync()
        {
            try
            {
                var me = await _botClient.GetMe();
                var chatMember = await _botClient.GetChatMember(_destinationChannelName, me.Id);

                return chatMember.Status == ChatMemberStatus.Administrator ||
                       chatMember.Status == ChatMemberStatus.Creator;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CheckSourceChannelAccess(string sourceChannelUsername)
        {
            if (string.IsNullOrEmpty(sourceChannelUsername))
            {
                Console.WriteLine("❌ Source channel username is not set");
                return false;
            }

            try
            {
                var sourceChat = await _botClient.GetChat($"@{sourceChannelUsername}");
                Console.WriteLine($"Бот допущен в канал для трансляции сообщений: {sourceChat.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Бот не допущен в канал для трансляции сообщений @{sourceChannelUsername}: {ex.Message}");
                return false;
            }
        }
    }
}

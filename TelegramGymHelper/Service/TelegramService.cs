using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bots.Types;

namespace TelegramGymHelper
{
    public class TelegramService
    {
        private readonly IConfiguration _configuration;
        private readonly TelegramBotClient _botClient;

        public TelegramService(IConfiguration configuration)
        {
            _configuration = configuration;

            var token = _configuration["TelegramBot:Token"];
            _botClient = new TelegramBotClient(token);
        }

        public async Task StartAsync()
        {
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // всі типи оновлень
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"✅ Бот запущено: @{me.Username}");

            await Task.Delay(-1); // Бот працює безкінечно
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message || message.Text is null)
                return;

            Console.WriteLine($"📩 Повідомлення: {message.Text}");

            if (message.Text.ToLower().Contains("hello"))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Привіт! Я твій тренувальний бот!",
                    cancellationToken: cancellationToken
                );
            }
            // Тут буде обробка інших команд (наприклад, /start, /random, /favorite)
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"❌ Помилка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}

using Telegram.Bot;
using TelegramUI.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Requests;
using System.Net.Http;
using System.Text.Json;
namespace TelegramUI
{
    public class TelegramService
    {
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        public TelegramService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;

            var token = _configuration["TelegramBot:Token"];
            _botClient = new TelegramBotClient(token);

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["BotApi:BaseUrl"]);
        }

        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();
            
            await _botClient.DeleteWebhook();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            var handler = new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync);

            await _botClient.ReceiveAsync(
                updateHandler: handler,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null) 
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            
        }
        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message) 
        {
            if (message.Text == "/start") 
            {
                await botClient.SendMessage(message.Chat.Id, "Виберіть команду");
                return;
            }
            else if (message.Text.StartsWith("/random"))
            {
                var exercise = await GetRandomExerciseAsync();
                if (exercise != null)
                {
                    string response = $"Вправа: {exercise.Name}\nОпис: {exercise.Description}\n{exercise}";
                    await botClient.SendMessage(message.Chat.Id, response);
                }
                else
                {
                    await botClient.SendMessage(message.Chat.Id, "Не вдалося знайти вправу");
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiEx => $"Telegram API Error:\n[{apiEx.ErrorCode}]\n{apiEx.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine($"❌ Помилка: {errorMessage}");
            return Task.CompletedTask;
        }
        private async Task<ExerciseDto?> GetRandomExerciseAsync()
        {
            var response = await _httpClient.GetAsync("api/exercises/random");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExerciseDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }

    }
}

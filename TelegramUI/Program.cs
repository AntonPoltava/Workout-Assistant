using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using TelegramUI;

namespace TelegramUI
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Реєструємо HttpClient з базовою адресою (для запитів до API)
                    services.AddHttpClient<TelegramService>(client =>
                    {
                        client.BaseAddress = new Uri(configuration["BotApi:BaseUrl"]);
                    });

                    // Реєструємо TelegramBotClient
                    services.AddSingleton<ITelegramBotClient>(provider =>
                    {
                        var token = configuration["TelegramBot:Token"];
                        return new TelegramBotClient(token);
                    });

                    // Додаємо сам сервіс
                    services.AddSingleton<TelegramService>();
                })
                .Build();

            // Запускаємо TelegramService
            var telegramService = host.Services.GetRequiredService<TelegramService>();
            await telegramService.StartAsync();
        }
    }
}

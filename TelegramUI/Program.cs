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
           .ConfigureAppConfiguration(config =>
           {
               config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
           })
           .ConfigureServices((context, services) =>
           {
               var configuration = context.Configuration;
               var botToken = configuration["TelegramBot:Token"];

               services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

               services.AddHttpClient<TelegramService>(client =>
               {
                   client.BaseAddress = new Uri(configuration["BotApi:BaseUrl"]);
               });

               services.AddSingleton<TelegramService>();
           })
           .Build();

            var telegramService = host.Services.GetRequiredService<TelegramService>();
            await telegramService.StartAsync();
        }
    }
}

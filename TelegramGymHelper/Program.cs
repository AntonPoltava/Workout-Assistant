
namespace TelegramGymHelper
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<TelegramService>();
                });

            var app = builder.Build();

            
            await app.Services.GetRequiredService<TelegramService>().StartAsync();

            app.Run();
        }
    }
}

using BotAPI.Client;
using BotAPI.Data;
using BotAPI.Database;
using Microsoft.EntityFrameworkCore;

namespace BotAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Services.AddHttpClient<WgerClient>();
            //builder.Services.AddSingleton<TelegramBotService>();

            builder.Services.AddScoped<DBExercise>();
            builder.Services.AddDbContext<BotApiDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            Console.WriteLine("Connection string: " + builder.Configuration.GetConnectionString("DefaultConnection"));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            Console.WriteLine($"🌐 API is running at: {builder.Configuration["ASPNETCORE_URLS"] ?? "https://localhost:5165"}");

            app.Run();
            
           

           

      


        }
    }
}

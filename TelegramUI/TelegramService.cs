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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TelegramUI
{
    public class TelegramService
    {
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, UserExerciseSession> _userSessions = new();

        public TelegramService(IConfiguration configuration,ITelegramBotClient botClient, HttpClient httpClient)
        {
            _configuration = configuration;
            var token = _configuration["TelegramBot:Token"];
           
            _botClient = botClient;

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
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data != null)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
            }

        }
        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message) 
        {
            if (message.Text == "/start")
            {
                var commandsKeyboard = new ReplyKeyboardMarkup(new[]
                {
                     new[] { new KeyboardButton("/help") },
                    new[] { new KeyboardButton("/random") },
                     new[] { new KeyboardButton("/list") },
                     new[] { new KeyboardButton("/favorite") }
                })
               
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

                await botClient.SendMessage(
                    message.Chat.Id,
                    "👋 Привіт! Обери команду нижче:",
                    replyMarkup: commandsKeyboard
                );
                return;
            }
            else if (message.Text == "/help")
            {
               string helpMessage = """
                🆘 <b>Допомога по командам:</b>

                /start – Запускає бота і показує меню.
                /random – Щоб обирати категорію і отримати випадкову вправу.
                /list – Щоб обирати категорію і гортати вправи одну за одною.
                /favorite – Показує збережені (обрані) вправи.

                ⬅️ ➡️ – Кнопки для навігації вправ.
                💾 Зберегти – Додає вправу до обраного.
                🗑️ Видалити – Видаляє вправу з обраного.

                Обери команду з меню або введи її вручну.
                """;

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: helpMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                );
                return;
            }

            else if (message.Text == "/favorite")
            {
                await ShowFavoriteExercises(botClient, message.Chat.Id);
            }
            else if (message.Text.StartsWith("/random"))
            {
                var categories = new[] { "Abs", "Arms", "Back", "Calves", "Cardio", "Chest", "Legs", "Shoulders" };

                var keyboard = new ReplyKeyboardMarkup(
                    categories.Select(c => new KeyboardButton[] { new KeyboardButton(c) })
                )
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Вибери категорію:",
                    replyMarkup: keyboard
                );

                return;
            }
            else if (IsExerciseCategory(message.Text))
            {
                if (_userSessions.TryGetValue(message.Chat.Id, out var session) && session.Mode == "list")
                {
                    await StartExerciseListSession(botClient, message.Chat.Id, message.Text);
                }
                else
                {
                    await SendRandomExerciseByCategory(botClient, message.Chat.Id, message.Text);
                }
            }

            else if (message.Text.StartsWith("/list"))
            {
                var categories = new[] { "Abs", "Arms", "Back", "Calves", "Cardio", "Chest", "Legs", "Shoulders" };

                var keyboard = new ReplyKeyboardMarkup(
                    categories.Select(c => new KeyboardButton[] { new KeyboardButton(c) })
                )
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Вибери категорію для списку:",
                    replyMarkup: keyboard
                );

                _userSessions[message.Chat.Id] = new UserExerciseSession
                {
                    Exercises = null,
                    CurrentIndex = -1,
                    Mode = "list"
                };

                return;
            }

            else if (message.Text == "➡️" || message.Text == "⬅️")
            {
                await NavigateExercise(botClient, message.Chat.Id, message.Text);
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
        private async Task SendRandomExerciseByCategory(ITelegramBotClient botClient, long chatId, string category)
        {
            var response = await _httpClient.GetAsync($"/api/Exercises/GetByCategory/exercise/random?category={category}");

            if (!response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "⚠️ Не вдалося отримати вправу.");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var exercise = JsonSerializer.Deserialize<ExerciseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Console.WriteLine($"[DEBUG] MediaUrl = {exercise?.MediaUrl}");
            
            var message = $"💪 <b>{exercise.Name}</b>\n\n{exercise.Description}";
            var media = exercise.MediaUrl;

            if (!string.IsNullOrWhiteSpace(media) && media.EndsWith(".mp4"))
            {
                await botClient.SendVideo(chatId, media, caption: message, parseMode: ParseMode.Html);
            }
            else if (!string.IsNullOrWhiteSpace(media))
            {
                await botClient.SendPhoto(chatId, media, caption: message, parseMode: ParseMode.Html);
            }
            else
            {
                await botClient.SendMessage(chatId, message, parseMode: ParseMode.Html);
            }
        }
        private bool IsExerciseCategory(string text)
        {
            var known = new[] { "Abs", "Arms", "Back", "Calves", "Cardio", "Chest", "Legs", "Shoulders" };
            return known.Contains(text, StringComparer.OrdinalIgnoreCase);
        }
        private async Task StartExerciseListSession(ITelegramBotClient botClient, long chatId, string category)
        {
            var response = await _httpClient.GetAsync($"/api/Exercises/GetExercisesByCategoryName/by-category-name/{category}");

            if (!response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "⚠️ Не вдалося отримати список вправ.");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var exercises = JsonSerializer.Deserialize<List<ExerciseDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (exercises == null || exercises.Count == 0)
            {
                await botClient.SendMessage(chatId, "⚠️ У цій категорії немає вправ.");
                return;
            }

            _userSessions[chatId] = new UserExerciseSession
            {
                Exercises = exercises,
                CurrentIndex = 0,
                Mode = "list"
            };

            await ShowCurrentExercise(botClient, chatId);
        }
        private async Task ShowCurrentExercise(ITelegramBotClient botClient, long chatId)
        {
            if (!_userSessions.TryGetValue(chatId, out var session))
                return;

            var ex = session.Exercises[session.CurrentIndex];
            var message = $"💪 <b>{ex.Name}</b>\n\n{ex.Description}";

            InlineKeyboardMarkup nav = new(new[]
            {
                 new[]
                 {
                    new InlineKeyboardButton("⬅️") { CallbackData = "prev" },
                    new InlineKeyboardButton("➡️") { CallbackData = "next" }
                 },
                 new[]
                 {
                    new InlineKeyboardButton("💾 Зберегти") { CallbackData = "save" }
                 }
            });

            if (!string.IsNullOrWhiteSpace(ex.MediaUrl) && ex.MediaUrl.EndsWith(".mp4"))
            {
                await botClient.SendVideo(chatId, ex.MediaUrl, caption: message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
            else if (!string.IsNullOrWhiteSpace(ex.MediaUrl))
            {
                await botClient.SendPhoto(chatId, ex.MediaUrl, caption: message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
            else
            {
                await botClient.SendMessage(chatId, message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
        }
        private async Task NavigateExercise(ITelegramBotClient botClient, long chatId, string direction)
        {
            if (!_userSessions.TryGetValue(chatId, out var session))
                return;
            
            if (session.Exercises == null || session.Exercises.Count == 0)
                return;
            
            if (direction == "next")
                session.CurrentIndex = (session.CurrentIndex + 1) % session.Exercises.Count;
           
            else if (direction == "prev")
                session.CurrentIndex = (session.CurrentIndex - 1 + session.Exercises.Count) % session.Exercises.Count;
            await ShowCurrentExercise(botClient, chatId);
            
            Console.WriteLine($"[DEBUG] Current index: {session.CurrentIndex}");

            if (session.Mode == "list")
            {
                await ShowCurrentExercise(botClient, chatId);
            }
            else
            {
                await ShowCurrentFavorite(botClient, chatId);
            }

        }
        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var action = callbackQuery.Data;

            if (action == "next" || action == "prev")
            {
                await NavigateExercise(botClient, chatId, action);
            }
            else if (action == "save")
            {
                if (_userSessions.TryGetValue(chatId, out var session))
                {
                    var exercise = session.Exercises[session.CurrentIndex];

                    var savePayload = new
                    {
                        UserId = chatId,
                        ExerciseId = exercise.Id,
                        Name = exercise.Name,
                        Description = exercise.Description,
                        MediaUrl = exercise.MediaUrl,
                        Category = exercise.Category
                    };
                    var jsonBody = JsonSerializer.Serialize(savePayload);
                    Console.WriteLine($"[DEBUG] POST body: {jsonBody}");
                    var content = new StringContent(JsonSerializer.Serialize(savePayload), System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("/api/Exercises/AddToFavorites/favorite", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Збережено!");
                    }
                    else
                    {
                        await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ Не вдалося зберегти.");
                    }
                }
            }
            else if (action.StartsWith("delete_fav_"))
            {
                var idStr = action.Replace("delete_fav_", "");
                if (int.TryParse(idStr, out int exerciseId))
                {
                    var response = await _httpClient.DeleteAsync($"/api/Exercises/RemoveFromFavorites?userId={chatId}&exerciseId={exerciseId}");

                    if (response.IsSuccessStatusCode)
                    {
                        // Видаляємо з сесії та показуємо наступну
                        if (_userSessions.TryGetValue(chatId, out var session))
                        {
                            session.Exercises.RemoveAll(e => e.Id == exerciseId);

                            if (session.CurrentIndex >= session.Exercises.Count)
                                session.CurrentIndex = Math.Max(0, session.Exercises.Count - 1);

                            if (session.Exercises.Count == 0)
                            {
                                await botClient.SendMessage(chatId, "❌ Всі обрані вправи видалені.");
                                _userSessions.Remove(chatId);
                            }
                            else
                            {
                                await ShowCurrentFavorite(botClient, chatId);
                            }
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "⚠️ Не вдалося видалити вправу.");
                    }
                }
            }
        }
        private async Task ShowFavoriteExercises(ITelegramBotClient botClient, long chatId)
        {
            var response = await _httpClient.GetAsync($"/api/Exercises/GetFavorites/favorites/{chatId}");

            if (!response.IsSuccessStatusCode)
            {
                await botClient.SendMessage(chatId, "⚠️ Не вдалося отримати обрані вправи.");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var favorites = JsonSerializer.Deserialize<List<ExerciseDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (favorites == null || favorites.Count == 0)
            {
                await botClient.SendMessage(chatId, "📭 У тебе ще немає обраних вправ.");
                return;
            }

            _userSessions[chatId] = new UserExerciseSession
            {
                Exercises = favorites,
                CurrentIndex = 0
            };

            await ShowCurrentFavorite(botClient, chatId);
        }
        private async Task ShowCurrentFavorite(ITelegramBotClient botClient, long chatId)
        {
            if (!_userSessions.TryGetValue(chatId, out var session))
                return;

            var ex = session.Exercises[session.CurrentIndex];
            var message = $"💪 <b>{ex.Name}</b>\n\n{ex.Description}";

            InlineKeyboardMarkup nav = new(new[]
            {
                 new[]
                 {
            new InlineKeyboardButton("⬅️") { CallbackData = "prev" },
            new InlineKeyboardButton("➡️") { CallbackData = "next" }
                },
                new[]
             {
            new InlineKeyboardButton("🗑️ Видалити") { CallbackData = $"delete_fav_{ex.Id}" }
             }
            });

            if (!string.IsNullOrWhiteSpace(ex.MediaUrl) && ex.MediaUrl.EndsWith(".mp4"))
            {
                await botClient.SendVideo(chatId, ex.MediaUrl, caption: message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
            else if (!string.IsNullOrWhiteSpace(ex.MediaUrl))
            {
                await botClient.SendPhoto(chatId, ex.MediaUrl, caption: message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
            else
            {
                await botClient.SendMessage(chatId, message, parseMode: ParseMode.Html, replyMarkup: nav);
            }
        }


    }
}

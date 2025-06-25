using BotAPI.Client;
using BotAPI.Data;
using BotAPI.Database;
using BotAPI.Models;
using BotAPI.Migrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace BotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ExercisesController : ControllerBase
    {
        private readonly ILogger<ExercisesController> _logger;
        private readonly WgerClient _wgerClient;
        private readonly BotApiDbContext _context;

        public ExercisesController(WgerClient wgerClient, BotApiDbContext context, ILogger<ExercisesController> _logger)
        {
            this._logger = _logger;
            this._wgerClient = wgerClient;
            this._context = context;
        }
        

        [HttpGet("{id}")]
        [ActionName("GetById")]
        public async Task<IActionResult> GetExercise(int id)
        {
            try
            {
                var exercise = await _wgerClient.GetExerciseByIdAsync(id);

                if (exercise == null)
                    return NotFound("Exercise not found");

                var translation = exercise.Translations
                    ?.FirstOrDefault(t => t.Language == 2);

                var dto = new ExerciseDto
                {
                    Name = translation?.Name ?? exercise.Name ?? "No name",
                    Description = translation.Description ?? exercise.Description ?? "No description",
                    MediaUrl = exercise.Videos?.FirstOrDefault()?.Video
                            ?? exercise.Images?.FirstOrDefault()?.Image
                            ?? string.Empty
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }

        }
        [HttpGet("exercise/random")]
        [ActionName("GetByCategory")]
        public async Task<ActionResult<ExerciseDto>> GetRandomExerciseByCategory([FromQuery] string category)
        {
            var exercises = await _wgerClient.GetExercisesByCategoryAsync(category);

            if (exercises == null || exercises.Count == 0)
                return NotFound($"No exercises found for category: {category}");

            var filtered = exercises
                .Where(e =>
                    e.Translations?.Any(t =>
                        t.Language == 2 &&
                        IsEnglish(t.Name) &&
                        IsEnglish(t.Description)
                    ) == true &&
                    (
                        (e.Videos?.Any(v => !v.Video.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)) == true) ||
                        (e.Images?.Any() == true)
                    )
                )
                .ToList();

            if (!filtered.Any())
                return NoContent();

            var random = new Random();
            var selected = filtered[random.Next(filtered.Count)];

            var translation = selected.Translations.First(t =>
                t.Language == 2 &&
                IsEnglish(t.Name) &&
                IsEnglish(t.Description));

            var videoUrl = selected.Videos?.FirstOrDefault(v =>
                !v.Video.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))?.Video;

            var imageUrl = selected.Images?.FirstOrDefault()?.Image;

            var dto = new ExerciseDto
            {
                
                Name = translation.Name,
                Description = StripHtml(translation.Description),
                MediaUrl = videoUrl ?? imageUrl ?? string.Empty
            };

            return Ok(dto);
        }

        private string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
        private bool IsEnglish(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            
            return System.Text.RegularExpressions.Regex.IsMatch(
                text,
                @"^[a-zA-Z0-9\s\p{P}]*$"
            );
        }

        [HttpPost("favorite")]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteExerciseDto favoriteDto)
        {
            if (favoriteDto == null)
            {
                return BadRequest("Request body is null");
            }

            if (favoriteDto.UserId == 0 || favoriteDto.ExerciseId == 0)
            {
                return BadRequest("UserId or ExerciseId is missing or zero");
            }
            try
            {
                var exists = await _context.FavoriteExercises
                    .AnyAsync(f => f.UserId == favoriteDto.UserId && f.ExerciseId == favoriteDto.ExerciseId);

                if (exists)
                {
                    return Conflict("Exercise is already in favorites.");
                }


                var favorite = new FavoriteExercise
                {
                    UserId = favoriteDto.UserId,
                    ExerciseId = favoriteDto.ExerciseId,
                    Name = favoriteDto.Name,
                    Description = favoriteDto.Description,
                    MediaUrl = favoriteDto.MediaUrl,
                    Category = favoriteDto.Category
                };

                _context.FavoriteExercises.Add(favorite);
                await _context.SaveChangesAsync();

                return Ok("Exercise added to favorites.");
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Exception in AddToFavorites: {ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            
            }

        [HttpDelete("favorite")]
        public async Task<IActionResult> RemoveFromFavorites([FromQuery] long userId, [FromQuery] int exerciseId)
        {
            Console.WriteLine($"[DEBUG] userId = {userId}, exerciseId = {exerciseId}");
            var favorite = await _context.FavoriteExercises
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ExerciseId == exerciseId);

            if (favorite == null)
            {
                Console.WriteLine("[DEBUG] Favorite not found in DB.");
                return NotFound("Exercise not found in favorites.");
            }

            _context.FavoriteExercises.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok("Exercise removed from favorites.");
        }

        [HttpGet("favorites/{userId}")]
        public async Task<IActionResult> GetFavorites(long userId)
        {
            var favorites = await _context.FavoriteExercises
                .Where(f => f.UserId == userId)
                .ToListAsync();

            if (favorites == null || favorites.Count == 0)
            {
                return NotFound("No favorite exercises found for this user.");
            }

            return Ok(favorites);
        }
        [HttpGet("by-category-name/{categoryName}")]
        public async Task<IActionResult> GetExercisesByCategoryName(string categoryName)
        {
            var allCategories = await _wgerClient.GetAllCategoriesAsync();
            var targetCategory = allCategories.FirstOrDefault(c =>
                string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (targetCategory == null)
                return NotFound($"Category \"{categoryName}\" not found.");

            var exercises = await _wgerClient.GetExercisesByCategoryAsync(categoryName);

            var filtered = exercises
                .Where(e =>
                    e.Translations?.Any(t =>
                        t.Language == 2 &&
                        IsEnglish(t.Name) &&
                        IsEnglish(t.Description)
                    ) == true &&
                    (
                        (e.Videos?.Any(v => !v.Video.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)) == true) ||
                        (e.Images?.Any() == true)
                    )
                )
                .Select(e =>
                {
                    var translation = e.Translations.First(t =>
                        t.Language == 2 &&
                        IsEnglish(t.Name) &&
                        IsEnglish(t.Description));

                    var videoUrl = e.Videos?.FirstOrDefault(v =>
                        !v.Video.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))?.Video;

                    var imageUrl = e.Images?.FirstOrDefault()?.Image;

                    return new ExerciseDto
                    {
                        Id = e.Id,
                        Name = translation.Name,
                        Description = StripHtml(translation.Description),
                        Category = targetCategory.Id,
                        MediaUrl = videoUrl ?? imageUrl ?? string.Empty
                    };
                })
                .ToList();

            if (filtered.Count == 0)
                return NoContent();

            return Ok(filtered);
        }
    }
}

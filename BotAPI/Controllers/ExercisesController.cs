using BotAPI.Client;
using BotAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace BotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ExercisesController : ControllerBase
    {
        private readonly ILogger<ExercisesController> _logger;
        private readonly WgerClient _wgerClient;

        public ExercisesController(WgerClient wgerClient, ILogger<ExercisesController> _logger)
        {
            this._logger = _logger;
            this._wgerClient = wgerClient;
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
            try
            {
                var exercises = await _wgerClient.GetExercisesByCategoryAsync(category);


                if (exercises == null || exercises.Count == 0)
                    return NotFound($"Not found exercise for: {category}");

                var random = new Random();
                var randomExercise = exercises[random.Next(exercises.Count)];
                var translation = randomExercise.Translations
                    ?.FirstOrDefault(t => t.Language == 2);

                var dto = new ExerciseDto
                {
                    Name = translation?.Name ?? randomExercise.Name ?? "No name",
                    Description = translation.Description ?? randomExercise.Description ?? "No description",
                    MediaUrl = randomExercise.Videos?.FirstOrDefault()?.Video
                            ?? randomExercise.Images?.FirstOrDefault()?.Image
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

    }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BotAPI.Models
{
   
    public class FavoriteExerciseDto
    {
        public long UserId { get; set; }
        public int ExerciseId { get; set; }
    }
    public class FavoriteExercise
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public int ExerciseId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class ExerciseInfoResponse<T>
    {
        public int Count { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
        public List<T> Results { get; set; } = new();
    }
    public class Translation
    {
        public int Id { get; set; }
        public int Language { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    public class ExerciseWgerResponse
    {
        public int Id { get; set; }
        public string? Uuid { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Category? Category { get; set; }
        public List<Muscle>? Muscles { get; set; }
        public List<ResponseImage>? Images { get; set; }
        public List<ResponseVideo>? Videos { get; set; }
        public List<Translation>? Translations { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class Muscle
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ResponseImage
    {
        public int Id { get; set; }
        public string? Image { get; set; }
        public string? Status { get; set; }
        public int? Exercise { get; set; }
        public bool? IsMain { get; set; }
    }

    public class ResponseVideo
    {
        public int Id { get; set; }
        public string? Video { get; set; }
        public string? Status { get; set; }
        public int? Exercise { get; set; }
    }
    public class ExerciseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Category { get; set; }
        public string MediaUrl { get; set; }
    }
}


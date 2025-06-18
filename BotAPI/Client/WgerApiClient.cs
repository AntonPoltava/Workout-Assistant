using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using BotAPI.Models;
using System.Collections.Generic;

namespace BotAPI.Client
{
    public class WgerClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public WgerClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["WgerApi:BaseUrl"];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ExerciseWgerResponse?> GetExerciseByIdAsync(int id)
        {
            var url = $"{_apiBaseUrl}/exerciseinfo/{id}/?format=json";
            var response = await _httpClient.GetAsync(url);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ExerciseWgerResponse>(json, _jsonOptions);
        }

        public async Task<List<ExerciseWgerResponse>> GetExercisesByCategoryAsync(string categoryName)
        {
            var url = $"{_apiBaseUrl}/exerciseinfo/?limit=1000&format=json";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ExerciseWgerResponse>();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<ExerciseInfoResponse<ExerciseWgerResponse>>(json, _jsonOptions);

            if (data == null || data.Results == null)
                return new List<ExerciseWgerResponse>();

            return data.Results
                .Where(e => e.Category != null &&
                            string.Equals(e.Category.Name, categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<List<ExerciseDto>> GetAllExercisesAsync()
        {
            var dtos = new List<ExerciseDto>();
            var mediaMap = new Dictionary<int, string>();

            // Отримуємо медіа
            int mediaPage = 1;
            while (true)
            {
                var mediaResp = await _httpClient.GetAsync($"{_apiBaseUrl}/exerciseimage/?page={mediaPage}");
                mediaResp.EnsureSuccessStatusCode();
                var mediaContent = await mediaResp.Content.ReadAsStringAsync();
                var mediaData = JsonSerializer.Deserialize<ExerciseInfoResponse<ResponseImage>>(mediaContent, _jsonOptions);
                if (mediaData?.Results == null || mediaData.Results.Count == 0)
                    break;

                foreach (var me in mediaData.Results)
                {
                    if (me.Exercise.HasValue && !mediaMap.ContainsKey(me.Exercise.Value))
                    {
                        mediaMap[me.Exercise.Value] = me.Image ?? "";
                    }
                }


                if (string.IsNullOrEmpty(mediaData.Next))
                    break;
                mediaPage++;
            }

            // Отримуємо вправи
            int page = 1;
            while (true)
            {
                var resp = await _httpClient.GetAsync($"{_apiBaseUrl}/exercise/?language=2&page={page}");
                resp.EnsureSuccessStatusCode();
                var cont = await resp.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ExerciseInfoResponse<ExerciseWgerResponse>>(cont, _jsonOptions);
                if (data?.Results == null || data.Results.Count == 0)
                    break;

                foreach (var ex in data.Results)
                {
                    dtos.Add(new ExerciseDto
                    {
                        Name = ex.Name,
                        Description = ex.Description,
                        MediaUrl = mediaMap.TryGetValue(ex.Id, out var url) ? url : ""
                    });
                }

                if (string.IsNullOrEmpty(data.Next))
                    break;
                page++;
            }

            return dtos;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var result = new List<Category>();
            int page = 1;

            while (true)
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/exercisecategory/?language=2&page={page}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ExerciseInfoResponse<Category>>(content, _jsonOptions);

                if (data?.Results == null || data.Results.Count == 0)
                    break;

                result.AddRange(data.Results);

                if (string.IsNullOrEmpty(data.Next))
                    break;

                page++;
            }

            return result;
        }
    }
}

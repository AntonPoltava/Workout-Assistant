using Microsoft.VisualBasic;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using BotAPI.Models;

namespace BotAPI.Client
{
    public class WgerClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

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
            return JsonSerializer.Deserialize<ExerciseWgerResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        public async Task<List<ExerciseWgerResponse>> GetExercisesByCategoryAsync(string categoryName)
        {
            var url = $"{_apiBaseUrl}/exerciseinfo/?limit=1000&format=json";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ExerciseWgerResponse>();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<ExerciseInfoResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null || data.Results == null)
                return new List<ExerciseWgerResponse>();

            return data.Results
                .Where(e => e.Category != null &&
                            string.Equals(e.Category.Name, categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }


}

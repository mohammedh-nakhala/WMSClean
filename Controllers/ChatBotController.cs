using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace WMSClean.Controllers
{
    [Authorize]
    public class ChatBotController : Controller
    {
        private readonly HttpClient _httpClient;

        public ChatBotController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { answer = "Please enter a valid question." });
            }

            try
            {
                var request = new
                {
                    model = "tinyllama", 
                    prompt = $"You are a warehouse management assistant. Answer briefly and helpfully: {question}",
                    stream = false
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { answer = "AI service is not responding. Make sure Ollama is running (ollama serve)." });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaResponse>(jsonResponse);

                return Json(new { answer = result?.response ?? "Sorry, I could not process that." });
            }
            catch (HttpRequestException)
            {
                return Json(new { answer = "Cannot connect to Ollama. Please run 'ollama serve' in a separate command prompt window." });
            }
            catch (Exception ex)
            {
                return Json(new { answer = $"Error: {ex.Message}" });
            }
        }

        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
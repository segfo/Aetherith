using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class SseClient
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task StartSseAsync(string message,string endpoint,string apiKey,string conversationId, Action<string> onMessage)
    {
        var payload = new
        {
            inputs = new { },
            query = message,
            response_mode = "streaming",
            conversation_id = conversationId,
            user = "Aetherith"
        };
        string jsonString = JsonConvert.SerializeObject(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Headers.Add("Accept", "text/event-stream");
        request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        try
        {
            var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead
            );

            if (!response.IsSuccessStatusCode)
            {
                UnityEngine.Debug.LogError($"HTTP Error: {response.StatusCode}");
                return;
            }

            if (response.Content == null)
            {
                UnityEngine.Debug.LogError("SSE Error: response.Content is null");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
                {
                    string data = line.Substring(6).Trim();
                    onMessage?.Invoke(data);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"SSE Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }
}

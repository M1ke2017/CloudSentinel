using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Json;

namespace CloudSentinel.Core.Services
{
    public class WebhookService(HttpClient httpClient)
    {
        public async Task<bool> SendAsync(string webhookUrl, string message)
        {
            try
            {
                using var reponse = await httpClient.PostAsJsonAsync(webhookUrl, new { text = message });
                return reponse.IsSuccessStatusCode;
            }
            catch (HttpRequestException) { return false; }
            catch (TaskCanceledException) { return false; }
        }
    }
}

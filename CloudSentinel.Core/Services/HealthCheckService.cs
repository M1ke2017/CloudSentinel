using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Core.Services
{
    public class HealthCheckService(HttpClient httpClient)
    {
        public async Task<bool> CheckAsync(string url)
        {
            try
            {
                using var response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            { return false; }
            catch (TaskCanceledException)
            { return false; }
        }
    }
}

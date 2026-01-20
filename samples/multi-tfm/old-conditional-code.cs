namespace ConditionalDemo;

/// <summary>
/// Example class demonstrating TFM-specific conditional compilation.
/// This is the "old" version before changes were made.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Method that exists in all TFMs - no changes
    public async Task<string> GetDataAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

#if NET8_0_OR_GREATER
    // This method only exists in .NET 8.0 and later
    // Will be modified in the new version
    public async Task<T> GetJsonAsync<T>(string endpoint)
    {
        var json = await GetDataAsync(endpoint);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
    }
#endif

#if NET10_0_OR_GREATER
    // This method only exists in .NET 10.0 and later
    // Uses newer APIs available in NET10
    public async Task<Stream> GetStreamAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }
#endif

    // Legacy method that exists in older frameworks
#if !NET8_0_OR_GREATER
    public string GetDataSync(string endpoint)
    {
        // Synchronous version for older frameworks
        var response = _httpClient.GetAsync(endpoint).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
#endif
}

/// <summary>
/// Configuration class with TFM-specific settings.
/// </summary>
public class AppConfiguration
{
    public string ApiBaseUrl { get; set; } = "https://api.example.com";

    public int TimeoutSeconds { get; set; } = 30;

#if NET8_0_OR_GREATER
    // Modern configuration using newer .NET features
    public required string ApiKey { get; set; }
#else
    // Fallback for older frameworks without required properties
    public string ApiKey { get; set; } = string.Empty;
#endif

#if NET10_0_OR_GREATER
    // New property only in NET10.0+
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
#endif
}

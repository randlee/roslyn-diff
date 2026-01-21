namespace ConditionalDemo;

/// <summary>
/// Example class demonstrating TFM-specific conditional compilation.
/// This is the "new" version with various TFM-specific changes.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Method that exists in all TFMs - modified across all frameworks
    public async Task<string> GetDataAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        // Added logging - applies to all TFMs
        Console.WriteLine($"Fetched data from {endpoint}");
        return await response.Content.ReadAsStringAsync();
    }

#if NET8_0_OR_GREATER
    // This method was modified - change applies only to NET8.0+
    public async Task<T> GetJsonAsync<T>(string endpoint) where T : class
    {
        // Added constraint and improved implementation
        var json = await GetDataAsync(endpoint);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)!;
    }

    // New method added only for NET8.0+ (not in NET10)
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
#endif

#if NET10_0_OR_GREATER
    // This method exists unchanged in NET10.0
    public async Task<Stream> GetStreamAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    // New method added only for NET10.0+
    public async Task<T> GetJsonWithCancellationAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
    {
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Deserialization failed");
    }
#endif

    // Legacy method that exists in older frameworks - removed in new version
    // (This entire block is deleted in the new version)
}

/// <summary>
/// Configuration class with TFM-specific settings.
/// </summary>
public class AppConfiguration
{
    public string ApiBaseUrl { get; set; } = "https://api.example.com";

    // Modified in all TFMs - changed default value
    public int TimeoutSeconds { get; set; } = 60;

#if NET8_0_OR_GREATER
    // Modern configuration using newer .NET features - unchanged
    public required string ApiKey { get; set; }
#else
    // Fallback for older frameworks - modified with better default
    public string ApiKey { get; set; } = "default-key";
#endif

#if NET10_0_OR_GREATER
    // Existing property - unchanged
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    // New property added only in NET10.0+
    public bool EnableMetrics { get; set; } = true;
#endif

    // New method added to all TFMs
    public void Validate()
    {
        if (string.IsNullOrEmpty(ApiBaseUrl))
            throw new InvalidOperationException("ApiBaseUrl is required");
        if (string.IsNullOrEmpty(ApiKey))
            throw new InvalidOperationException("ApiKey is required");
    }
}

#if NET10_0_OR_GREATER
/// <summary>
/// Entirely new class that only exists in NET10.0+
/// </summary>
public class MetricsCollector
{
    private readonly TimeProvider _timeProvider;

    public MetricsCollector(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void RecordRequest(string endpoint, TimeSpan duration)
    {
        Console.WriteLine($"[{_timeProvider.GetUtcNow()}] {endpoint}: {duration.TotalMilliseconds}ms");
    }
}
#endif

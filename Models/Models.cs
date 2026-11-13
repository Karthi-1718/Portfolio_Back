namespace PortfolioApi.Models;

/// <summary>Contact form request payload.</summary>
public class ContactRequest
{
    public string Name    { get; set; } = string.Empty;
    public string Email   { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Honeypot – must remain empty. Bots fill this automatically.</summary>
    public string Website { get; set; } = string.Empty;
}

/// <summary>Unified API response envelope.</summary>
public class ApiResponse
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Strongly-typed settings bound from appsettings / environment variables.</summary>
public class ResendSettings
{
    public string ApiKey                { get; set; } = string.Empty;
    public string FromEmail             { get; set; } = string.Empty;
    public string ContactReceiverEmail  { get; set; } = string.Empty;
}

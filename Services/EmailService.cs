using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PortfolioApi.Models;

namespace PortfolioApi.Services;

public interface IEmailService
{
    Task SendContactEmailAsync(ContactRequest request);
}

public class ResendEmailService : IEmailService
{
    private readonly ResendSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IOptions<ResendSettings> settings,
        HttpClient httpClient,
        ILogger<ResendEmailService> logger)
    {
        _settings   = settings.Value;
        _httpClient  = httpClient;
        _logger      = logger;
    }

    public async Task SendContactEmailAsync(ContactRequest req)
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

        var submissionTime = TimeZoneInfo
            .ConvertTimeFromUtc(DateTime.UtcNow, istZone)
            .ToString("dddd, dd MMMM yyyy hh:mm:ss tt");

        var payload = new
        {
            from     = _settings.FromEmail,
            to       = new[] { _settings.ContactReceiverEmail },
            reply_to = req.Email,
            subject  = $"New Portfolio Contact: {req.Subject}",
            html     = BuildHtmlBody(req, submissionTime)
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using var response = await _httpClient.SendAsync(request);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Resend API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Resend API returned {response.StatusCode}: {body}");
        }

        _logger.LogInformation("Email sent via Resend. From: {Email}", req.Email);
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#039;")
            .Replace("\n", "<br/>");

    private static string BuildHtmlBody(ContactRequest req, string submissionTime)
    {
        var n = EscapeHtml(req.Name);
        var e = EscapeHtml(req.Email);
        var s = EscapeHtml(req.Subject);
        var m = EscapeHtml(req.Message);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"UTF-8\"/><style>");
        sb.Append("body{font-family:'Segoe UI',Arial,sans-serif;background:#0a0a0a;color:#e0e0e0;margin:0;padding:0}");
        sb.Append(".container{max-width:600px;margin:40px auto;background:#111827;border-radius:12px;overflow:hidden;border:1px solid #1e3a5f}");
        sb.Append(".header{background:linear-gradient(135deg,#1a1a2e 0%,#16213e 50%,#0f3460 100%);padding:32px;text-align:center}");
        sb.Append(".header h1{margin:0;color:#60a5fa;font-size:24px;letter-spacing:2px}");
        sb.Append(".header p{margin:8px 0 0;color:#94a3b8;font-size:14px}");
        sb.Append(".body{padding:32px}");
        sb.Append(".field{margin-bottom:20px;padding:16px;background:#1e293b;border-radius:8px;border-left:3px solid #3b82f6}");
        sb.Append(".field-label{font-size:11px;text-transform:uppercase;letter-spacing:1px;color:#60a5fa;margin-bottom:6px}");
        sb.Append(".field-value{color:#e2e8f0;font-size:15px;line-height:1.6}");
        sb.Append(".msg-box{background:#1e293b;border-radius:8px;padding:20px;border:1px solid #334155;line-height:1.8;color:#cbd5e1}");
        sb.Append(".footer{background:#0f172a;padding:20px;text-align:center;border-top:1px solid #1e293b}");
        sb.Append(".footer p{margin:0;color:#64748b;font-size:12px}");
        sb.Append(".badge{display:inline-block;background:#1d4ed8;color:#bfdbfe;padding:4px 10px;border-radius:20px;font-size:12px;margin-top:4px}");
        sb.Append("</style></head><body><div class=\"container\">");

        // Header
        sb.Append("<div class=\"header\"><h1>&#128236; New Portfolio Contact</h1>");
        sb.Append("<p>Someone reached out through your portfolio</p></div>");

        // Body
        sb.Append("<div class=\"body\">");

        sb.Append("<div class=\"field\"><div class=\"field-label\">From</div>");
        sb.Append($"<div class=\"field-value\"><strong>{n}</strong></div></div>");

        sb.Append("<div class=\"field\"><div class=\"field-label\">Email Address</div>");
        sb.Append($"<div class=\"field-value\"><a href=\"mailto:{e}\" style=\"color:#60a5fa;\">{e}</a></div></div>");

        sb.Append("<div class=\"field\"><div class=\"field-label\">Subject</div>");
        sb.Append($"<div class=\"field-value\">{s}</div></div>");

        sb.Append("<div class=\"field\"><div class=\"field-label\">Message</div>");
        sb.Append($"<div class=\"msg-box\">{m}</div></div>");

        sb.Append("<div class=\"field\"><div class=\"field-label\">Submitted At</div>");
        sb.Append($"<div class=\"field-value\">{submissionTime}</div>");
        sb.Append("<span class=\"badge\">IST &#8211; Tamil Nadu, India</span></div>");

        sb.Append("</div>"); // body

        // Footer
        sb.Append("<div class=\"footer\">");
        sb.Append("<p>This message was sent via your portfolio contact form.</p>");
        sb.Append($"<p style=\"margin-top:8px;\">Reply directly to respond to {n}.</p></div>");

        sb.Append("</div></body></html>");
        return sb.ToString();
    }
}

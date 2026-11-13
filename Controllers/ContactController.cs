using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Models;
using PortfolioApi.Services;

namespace PortfolioApi.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IValidator<ContactRequest> _validator;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IEmailService emailService,
        IValidator<ContactRequest> validator,
        ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _validator    = validator;
        _logger       = logger;
    }

    /// <summary>
    /// POST /api/contact — Sends a contact form email via Resend.
    /// </summary>
    [HttpPost("contact")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Contact([FromBody] ContactRequest request)
    {
        // Honeypot check — silently accept bots (don't reveal detection)
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogWarning("Honeypot triggered from IP: {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Your message has been sent successfully!"
            });
        }

        // FluentValidation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new { field = e.PropertyName, message = e.ErrorMessage });

            return BadRequest(new
            {
                success = false,
                message = "Validation failed. Please check your input.",
                errors
            });
        }

        try
        {
            await _emailService.SendContactEmailAsync(request);

            _logger.LogInformation(
                "[CONTACT] Email sent. From: {Email} | Subject: {Subject}",
                request.Email, request.Subject);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Your message has been sent successfully! Karthikeyan will get back to you soon."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONTACT] Email delivery failed for {Email}", request.Email);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Failed to send your message. Please try again or email directly."
            });
        }
    }
}

# Karthikeyan K – Portfolio Backend (C# / ASP.NET Core)

> Backend API for the portfolio contact form, built with **ASP.NET Core (.NET 10)**, **C#**, and **Resend** for email delivery.

## 📁 Structure

```
backend-csharp/
├── Controllers/
│   └── ContactController.cs    ← POST /api/contact endpoint
├── Services/
│   └── EmailService.cs         ← Resend HTTP API integration
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
├── Models/
│   └── Models.cs               ← ContactRequest, ApiResponse, ResendSettings
├── Validators/
│   └── ContactRequestValidator.cs  ← FluentValidation rules
├── Program.cs                  ← App bootstrap, DI, middleware pipeline
├── appsettings.json
├── appsettings.Development.json
├── .env.example
└── backend-csharp.csproj
```

## 🚀 Run Locally

```powershell
cd d:\Profile\portfolio\backend-csharp

# 1. Create .env from template
Copy-Item .env.example .env

# 2. Edit .env — add your Resend API key and emails
notepad .env

# 3. Run (hot-reload)
dotnet watch run
# → http://localhost:5000
```

Or without hot-reload:
```powershell
dotnet run
```

## 🔑 Environment Variables (.env)

The checked-in `.env.example` values are templates and cannot send email. Replace all three Resend values before starting the API. The API now validates them at startup and reports a specific configuration error instead of accepting requests that will fail at delivery time.

For a quick Resend test, use `onboarding@resend.dev` as the sender and send only to the email address verified on your Resend account. For production, verify your domain in Resend and set `RESEND_FROM_EMAIL` to an address on that domain.

| Variable | Description |
|----------|-------------|
| `RESEND_API_KEY` | From [resend.com/api-keys](https://resend.com/api-keys) |
| `CONTACT_RECEIVER_EMAIL` | Your personal email to receive messages |
| `RESEND_FROM_EMAIL` | Verified sender from your Resend domain |
| `FRONTEND_URL` | Angular app URL (default: `http://localhost:4200`) |
| `PORT` | Server port (default: `5000`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |

## 🧪 Test the API

**Health check:**
```
GET http://localhost:5000/health
```

**Contact form (Postman / curl):**
```json
POST http://localhost:5000/api/contact
Content-Type: application/json

{
  "name": "Test Recruiter",
  "email": "recruiter@company.com",
  "subject": "Job Opportunity",
  "message": "Hi Karthikeyan! I found your portfolio and would love to connect.",
  "website": ""
}
```

**Interactive API Docs (dev only):**
```
GET http://localhost:5000/scalar
```

## 📦 NuGet Packages

| Package | Purpose |
|---------|---------|
| `AspNetCoreRateLimit` | IP-based rate limiting (5 req / 15 min on `/api/contact`) |
| `FluentValidation.AspNetCore` | Input validation with fluent rules |
| `Microsoft.AspNetCore.OpenApi` | Built-in .NET 10 OpenAPI schema generation |
| `Scalar.AspNetCore` | Beautiful API reference UI |
| `dotenv.net` | Load `.env` file into `IConfiguration` |

## 🔒 Security Features

- ✅ IP rate limiting — 5 requests per 15 minutes on the contact endpoint
- ✅ Input validation — FluentValidation with strict rules
- ✅ Honeypot spam protection — `website` field must remain empty
- ✅ CORS — only allows requests from the configured frontend URL
- ✅ No stack traces in production responses
- ✅ Resend API key stored in env only — never in source code
- ✅ Request body size implicitly limited by ASP.NET Core defaults

## 🏗️ Production Deployment

```powershell
dotnet publish -c Release -o ./publish
```

Then run on your server:
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet ./publish/PortfolioApi.dll
```

Set all environment variables on your hosting platform (Railway, Azure App Service, Fly.io, etc.).

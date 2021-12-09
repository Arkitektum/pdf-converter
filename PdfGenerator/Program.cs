using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using PdfGenerator.Security;
using PdfGenerator.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddSingleton<IBrowserProvider, BrowserProvider>();
services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
services.AddTransient<IPdfService, PdfService>();
services.AddTransient<IAuthorizationHandler, ApiKeyRequirementHandler>();

services.Configure<PdfSettings>(configuration.GetSection(PdfSettings.SectionName));

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

var authenticationConfig = configuration.GetSection(AuthenticationConfig.SectionName);
var apiKey = authenticationConfig.GetSection("ApiKey").Value;

services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyPolicy",
        policyBuilder => policyBuilder.AddRequirements(new ApiKeyRequirement(new[] { apiKey })));
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("/logs/pdf-generator-.txt", rollingInterval: RollingInterval.Day)
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger, true);

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PDF Generator API v1");
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

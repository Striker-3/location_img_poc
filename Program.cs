using location_img_poc.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<WhatsAppService>();

var app = builder.Build();

// Trust X-Forwarded-* headers from Render
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Enable Swagger also on Render if you want (set ENABLE_SWAGGER=true in Render env vars)
var enableSwagger = app.Environment.IsDevelopment() ||
                    string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Behind a proxy like Render, you can skip HTTPS redirect (Render does TLS at the edge).
// If you insist on keeping it, set ASPNETCORE_HTTPS_PORT in env. Safer to comment out:
//// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { ok = true, service = "location_img_poc" }));

app.Run();

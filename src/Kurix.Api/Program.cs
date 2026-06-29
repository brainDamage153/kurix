using System.Text;
using Kurix.Api.Auth;
using Kurix.Api.Middleware;
using Kurix.Api.Seeding;
using Kurix.Infrastructure;
using Kurix.Infrastructure.Configuration;
using Kurix.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure: EF Core (Azure SQL), options binding, external clients.
builder.Services.AddInfrastructure(builder.Configuration);

// Module 1 tools (with mock connectors) contributed to the tool registry.
builder.Services.AddKurixTools();

// JWT issuing for dashboard login.
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// The widget is embedded on third-party sites; the API key authorizes requests.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// --- Authentication (JWT for the dashboard) ---
// Configure validation from IOptions<JwtOptions> so the validation key/issuer
// always match what JwtTokenService uses to sign (both bind the final config).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DevDataSeeder.SeedAsync(app);
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Resolve the tenant from the widget API key (no-op when no key header is present).
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

// Lightweight liveness endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;

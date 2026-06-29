using Kurix.Api.Middleware;
using Kurix.Infrastructure;
using Kurix.Tools;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure: EF Core (Azure SQL), options binding, external clients.
builder.Services.AddInfrastructure(builder.Configuration);

// Module 1 tools (with mock connectors) contributed to the tool registry.
builder.Services.AddKurixTools();

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Resolve the tenant from the widget API key before hitting controllers.
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

// Lightweight liveness endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;

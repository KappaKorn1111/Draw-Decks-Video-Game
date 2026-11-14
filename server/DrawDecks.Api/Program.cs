using DrawDecks.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", service = "draw-decks-api" }));
app.MapGet("/api/v1", () => Results.Ok(new
{
    name = "Draw Decks API",
    apiVersion = "v1",
    domainAssembly = typeof(MatchFactory).Assembly.GetName().Version?.ToString() ?? "unknown"
}));
app.Run();

public partial class Program { }

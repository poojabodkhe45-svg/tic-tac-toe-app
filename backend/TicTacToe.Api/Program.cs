using System.Text.Json.Serialization;
using TicTacToe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.Services.AddSingleton<IGameService, GameService>();

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure CORS for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

// Root landing message
app.MapGet("/", () => Results.Content(
    "<!DOCTYPE html><html><head><title>Tic Tac Toe API</title><style>body{font-family:sans-serif;padding:3rem;background:#0f172a;color:#f8fafc;line-height:1.6}a{color:#38bdf8;text-decoration:none;font-weight:bold}code{background:#1e293b;padding:0.2rem 0.5rem;border-radius:4px}</style></head><body>" +
    "<h1>🎮 Tic Tac Toe .NET Web API is Running!</h1>" +
    "<p>This port serves the <strong>REST API endpoints</strong> (JSON data).</p>" +
    "<p>To play the game UI, run the Angular app and open: <a href='http://localhost:4200' target='_blank'>http://localhost:4200</a></p>" +
    "<hr style='border:1px solid #334155;margin:2rem 0;' />" +
    "<h3>Available API Endpoints:</h3>" +
    "<ul>" +
    "<li><code>GET /api/scoreboard</code> - <a href='/api/scoreboard'>View Scoreboard JSON</a></li>" +
    "<li><code>POST /api/games</code> - Create new game session</li>" +
    "<li><code>POST /api/games/{id}/moves</code> - Submit move</li>" +
    "</ul></body></html>", 
    "text/html"
));

app.MapControllers();

app.Run();

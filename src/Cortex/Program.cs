using Cortex.Database;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Infrastructure.Settings;
using Cortex.AI.Workers;
using Cortex.Modules.Auth;
using Cortex.Modules.Content;
using Cortex.Modules.Content.Hubs;
using Cortex.Modules.Drip;
using Cortex.Modules.Search;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add standard Web API controllers and Swagger documentation
// Add standard Web API controllers, Swagger, and SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173", "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register Database Context (PostgreSQL + pgvector)
builder.Services.AddDbContext<CortexDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CortexDatabase"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseVector();
            npgsqlOptions.MigrationsAssembly(typeof(CortexDbContext).Assembly.FullName);
        });
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<DatabaseSeeder>();

// Configure Global Settings Options
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<AIServiceSettings>(builder.Configuration.GetSection("AIService"));

// Caching (Redis ConnectionMultiplexer & CacheService)
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Messaging (RabbitMQ ConnectionFactory & MessageBroker)
var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
var factory = new ConnectionFactory
{
    HostName = rabbitSettings.HostName,
    Port = rabbitSettings.Port,
    UserName = rabbitSettings.UserName,
    Password = rabbitSettings.Password
};
builder.Services.AddSingleton<IConnectionFactory>(factory);
builder.Services.AddScoped<IMessageBroker, RabbitMqMessageBroker>();

// AI Extraction Service & hosted worker
var aiSettings = builder.Configuration.GetSection("AIService").Get<AIServiceSettings>();
if (aiSettings?.ActiveProvider == "LocalOllama")
{
    builder.Services.AddHttpClient<IAIExtractionService, OllamaExtractionService>();
}
else
{
    builder.Services.AddHttpClient<IAIExtractionService, GeminiExtractionService>();
}
builder.Services.AddHttpClient<IPythonAIService, PythonAIService>();
builder.Services.AddHostedService<AIExtractionWorker>();

// Register Vertical Slice Modules
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddContentModule(builder.Configuration);
builder.Services.AddDripModule(builder.Configuration);
builder.Services.AddSearchModule(builder.Configuration);

// QuickBoost AI services registration
builder.Services.AddScoped<Cortex.Services.IInterestProfiler, Cortex.Services.InterestProfiler>();
builder.Services.AddScoped<Cortex.Services.IVideoFetcher, Cortex.Services.VideoFetcher>();
builder.Services.AddScoped<Cortex.Services.ITranscriptionService, Cortex.Services.TranscriptionService>();
builder.Services.AddScoped<Cortex.Services.IEmotionAnalyzer, Cortex.Services.EmotionAnalyzer>();
builder.Services.AddScoped<Cortex.Services.IClipSelector, Cortex.Services.ClipSelector>();


// Setup Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var signingKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect root URL to Swagger UI (avoids 404 on /)
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseSerilogRequestLogging();

// Global Exception Handler
app.UseMiddleware<Cortex.Middleware.ExceptionHandlingMiddleware>();

// Request/Response Logging
app.UseMiddleware<Cortex.Infrastructure.Logging.RequestResponseLoggingMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ContentHub>("/hubs/content");

app.Run();

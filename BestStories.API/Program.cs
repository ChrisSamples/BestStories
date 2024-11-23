using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BestStories.API;
using BestStories.Infrastructure;
using BestStories.Infrastructure.Config;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfigurationSection section = builder.Configuration.GetSection("BestStoriesOptions");
BestStoriesConfig bestStoriesConfig = section.Get<BestStoriesConfig>()
    ?? throw new ArgumentException("Missing section in config.", "BestStoriesOptions");
//builder.Services.Configure<BestStoriesConfig>(section);

string? logSeqUrl = bestStoriesConfig.LogSeqUrl; // "http://localhost:5341"

// Configure Serilog
LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
// Seq for centralized log management (apply only if configured in config)
if (!string.IsNullOrEmpty(logSeqUrl))
    loggerConfiguration.WriteTo.Seq(logSeqUrl);
Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog(); // Use Serilog as the logging provider

// Add services to the container.

builder.Services.AddBestStoriesServiceInfrastructure(bestStoriesConfig);

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
});

builder.Services.AddControllers()
   .AddJsonOptions(options =>
   {
       options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
       options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

       // Ensure time zone offset is included in serialized dates
       options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
   });

builder.Services.AddApiVersioning(x =>
{
    //x.DefaultApiVersion = new ApiVersion(1, 0);
    x.AssumeDefaultVersionWhenUnspecified = false;
    x.ReportApiVersions = true;
    x.ApiVersionReader = new UrlSegmentApiVersionReader();
})
    .AddApiExplorer(options =>
    {
        // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
        // note: the specified format code will format the version as "'v'major[.minor][-status]"
        options.GroupNameFormat = "'v'VVV";
        // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
        // can also be used to control the format of the API version in route templates
        options.SubstituteApiVersionInUrl = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<NamedSwaggerGenOptions>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("Fixed", policy =>
    {
        policy.PermitLimit = 2; // Max requests allowed in the time window
        policy.Window = TimeSpan.FromSeconds(10); // Reset window every 10 seconds
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 5; // Allow up to 5 requests to queue
    });
    options.AddSlidingWindowLimiter("Sliding", policy =>
    {
        policy.PermitLimit = 10; // Max requests allowed
        policy.Window = TimeSpan.FromSeconds(10); // Sliding window size
        policy.SegmentsPerWindow = 2; // Divide the window into 2 segments
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 5; // Allow up to 5 requests to queue
    });
    options.AddConcurrencyLimiter("Concurrency", policy =>
    {
        policy.PermitLimit = 10; // Max concurrent requests
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 5; // Allow up to 5 requests to queue
    });
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Add a Swagger endpoint for each API version
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                    $"API {description.GroupName.ToUpperInvariant()}");
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

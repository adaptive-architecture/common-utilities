using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Samples.ConsistentHashing;
using AdaptArch.Common.Utilities.Samples.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOptions<RepeatingWorkerConfiguration>()
    .BindConfiguration("jobWorkers");

// Add background jobs
builder.Services
    .AddSingleton<WorkersState>()
    .AddBackgroundJobs()
    .WithDelayedJob<RandomNumberGeneratorJob>()
    .WithPeriodicJob<ReporterJob>();

// Configure versioned static files middleware
builder.Services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesPathPrefix = "/static/";
    options.StaticFilesDirectory = "wwwroot/static";
    options.VersionCookieNamePrefix = ".aa_sfv";
    options.UseCacheDuration = ctx => ctx.RequestPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
        ? TimeSpan.Zero
        : TimeSpan.FromHours(1);
});

var app = builder.Build();

// Run console examples on startup
Console.WriteLine("=== AdaptArch Common Utilities Samples ===\n");
Console.WriteLine("=== Consistent Hashing Examples ===");
DatabaseRoutingExample.RunExample();
HttpRoutingExample.RunExample();
HistoryManagementExample.RunExample();
Console.WriteLine("\n=== Background Worker Examples ===");
Console.WriteLine("Background jobs started (RandomNumberGenerator + Reporter)\n");

// Configure HTTP request pipeline
app.UseVersionedStaticFiles();

// Serve static files from wwwroot
app.UseStaticFiles();

// Map sample endpoints
app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapGet("/api/version", (HttpContext context) =>
{
    return context.Request.Cookies
        .Where(c => c.Key.StartsWith(".aa_sfv_v_"))
        .OrderBy(c => c.Key)
        .ToDictionary(c => c.Key, c =>
        {
            var versionPayload = VersionCookiePayload.TryParse(c.Value, out var vp) ? vp : null;
            return new
            {
                versionPayload?.Version,
                versionPayload?.DateModified
            };
        });
});

app.MapGet("/api/set-version/{directory}/{version}", (string directory, string version, HttpContext context) =>
{
    var cookieName = $".aa_sfv_v_{directory}";
    var timestamp = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
    var cookieValue = $"{timestamp}~{version}";

    context.Response.Cookies.Append(cookieName, cookieValue, new CookieOptions
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromDays(30)
    });

    return Results.Ok(new { Success = true, CookieName = cookieName, CookieValue = cookieValue });
});

app.MapGet("/api/clear-version/{directory}", (string directory, HttpContext context) =>
{
    var cookieName = $".aa_sfv_v_{directory}";
    context.Response.Cookies.Delete(cookieName);
    return Results.Ok(new { Success = true, CookieName = cookieName, Cleared = true });
});

Console.WriteLine($"\nASP.NET Core app listening on: {String.Join(", ", builder.Configuration["ASPNETCORE_URLS"]?.Split(';') ?? ["http://localhost:5000"])}");
Console.WriteLine("Navigate to http://localhost:5000 to see the versioned static files demo\n");

await app.RunAsync();

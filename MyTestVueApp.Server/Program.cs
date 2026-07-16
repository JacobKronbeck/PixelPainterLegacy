using Microsoft.Extensions.Configuration;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using MyTestVueApp.Server.Hubs;
using MyTestVueApp.Server.Auth;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pixel Painter Legacy API",
        Version = "v1",
        Description = "ASP.NET Core API for Pixel Painter Legacy accounts, artwork, comments, reactions, notifications, tags, and map points."
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services.AddSignalR(sig => {
  sig.MaximumReceiveMessageSize = 524288;
});

builder.Services
    .AddOptions<ApplicationConfiguration>()
    .Bind(builder.Configuration.GetSection("ApplicationConfiguration"))
    .Validate(config => !string.IsNullOrWhiteSpace(config.ConnectionString),
        "ApplicationConfiguration:ConnectionString is required.")
    .Validate(config => builder.Environment.IsDevelopment()
        || (!string.IsNullOrWhiteSpace(config.ClientId)
            && !string.IsNullOrWhiteSpace(config.ClientSecret)
            && !string.IsNullOrWhiteSpace(config.RedirectUrl)
            && !string.IsNullOrWhiteSpace(config.OAuthRedirectUrl)),
        "Production requires Google OAuth credentials and redirect URLs.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = AuthCookieOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

var frontendOrigins = new[]
{
    builder.Configuration["ApplicationConfiguration:RedirectUrl"],
    builder.Configuration["ApplicationConfiguration:PostLoginRedirectUrl"],
    "https://pixel-painter-legacy.vercel.app",
    "http://localhost:5173"
}
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//Custom Services
builder.Services.AddTransient<IPostgresDataAccess, PostgresDataAccess>();
builder.Services.AddTransient<IArtAccessService, ArtAccessService>();
builder.Services.AddTransient<ILoginService, LoginService>();
builder.Services.AddTransient<ILikeService, LikeService>();
builder.Services.AddTransient<IDislikeService, DislikeService>();
builder.Services.AddTransient<ICommentDislikeService, CommentDislikeService>();
builder.Services.AddTransient<ICommentLikeService, CommentLikeService>();
builder.Services.AddTransient<ICommentAccessService, CommentAccessService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddTransient<ITagService, TagService>();
builder.Services.AddTransient<IArtistService, ArtistService>();
builder.Services.AddTransient<IMapAccessService, MapAccessService>();
builder.Services.AddTransient<IFriendsService, FriendsService>();
builder.Services.AddTransient<IGoogleOAuthClient, GoogleOAuthClient>();
builder.Services.AddTransient<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddTransient<IV2AccountService, V2AccountService>();
builder.Services.AddTransient<IV2ArtService, V2ArtService>();

var app = builder.Build();

var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var hasWebRoot = Directory.Exists(webRootPath);

if (hasWebRoot)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("FrontendCors");

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var isMutation = HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);

    if (isMutation && context.User.Identity?.IsAuthenticated == true)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            var requestOrigin = $"{context.Request.Scheme}://{context.Request.Host}".TrimEnd('/');
            var isAllowedOrigin = string.Equals(origin.TrimEnd('/'), requestOrigin, StringComparison.OrdinalIgnoreCase)
                || frontendOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase);

            if (!isAllowedOrigin)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Cross-origin authenticated mutation rejected." });
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

if (hasWebRoot)
{
    app.MapFallbackToFile("/index.html");
}

app.MapHub<SignalHub>("/signalHub");

app.Run();

public partial class Program
{
}

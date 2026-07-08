using Microsoft.Extensions.Configuration;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using MyTestVueApp.Server.Hubs;
using Microsoft.OpenApi.Models;
using System.Reflection;

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

builder.Services.Configure<ApplicationConfiguration>(builder.Configuration.GetSection("ApplicationConfiguration"));

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

app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

if (hasWebRoot)
{
    app.MapFallbackToFile("/index.html");
}

app.MapHub<SignalHub>("/signalHub");

app.Run();

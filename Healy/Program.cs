using Azure;
using Azure.AI.OpenAI;
using Healy.Models;
using Healy.Services;
using Microsoft.Azure.Cosmos;
using OpenAI.Chat;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/debug.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<Healy.Services.BlobService>();

builder.Services.Configure<AzureOpenAIModel>(builder.Configuration.GetSection("AzureOpenAI"));

builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("AzureOpenAI").Get<AzureOpenAIModel>();
    return new AzureOpenAIClient(
        new Uri(config.Endpoint!),
        new AzureKeyCredential(config.ApiKey!));
});

builder.Services.AddSingleton<ChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("AzureOpenAI").Get<AzureOpenAIModel>();
    var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
    return azureClient.GetChatClient(config.DeploymentName!);
});

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpointUri = configuration["CosmosDb:EndpointUri"];
    var primaryKey = configuration["CosmosDb:PrimaryKey"];
    return new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions
    {
        ApplicationName = "Healy"
    });
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<IAiAnalysisService<InsightsData>, AIInsightsAnalysisService>();
builder.Services.AddSingleton<IAiAnalysisService<ActivitiesData>, AIActivitiesAnalysisService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "profile-shortcut",
    pattern: "Profile",
    defaults: new { controller = "Home", action = "Profile" });

app.MapControllerRoute(
    name: "insights-shortcut",
    pattern: "Insights",
    defaults: new { controller = "Insights", action = "Index" });

app.MapControllerRoute(
    name: "activities-shortcut",
    pattern: "Activities",
    defaults: new { controller = "Activities", action = "Index" });

app.MapControllerRoute(
    name: "login-shortcut",
    pattern: "Login",
    defaults: new { controller = "Home", action = "Login" });

app.MapControllerRoute(
    name: "register-shortcut",
    pattern: "Register",
    defaults: new { controller = "Home", action = "Register" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

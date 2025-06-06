var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Healy.Services.BlobService>();

var app = builder.Build();

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
    defaults: new { controller = "Home", action = "Insights" });

app.MapControllerRoute(
    name: "activities-shortcut",
    pattern: "Activities",
    defaults: new { controller = "Home", action = "Activities" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

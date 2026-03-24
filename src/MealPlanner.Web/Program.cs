using MealPlanner.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("MealPlannerApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});

builder.Services.AddScoped<MealPlanner.Web.Services.RecipeApiClient>();
builder.Services.AddScoped<MealPlanner.Web.Services.MealPlanApiClient>();
builder.Services.AddScoped<MealPlanner.Web.Services.ShoppingListApiClient>();
builder.Services.AddScoped<MealPlanner.Web.Services.FridgeApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using AgentPortal;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDataProtection().PersistKeysToFileSystem(
    new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddScoped<WholesaleStore>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error");
app.UseStaticFiles();
app.UseSession();
app.Use(async (context, next) =>
{
    var agentId = context.Session.GetInt32("agentId");
    if (agentId.HasValue && !context.Request.Path.StartsWithSegments("/login") && !context.Request.Path.StartsWithSegments("/error"))
    {
        var store = context.RequestServices.GetRequiredService<WholesaleStore>();
        try
        {
            if (!await store.IsAgentActiveAsync(agentId.Value, context.Session.GetString("user")))
            {
                context.Session.Clear(); context.Response.Redirect("/login"); return;
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("The database is temporarily unavailable. Please retry shortly."); return;
        }
    }
    await next();
});
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.MapRazorPages();
app.Run();

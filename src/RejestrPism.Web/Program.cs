using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Api;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var sql = builder.Configuration.GetConnectionString("SqlServer");

builder.Services.AddDbContext<AppDb>(o =>
{
    if (!string.IsNullOrWhiteSpace(sql)) o.UseSqlServer(sql, s => s.EnableRetryOnFailure());
    else o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=rejestr-dev.db");
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 12;
        // Hasła generowane przez Coolify są alfanumeryczne (16+ znaków), więc nie wymagamy znaku specjalnego.
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDb>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Konto/Logowanie";
    o.AccessDeniedPath = "/Konto/BrakDostepu";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.Events.OnRedirectToLogin = ctx => { if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401; else ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 403; else ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Pisma");
});
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Rejestr pism – API (DEMO)", Version = "v1", Description = "Projekt pokazowy Isvero Studio. Dane fikcyjne." }));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RejestrService>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDb>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    if (db.Database.IsSqlServer()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
    if (!app.Configuration.GetValue<bool>("Demo:SkipSeed"))
        await Seed.RunAsync(scope.ServiceProvider, app.Configuration["Demo:Password"]);
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
// Strony błędów (np. 404) z pełnym układem: tytuł, lang, nawigacja. API zwraca same kody.
app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api"), b => b.UseStatusCodePagesWithReExecute("/Error", "?kod={0}"));
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; frame-ancestors 'none'";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "Rejestr pism – API (DEMO)");
app.MapHealthChecks("/health");
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /\n"));
PismaApi.Map(app);
app.MapRazorPages();
app.Run();

public partial class Program;

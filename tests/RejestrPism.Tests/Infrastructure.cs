using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RejestrPism.Web.Data;

namespace RejestrPism.Tests;

/// <summary>Aplikacja na SQLite w pamięci + testowe uwierzytelnianie nagłówkiem X-Test-User / X-Test-Roles.</summary>
public class AppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    public AppFactory() => _conn.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Demo:SkipSeed", "true");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<AppDb>>();
            s.AddDbContext<AppDb>(o => o.UseSqlite(_conn));
            s.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuth.Scheme;
                o.DefaultChallengeScheme = TestAuth.Scheme;
                o.DefaultForbidScheme = TestAuth.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuth>(TestAuth.Scheme, _ => { });
        });
    }

    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _conn.Dispose(); }
}

static class ServiceCollectionExt
{
    public static void RemoveAll<T>(this IServiceCollection s)
    {
        foreach (var d in s.Where(x => x.ServiceType == typeof(T)).ToList()) s.Remove(d);
    }
}

public class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e) : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    public const string Scheme = "Test";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim> { new(ClaimTypes.Name, user.ToString()) };
        foreach (var r in Request.Headers["X-Test-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)) claims.Add(new(ClaimTypes.Role, r));
        var id = new ClaimsIdentity(claims, Scheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme)));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties p) { Response.StatusCode = 401; return Task.CompletedTask; }
    protected override Task HandleForbiddenAsync(AuthenticationProperties p) { Response.StatusCode = 403; return Task.CompletedTask; }
}

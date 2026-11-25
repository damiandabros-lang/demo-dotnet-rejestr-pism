using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;

namespace RejestrPism.Web.Services;

public static class Seed
{
    public const string RolaKancelaria = "Kancelaria";
    public const string RolaPracownik = "Pracownik";
    /// <summary>Publiczne konto tylko do podglądu (bez ról). Hasło jest jawne, bo konto niczego nie zmienia.</summary>
    public const string GoscEmail = "gosc@demo.example";
    public const string GoscHasloDomyslne = "Gosc-Demo-2026";

    public static async Task RunAsync(IServiceProvider sp, string? demoPassword, string? goscPassword = GoscHasloDomyslne)
    {
        var db = sp.GetRequiredService<AppDb>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<IdentityUser>>();
        foreach (var r in new[] { RolaKancelaria, RolaPracownik })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        if (!string.IsNullOrWhiteSpace(demoPassword))
        {
            await Uzytkownik(users, "kancelaria@demo.example", demoPassword, RolaKancelaria, RolaPracownik);
            await Uzytkownik(users, "pracownik@demo.example", demoPassword, RolaPracownik);
        }
        if (!string.IsNullOrWhiteSpace(goscPassword))
        {
            await Uzytkownik(users, GoscEmail, goscPassword);
            var g = await users.FindByEmailAsync(GoscEmail);
            if (g is not null && g.LockoutEnabled) await users.SetLockoutEnabledAsync(g, false); // nikt nie zablokuje konta gościa
        }

        if (await db.Pisma.AnyAsync()) return;
        var svc = sp.GetRequiredService<RejestrService>();
        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);
        var dane = new (Kierunek, string, string, string, string, int)[]
        {
            (Kierunek.Przychodzace, "Jan Kowalski-Przykładowy (osoba fikcyjna)", "Wniosek o wydanie zaświadczenia o przeznaczeniu działki", "GP.6727.1.2026", "Referat Gospodarki Przestrzennej", -12),
            (Kierunek.Przychodzace, "Stowarzyszenie Przykładowe (fikcyjne)", "Oferta realizacji zadania publicznego", "OR.524.3.2026", "Referat Organizacyjny", -9),
            (Kierunek.Przychodzace, "Firma Testowa sp. z o.o. (fikcyjna)", "Faktura za usługi odbioru odpadów – listopad", "FN.3026.41.2026", "Referat Finansowy", -6),
            (Kierunek.Wychodzace, "Jan Kowalski-Przykładowy (osoba fikcyjna)", "Zaświadczenie o przeznaczeniu działki", "GP.6727.1.2026", "Referat Gospodarki Przestrzennej", -4),
            (Kierunek.Przychodzace, "Anna Wzorcowa (osoba fikcyjna)", "Skarga na stan drogi gminnej", "OR.1510.2.2026", "Referat Inwestycji", -2),
            (Kierunek.Przychodzace, "Urząd Wojewódzki – nazwa fikcyjna", "Prośba o informację o realizacji programu", "OR.0002.7.2026", "Sekretariat", -1),
        };
        foreach (var (k, kto, co, znak, kom, dni) in dane)
        {
            var p = await svc.ZarejestrujAsync(new NowePismo(k, kto, co, dzis.AddDays(dni), znak, kom, k == Kierunek.Przychodzace ? dzis.AddDays(dni + 30) : null), "system-demo");
            if (dni < -5) await svc.ZmienStatusAsync(p.Id, StatusPisma.Zalatwione, "system-demo");
            else if (dni < -2) await svc.ZmienStatusAsync(p.Id, StatusPisma.WRealizacji, "system-demo");
        }
    }

    private static async Task Uzytkownik(UserManager<IdentityUser> users, string email, string pass, params string[] role)
    {
        var u = await users.FindByEmailAsync(email);
        if (u is null)
        {
            u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var r = await users.CreateAsync(u, pass);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
        }
        foreach (var x in role) if (!await users.IsInRoleAsync(u, x)) await users.AddToRoleAsync(u, x);
    }
}

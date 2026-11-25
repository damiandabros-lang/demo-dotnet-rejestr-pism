using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;

namespace RejestrPism.Web.Services;

public record NowePismo(Kierunek Kierunek, string Kontrahent, string Przedmiot, DateOnly DataPisma, string? ZnakSprawy, string? Komorka, DateOnly? Termin);

public class RejestrService(AppDb db, TimeProvider clock)
{
    public static string Prefiks(Kierunek k) => k == Kierunek.Przychodzace ? "KP" : "KW";

    /// <summary>Rejestruje pismo i nadaje kolejny numer kancelaryjny w danym roku (z ponowieniem przy kolizji).</summary>
    public async Task<Pismo> ZarejestrujAsync(NowePismo n, string login, CancellationToken ct = default)
    {
        var teraz = clock.GetUtcNow();
        for (var proba = 0; proba < 5; proba++)
        {
            var rok = n.DataPisma.Year;
            var ostatni = await db.Pisma.Where(p => p.Kierunek == n.Kierunek && p.Rok == rok).MaxAsync(p => (int?)p.Kolejny, ct) ?? 0;
            var p = new Pismo
            {
                Kierunek = n.Kierunek, Rok = rok, Kolejny = ostatni + 1,
                Numer = $"{Prefiks(n.Kierunek)}/{rok}/{ostatni + 1:D5}",
                Kontrahent = n.Kontrahent.Trim(), Przedmiot = n.Przedmiot.Trim(),
                ZnakSprawy = string.IsNullOrWhiteSpace(n.ZnakSprawy) ? null : n.ZnakSprawy.Trim(),
                Komorka = string.IsNullOrWhiteSpace(n.Komorka) ? null : n.Komorka.Trim(),
                DataPisma = n.DataPisma, Termin = n.Termin, Utworzono = teraz, UtworzylLogin = login,
            };
            p.Historia.Add(new WpisHistorii { Kiedy = teraz, Kto = login, Opis = $"Zarejestrowano pismo {p.Numer}" });
            db.Pisma.Add(p);
            try { await db.SaveChangesAsync(ct); return p; }
            catch (DbUpdateException) when (proba < 4) { db.ChangeTracker.Clear(); }
        }
        throw new InvalidOperationException("Nie udało się nadać numeru kancelaryjnego.");
    }

    public async Task<Pismo?> ZmienStatusAsync(int id, StatusPisma status, string login, CancellationToken ct = default)
    {
        var p = await db.Pisma.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        if (p.Status == status) return p;
        var stary = p.Status;
        p.Status = status;
        db.Historia.Add(new WpisHistorii { PismoId = p.Id, Kiedy = clock.GetUtcNow(), Kto = login, Opis = $"Status: {stary.Nazwa()} → {status.Nazwa()}" });
        await db.SaveChangesAsync(ct);
        return p;
    }

    public IQueryable<Pismo> Szukaj(string? q, StatusPisma? status, Kierunek? kierunek)
    {
        var x = db.Pisma.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            x = x.Where(p => p.Numer.Contains(t) || p.Kontrahent.Contains(t) || p.Przedmiot.Contains(t) || (p.ZnakSprawy != null && p.ZnakSprawy.Contains(t)));
        }
        if (status is not null) x = x.Where(p => p.Status == status);
        if (kierunek is not null) x = x.Where(p => p.Kierunek == kierunek);
        return x.OrderByDescending(p => p.Rok).ThenByDescending(p => p.DataPisma).ThenByDescending(p => p.Kolejny);
    }
}

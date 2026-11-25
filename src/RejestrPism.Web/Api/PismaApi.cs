using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

namespace RejestrPism.Web.Api;

public record PismoDto(int Id, string Numer, string Kierunek, string Kontrahent, string Przedmiot, string? ZnakSprawy, string? Komorka, DateOnly DataPisma, DateOnly? Termin, string Status);
public record NowePismoDto(
    [property: Required] Kierunek Kierunek,
    [property: Required, MaxLength(200)] string Kontrahent,
    [property: Required, MaxLength(300)] string Przedmiot,
    DateOnly DataPisma, string? ZnakSprawy, string? Komorka, DateOnly? Termin);
public record ZmianaStatusuDto(StatusPisma Status);
public record StronaDto<T>(IReadOnlyList<T> Pozycje, int Strona, int Rozmiar, int Razem);

public static class PismaApi
{
    static PismoDto Dto(Pismo p) => new(p.Id, p.Numer, p.Kierunek.ToString(), p.Kontrahent, p.Przedmiot, p.ZnakSprawy, p.Komorka, p.DataPisma, p.Termin, p.Status.ToString());

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/pisma").RequireAuthorization().WithTags("Pisma");

        g.MapGet("/", async (RejestrService svc, string? q, StatusPisma? status, Kierunek? kierunek, int strona = 1, int rozmiar = 20) =>
        {
            rozmiar = Math.Clamp(rozmiar, 1, 100); strona = Math.Max(1, strona);
            var query = svc.Szukaj(q, status, kierunek);
            var razem = await query.CountAsync();
            var items = await query.Skip((strona - 1) * rozmiar).Take(rozmiar).ToListAsync();
            return Results.Ok(new StronaDto<PismoDto>(items.Select(Dto).ToList(), strona, rozmiar, razem));
        }).WithSummary("Lista pism z wyszukiwaniem i stronicowaniem");

        g.MapGet("/{id:int}", async (int id, AppDb db) =>
            await db.Pisma.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id) is { } p ? Results.Ok(Dto(p)) : Results.NotFound())
         .WithSummary("Szczegóły pisma");

        g.MapPost("/", async (NowePismoDto n, RejestrService svc, HttpContext ctx) =>
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(n, new ValidationContext(n), errors, true))
                return Results.ValidationProblem(errors.GroupBy(e => e.MemberNames.FirstOrDefault() ?? "").ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage ?? "").ToArray()));
            if (string.IsNullOrWhiteSpace(n.Kontrahent) || string.IsNullOrWhiteSpace(n.Przedmiot))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["Pismo"] = ["Nadawca i przedmiot są wymagane."] });
            var p = await svc.ZarejestrujAsync(new NowePismo(n.Kierunek, n.Kontrahent, n.Przedmiot, n.DataPisma, n.ZnakSprawy, n.Komorka, n.Termin), ctx.User.Identity!.Name!);
            return Results.Created($"/api/pisma/{p.Id}", Dto(p));
        }).RequireAuthorization(pol => pol.RequireRole(Seed.RolaKancelaria)).WithSummary("Rejestracja pisma (rola Kancelaria)");

        g.MapPost("/{id:int}/status", async (int id, ZmianaStatusuDto z, RejestrService svc, HttpContext ctx) =>
            await svc.ZmienStatusAsync(id, z.Status, ctx.User.Identity!.Name!) is { } p ? Results.Ok(Dto(p)) : Results.NotFound())
         .WithSummary("Zmiana statusu pisma");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

namespace RejestrPism.Tests;

public class RejestrTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _f;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public RejestrTests(AppFactory f) => _f = f;

    HttpClient Klient(string? user = null, string roles = "")
    {
        var c = _f.CreateClient(new() { AllowAutoRedirect = false });
        if (user is not null) { c.DefaultRequestHeaders.Add("X-Test-User", user); c.DefaultRequestHeaders.Add("X-Test-Roles", roles); }
        return c;
    }

    [Fact]
    public async Task Numeracja_jest_kolejna_i_osobna_dla_kierunku()
    {
        using var scope = _f.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<RejestrService>();
        var d = new DateOnly(2031, 3, 1); // osobny rok, żeby nie kolidować z innymi testami
        var a = await svc.ZarejestrujAsync(new NowePismo(Kierunek.Przychodzace, "A", "Test 1", d, null, null, null), "t");
        var b = await svc.ZarejestrujAsync(new NowePismo(Kierunek.Przychodzace, "B", "Test 2", d, null, null, null), "t");
        var c = await svc.ZarejestrujAsync(new NowePismo(Kierunek.Wychodzace, "C", "Test 3", d, null, null, null), "t");
        Assert.Equal("KP/2031/00001", a.Numer);
        Assert.Equal("KP/2031/00002", b.Numer);
        Assert.Equal("KW/2031/00001", c.Numer);
    }

    [Fact]
    public async Task Zmiana_statusu_zapisuje_historie()
    {
        using var scope = _f.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<RejestrService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var p = await svc.ZarejestrujAsync(new NowePismo(Kierunek.Przychodzace, "X", "Status", new DateOnly(2032, 1, 2), null, null, null), "t");
        await svc.ZmienStatusAsync(p.Id, StatusPisma.Zalatwione, "kierownik");
        var wpisy = db.Historia.Where(h => h.PismoId == p.Id).OrderBy(h => h.Id).ToList();
        Assert.Equal(2, wpisy.Count);
        Assert.Contains("Załatwione", wpisy[1].Opis);
        Assert.Equal("kierownik", wpisy[1].Kto);
    }

    [Fact]
    public async Task Api_bez_logowania_zwraca_401()
    {
        var r = await Klient().GetAsync("/api/pisma");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Pracownik_nie_moze_rejestrowac_pism()
    {
        var r = await Klient("p@demo.example", "Pracownik").PostAsJsonAsync("/api/pisma",
            new { kierunek = "Przychodzace", kontrahent = "X", przedmiot = "Y", dataPisma = "2026-11-25" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Kancelaria_rejestruje_pismo_przez_api_i_widzi_je_na_liscie()
    {
        var c = Klient("k@demo.example", "Kancelaria,Pracownik");
        var r = await c.PostAsJsonAsync("/api/pisma", new { kierunek = "Przychodzace", kontrahent = "Firma Testowa (fikcyjna)", przedmiot = "Wniosek testowy API", dataPisma = "2033-05-05", znakSprawy = "OR.0001.1.2033" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("KP/2033/00001", dto.GetProperty("numer").GetString());

        var lista = await c.GetFromJsonAsync<JsonElement>("/api/pisma?q=Wniosek%20testowy%20API", Json);
        Assert.Equal(1, lista.GetProperty("razem").GetInt32());
    }

    [Fact]
    public async Task Walidacja_odrzuca_puste_pola()
    {
        var r = await Klient("k@demo.example", "Kancelaria").PostAsJsonAsync("/api/pisma", new { kierunek = "Przychodzace", kontrahent = "", przedmiot = "", dataPisma = "2026-11-25" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Strona_glowna_ma_baner_demo_i_noindex()
    {
        var r = await Klient().GetAsync("/");
        r.EnsureSuccessStatusCode();
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("DEMO", html);
        Assert.Contains("Isvero Studio", html);
        Assert.Equal("noindex, nofollow", r.Headers.GetValues("X-Robots-Tag").Single());
    }

    [Fact]
    public async Task Rejestr_wymaga_logowania()
    {
        var r = await Klient().GetAsync("/Pisma");
        Assert.True(r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Health_check_dziala()
    {
        var r = await Klient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Brak_strony_daje_404_z_pelnym_ukladem()
    {
        var r = await Klient().GetAsync("/strona-ktorej-nie-ma");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("lang=\"pl\"", html);
        Assert.Contains("<title>Nie znaleziono strony", html);
        Assert.Contains("DEMO", html);
    }

    [Fact]
    public async Task Api_zwraca_sam_kod_404()
    {
        var r = await Klient("kancelaria@demo.example", "Kancelaria").GetAsync("/api/pisma/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.DoesNotContain("<html", await r.Content.ReadAsStringAsync());
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

namespace RejestrPism.Web.Pages.Pisma;

public class IndexModel(RejestrService svc) : PageModel
{
    const int Rozmiar = 20;
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public Kierunek? Kierunek { get; set; }
    [BindProperty(SupportsGet = true)] public StatusPisma? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int Strona { get; set; } = 1;
    public List<Pismo> Pozycje { get; private set; } = new();
    public int Razem, Stron;

    public async Task OnGetAsync()
    {
        var q = svc.Szukaj(Q, Status, Kierunek);
        Razem = await q.CountAsync();
        Stron = Math.Max(1, (int)Math.Ceiling(Razem / (double)Rozmiar));
        Strona = Math.Clamp(Strona, 1, Stron);
        Pozycje = await q.Skip((Strona - 1) * Rozmiar).Take(Rozmiar).ToListAsync();
    }
}

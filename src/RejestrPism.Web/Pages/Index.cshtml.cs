using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;

namespace RejestrPism.Web.Pages;

public class IndexModel(AppDb db) : PageModel
{
    public int Razem, WRealizacji, PoTerminie;
    public async Task OnGetAsync()
    {
        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);
        Razem = await db.Pisma.CountAsync();
        WRealizacji = await db.Pisma.CountAsync(p => p.Status == StatusPisma.WRealizacji);
        PoTerminie = await db.Pisma.CountAsync(p => p.Termin != null && p.Termin < dzis && p.Status != StatusPisma.Zalatwione && p.Status != StatusPisma.Archiwum);
    }
}

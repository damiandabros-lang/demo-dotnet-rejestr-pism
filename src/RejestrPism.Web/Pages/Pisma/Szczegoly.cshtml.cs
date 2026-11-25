using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

namespace RejestrPism.Web.Pages.Pisma;

public class SzczegolyModel(AppDb db, RejestrService svc) : PageModel
{
    public Pismo Pismo { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await db.Pisma.Include(x => x.Historia).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        Pismo = p; return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, StatusPisma status)
    {
        if (!User.IsInRole(Seed.RolaPracownik)) return Forbid();
        if (await svc.ZmienStatusAsync(id, status, User.Identity!.Name!) is null) return NotFound();
        TempData["Info"] = "Zapisano status.";
        return RedirectToPage(new { id });
    }
}

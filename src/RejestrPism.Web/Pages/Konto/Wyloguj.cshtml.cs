using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RejestrPism.Web.Pages.Konto;

public class WylogujModel(SignInManager<IdentityUser> signIn) : PageModel
{
    public IActionResult OnGet() => Redirect("/");
    public async Task<IActionResult> OnPostAsync() { await signIn.SignOutAsync(); return Redirect("/"); }
}

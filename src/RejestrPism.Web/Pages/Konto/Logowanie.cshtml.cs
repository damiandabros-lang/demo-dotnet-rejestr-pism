using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RejestrPism.Web.Pages.Konto;

public class LogowanieModel(SignInManager<IdentityUser> signIn) : PageModel
{
    public string? Blad { get; private set; }
    public string? Email { get; private set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string haslo)
    {
        Email = email;
        var r = await signIn.PasswordSignInAsync(email ?? "", haslo ?? "", isPersistent: false, lockoutOnFailure: true);
        if (r.Succeeded) return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Pisma");
        Blad = r.IsLockedOut ? "Konto zostało czasowo zablokowane po kilku nieudanych próbach." : "Nieprawidłowy e-mail lub hasło.";
        return Page();
    }
}

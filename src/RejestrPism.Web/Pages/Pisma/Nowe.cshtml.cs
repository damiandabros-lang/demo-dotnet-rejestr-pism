using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RejestrPism.Web.Data;
using RejestrPism.Web.Services;

namespace RejestrPism.Web.Pages.Pisma;

[Authorize(Roles = Seed.RolaKancelaria)]
public class NoweModel(RejestrService svc) : PageModel
{
    public class Formularz
    {
        [Required(ErrorMessage = "Wybierz kierunek pisma.")] public Kierunek? Kierunek { get; set; }
        [Required(ErrorMessage = "Podaj nadawcę lub adresata."), MaxLength(200)] public string? Kontrahent { get; set; }
        [Required(ErrorMessage = "Podaj przedmiot pisma."), MaxLength(300)] public string? Przedmiot { get; set; }
        [Required(ErrorMessage = "Podaj datę pisma.")] public DateOnly? DataPisma { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [MaxLength(40), RegularExpression(@"^[A-ZĄĆĘŁŃÓŚŹŻ]{1,5}\.\d{1,4}(\.\d{1,6})*\.\d{4}$", ErrorMessage = "Znak sprawy w formacie np. OR.0001.1.2026.")] public string? ZnakSprawy { get; set; }
        [MaxLength(100)] public string? Komorka { get; set; }
        public DateOnly? Termin { get; set; }
    }
    [BindProperty] public Formularz Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var p = await svc.ZarejestrujAsync(new NowePismo(Input.Kierunek!.Value, Input.Kontrahent!, Input.Przedmiot!, Input.DataPisma!.Value, Input.ZnakSprawy, Input.Komorka, Input.Termin), User.Identity!.Name!);
        TempData["Info"] = $"Zarejestrowano pismo {p.Numer}.";
        return RedirectToPage("Szczegoly", new { id = p.Id });
    }
}

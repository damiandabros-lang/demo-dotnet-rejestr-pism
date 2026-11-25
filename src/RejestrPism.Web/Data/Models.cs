using System.ComponentModel.DataAnnotations;

namespace RejestrPism.Web.Data;

public enum Kierunek { Przychodzace = 1, Wychodzace = 2 }
public enum StatusPisma { Zarejestrowane = 1, Przekazane = 2, WRealizacji = 3, Zalatwione = 4, Archiwum = 5 }

/// <summary>Pismo w rejestrze korespondencji (dane demonstracyjne, fikcyjne).</summary>
public class Pismo
{
    public int Id { get; set; }

    /// <summary>Numer kancelaryjny, np. KP/2026/00012 (przychodzące) lub KW/2026/00003 (wychodzące).</summary>
    [MaxLength(20)] public string Numer { get; set; } = "";
    public int Rok { get; set; }
    public int Kolejny { get; set; }
    public Kierunek Kierunek { get; set; }

    [Required(ErrorMessage = "Podaj nadawcę lub adresata."), MaxLength(200)]
    [Display(Name = "Nadawca / adresat")]
    public string Kontrahent { get; set; } = "";

    [Required(ErrorMessage = "Podaj przedmiot pisma."), MaxLength(300)]
    [Display(Name = "Przedmiot")]
    public string Przedmiot { get; set; } = "";

    [MaxLength(40), Display(Name = "Znak sprawy (JRWA)")]
    public string? ZnakSprawy { get; set; }

    [MaxLength(100), Display(Name = "Komórka / referat")]
    public string? Komorka { get; set; }

    [Display(Name = "Data wpływu / wysłania")]
    public DateOnly DataPisma { get; set; }

    [Display(Name = "Termin załatwienia")]
    public DateOnly? Termin { get; set; }

    public StatusPisma Status { get; set; } = StatusPisma.Zarejestrowane;
    public DateTimeOffset Utworzono { get; set; }
    [MaxLength(100)] public string UtworzylLogin { get; set; } = "";

    public List<WpisHistorii> Historia { get; set; } = new();
}

/// <summary>Ślad audytowy zmian pisma.</summary>
public class WpisHistorii
{
    public int Id { get; set; }
    public int PismoId { get; set; }
    public Pismo? Pismo { get; set; }
    public DateTimeOffset Kiedy { get; set; }
    [MaxLength(100)] public string Kto { get; set; } = "";
    [MaxLength(300)] public string Opis { get; set; } = "";
}

public static class Opisy
{
    public static string Nazwa(this StatusPisma s) => s switch
    {
        StatusPisma.Zarejestrowane => "Zarejestrowane",
        StatusPisma.Przekazane => "Przekazane do komórki",
        StatusPisma.WRealizacji => "W realizacji",
        StatusPisma.Zalatwione => "Załatwione",
        StatusPisma.Archiwum => "W archiwum",
        _ => s.ToString()
    };
    public static string Nazwa(this Kierunek k) => k == Kierunek.Przychodzace ? "Przychodzące" : "Wychodzące";
}

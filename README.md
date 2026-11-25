# DEMO: rejestr korespondencji – ASP.NET Core 8 + MS SQL Server

> **Projekt pokazowy (portfolio) Isvero Studio, listopad 2026 r.** Nie jest to system żadnej instytucji ani wdrożenie dla klienta. Wszystkie dane, osoby i urzędy są fikcyjne.

Prosty „EZD-lite”: rejestr pism przychodzących i wychodzących dla urzędu.

## Funkcje
- automatyczna numeracja kancelaryjna `KP/RRRR/NNNNN` i `KW/RRRR/NNNNN` (unikalny indeks + ponowienie przy kolizji),
- znaki spraw JRWA (walidacja formatu), komórki, terminy, statusy, **pełna historia zmian (audyt)**,
- wyszukiwanie, filtry, stronicowanie, pulpit z licznikami (w tym „po terminie”),
- **ASP.NET Core Identity**: logowanie, role `Kancelaria` (rejestracja) i `Pracownik` (podgląd, statusy), blokada po 5 nieudanych próbach,
- **REST API** (Minimal API) + dokumentacja OpenAPI/Swagger pod `/swagger`, 401/403 dla API zamiast przekierowań,
- health check `/health`, nagłówki bezpieczeństwa i CSP, `noindex`,
- interfejs projektowany pod WCAG 2.1 AA (skip link, fokus, etykiety, podsumowanie błędów, dostępne tabele).

## Technologia
C# 12, .NET 8, ASP.NET Core (Razor Pages + Minimal API), Entity Framework Core 8 z migracjami, Microsoft SQL Server 2022 (Express), SQLite tylko do uruchomień deweloperskich i testów, xUnit + `WebApplicationFactory` (9 testów integracyjnych), Docker (testy uruchamiane w trakcie budowania obrazu), Docker Compose / Coolify.

## Uruchomienie
```bash
# lokalnie bez SQL Server (SQLite):
Demo__Password='Haslo-Demo-123!' dotnet run --project src/RejestrPism.Web
# z MS SQL Server:
SA_PASSWORD='Silne-Haslo-2026!' DEMO_PASSWORD='Haslo-Demo-123!' docker compose up --build
dotnet test
```
Konta demo: `kancelaria@demo.example`, `pracownik@demo.example` (hasło z `DEMO_PASSWORD`).

## API (przykład)
```
GET  /api/pisma?q=wniosek&status=WRealizacji&strona=1&rozmiar=20
GET  /api/pisma/{id}
POST /api/pisma            (rola Kancelaria)
POST /api/pisma/{id}/status
```

## Wymagania serwera
SQL Server 2022 potrzebuje ok. 2 GB RAM (limit w `docker-compose.yml`: 2 GB, `MSSQL_MEMORY_LIMIT_MB=1536`).

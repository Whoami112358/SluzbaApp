# System Zarządzania Służbami

## Opis projektu
System Zarządzania Służbami to aplikacja internetowa mająca na celu automatyzację i usprawnienie procesu zarządzania służbami w jednostkach wojskowych. System umożliwia dowódcom jednostek wojskowych efektywne zarządzanie harmonogramem służb oraz komunikację z podwładnymi. Aplikacja zapewnia intuicyjny interfejs, wysoką dostępność oraz zgodność z wymaganiami bezpieczeństwa.

---

## Funkcjonalności

### Zrealizowane w Sprint 1:
- **Rejestracja użytkowników**: możliwość dodawania nowych kont użytkowników przez administratora.
- **Logowanie i uwierzytelnianie**: zapewnienie dostępu do systemu dla zarejestrowanych użytkowników.
- **Nawigacja po systemie**: intuicyjny dostęp do głównych funkcji aplikacji.
- **Harmonogram służb**: przegląd harmonogramu przez żołnierzy.
- **Kontrola służb**: oficer dyżurny może sprawdzić listę żołnierzy aktualnie pełniących służbę.

### Plany na kolejne sprinty:
- **Powiadomienia i przypomnienia o służbach.**
- **Raportowanie i statystyki dotyczące służb.**
- **Zaawansowane filtrowanie harmonogramu.**

---

## Wymagania systemowe

- **Frontend**: CSHTML
- **Backend**: .NET Core
- **Baza danych**: AWS DB (technologia chmurowa)
- **Przeglądarki**: Chrome, Firefox, Safari itd.

---

## Instalacja i uruchomienie

### 1. Klonowanie repozytorium

```bash
git clone https://github.com/Whoami112358/SluzbaApp.git
cd SluzbaApp
```

### 2. Konfiguracja środowiska

1. Zainstaluj wymagane narzędzia:
   - .NET Core SDK
2. Skonfiguruj połączenie z bazą danych w pliku `appsettings.json`:
   ```json
   {
       "ConnectionStrings": {
           "DefaultConnection": "<connection_string>"
       }
   }
   ```

### 3. Budowanie i uruchamianie projektu

#### Backend:
```bash
 dotnet restore
 dotnet build
 dotnet run
```

Aplikacja powinna być dostępna pod adresem: `http://localhost:5111`.

---

## Testy jednostkowe

Testy jednostkowe zostały przygotowane w technologii NUnit.

Aby uruchomić testy, wykonaj:
```bash
cd TestProject
 dotnet test
```

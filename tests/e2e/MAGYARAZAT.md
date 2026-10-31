# Magyarázat – E2E- és API-tesztek (neked, interjúra készüléshez)

Ez a mappa (`tests/e2e`) a böngészős és API-teszteket tartalmazza. A projekt többi része a fejlesztői tudásodat mutatja,
ez pedig azt, hogy a saját alkalmazásodat végig is tudod tesztelni. Fejlesztői interjún ez jó pont. A README angolul
van a cégeknek, ez a fájl neked szól.

## Mi van benne, egy mondatban mindegyik

| Rész | Mi ez | Miért jó a CV-n |
|---|---|---|
| `Tests/UI` | Playwright tesztek C#-ban: egy robot végigkattintja a weboldalt egy igazi böngészőben | A „Playwright”, „Selenium”, „E2E” szavak szinte minden tesztelői hirdetésben benne vannak |
| `Pages/` | Page Object minta: minden oldalnak van egy osztálya, ami tudja, hol vannak a gombok | Interjún szinte mindig rákérdeznek |
| `Tests/Api` | API tesztek: nincs böngésző, közvetlenül HTTP kéréseket küldünk | Gyorsabb és stabilabb, mint a UI teszt |
| `postman/` | Ugyanez Postmanben | Sok cég Postmant használ, a hirdetésekben gyakran szerepel |
| `docs/TEST_PLAN.md` | Tesztterv: mit, hogyan, milyen szinteken tesztelünk | Azt mutatja, hogy tervezni is tudsz, nem csak kattintani |
| `docs/TEST_CASES.md` | 33 teszteset (ebből 4 felfedező tesztelési cél), mindegyiknél a tesztelési technika | Ez az ISTQB anyag a gyakorlatban |
| `docs/BUG_REPORTS.md` | 3 hibajegy (egy javítva, kettő nyitott) | Jól megírt bugriportot kevés kezdő tud mutatni |

## Hogyan futtasd

1. A repó gyökeréből: `web` mappa → `npm install` → `npm run build:api`, aztán `src/ServiceBooking.Api` → `dotnet run`.
   Ekkor a teljes app fut a http://localhost:5080 címen.
2. Új terminálban, a `tests/e2e` mappában:
   ```
   dotnet build ServiceBooking.E2E
   pwsh ServiceBooking.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
   dotnet test ServiceBooking.E2E --settings e2e.runsettings
   ```
   A `pwsh` a PowerShell 7. Ha nincs meg: `winget install Microsoft.PowerShell`.
3. Ha látni akarod, ahogy a robot kattintgat: az `e2e.runsettings` fájlban `<Headless>false</Headless>`.
   Ezt érdemes egyszer kipróbálni, és jó demó egy interjún is.

**Postman:** Import → válaszd ki a `postman/szerviz-foglalo.postman_collection.json` fájlt → a kollekción
„Run collection”. Sorrendben kell futtatni, mert a kérések egymás eredményeit használják (pl. az új foglalás azonosítóját).

## A legfontosabb ötletek, amiket érdemes elmondani

**1. „A teszteket az API-n keresztül készítem elő, a böngészőben csak azt nézem, amit tesztelek.”**
A lemondás tesztjénél a foglalást nem kattintgatva hozom létre, hanem egy HTTP kéréssel. Így gyorsabb, és ha a
foglalóoldal elromlik, attól még nem bukik el a lemondás tesztje is.

**2. „Nincs beégetett dátum.”** Minden teszt megkérdezi az API-tól, mikor van szabad időpont, és véletlen rendszámot
használ. Ezért a tesztek egymástól függetlenek, és akkor is működnek, ha az adatbázisban már sok foglalás van.

**3. „Szerep és felirat alapján keresem az elemeket, nem CSS osztály alapján.”** `GetByRole(AriaRole.Button, "Időpont foglalása")`:
így keresi egy felhasználó vagy egy képernyőolvasó is. Ha a dizájner átírja a CSS-t, a teszt nem törik el.

**4. „Nincs `Thread.Sleep`.”** A Playwright `Expect` magától vár, amíg teljesül a feltétel. Ahol az oldal adatot tölt
újra (napváltás), ott megvárom a pontos hálózati választ (`RunAndWaitForResponseAsync`). Enélkül a teszt néha az
előző nap időpontjait nézné, vagyis flaky lenne.

**5. „Ha egy teszt elbukik, marad nyoma.”** Az `E2ETestBase` minden tesztről trace-t rögzít, és bukáskor elmenti:
képernyőképek, DOM, hálózati forgalom lépésenként. A CI ezt feltölti, így a hibát meg lehet nézni anélkül, hogy újra
kellene futtatni.

**6. A tesztpiramis.** Sok gyors unit teszt (`tests/ServiceBooking.Tests`), kevesebb integrációs és API teszt, és csak néhány lassú
böngészős teszt a fő folyamatokra. Ha egy interjún megkérdezik, miért nem tesztelsz mindent a böngészőben: mert lassú
és törékeny, a szabályokat olcsóbb lent tesztelni.

## Valószínű interjúkérdések, és mit válaszolhatsz

**„Mi a különbség a severity és a priority között?”**
A severity azt mutatja, mekkora a hiba hatása (pl. dupla foglalás = Major). A priority azt, mennyire sürgős javítani.
Egy elírás a cégnévben a főoldalon kis severity, de nagy priority.

**„Mondj egy hibát, amit találtál.”** → BUG-001: egy elgépelt API cím 200 OK-t és egy HTML oldalt adott vissza 404
helyett. Ez a monitoringot is félrevezeti. Megtaláltam, leírtam, javítottam, és írtam rá regressziós tesztet.

**„Mi az a határérték-elemzés? Mondj példát.”** Legalább 2 órával előre lehet foglalni. Ha most 10:00 van, akkor a
12:00 még jó, a 11:45 már nem. Mindkét oldalt tesztelem, mert a hibák jellemzően a határon vannak (`<` vagy `<=`).

**„Mi az a döntési tábla?”** → A lemondásnál négy eset van (TEST_PLAN.md, 3. pont): rossz e-mail, már nem foglalt
állapot, 24 órán belül, és a sikeres eset. Mindegyikre van teszt.

**„Mi az a Page Object?”** Minden oldalnak van egy osztálya, ami tudja, hogyan kell az oldalon dolgozni. A teszt csak
annyit mond: `booking.ChooseServiceAsync("Olajcsere")`. Ha változik az oldal, csak egy helyen kell javítani.

**„Mit tennél, ha egy teszt néha elbukik, néha nem (flaky)?”** Megnézem a trace-t, és megkeresem, mire nem vár
rendesen. Gyakran időzítés vagy közös tesztadat okozza. Erre jó példa a `ChooseDayAsync`, ahol a hálózati választ várom
meg, és hogy minden teszt saját adatot használ.

**„Hogyan tesztelnéd a BUG-002-t (két egyszerre érkező foglalás)?”** Két párhuzamos HTTP kéréssel
(`Task.WhenAll`) vagy egy terheléses eszközzel (k6, JMeter), sokszor ismételve, mert ritkán jön elő.

## Őszintén, ha rákérdeznek

Ha megkérdezik, hogyan készült: AI segítségével raktad össze, de minden részét érted és el tudod magyarázni.
Ma ez teljesen elfogadott, a kérdés az, hogy érted-e. Ezért nézd át a fájlokat egyesével, és futtasd le őket.

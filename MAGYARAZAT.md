# Magyarázat – szerviz-foglalo (neked, interjúra készüléshez)

A README angolul van a cégeknek. Ez a fájl neked szól: mi hol van, mi miért van úgy, és mit mondj, ha kérdeznek.
Javaslom, hogy egyszer olvasd végig nyitott kóddal, és minden fájlt nyiss meg, amiről szó van.

## Mi ez az app, 20 másodpercben (így mutasd be)

„Egy autószerviz időpontfoglaló rendszere. Az ügyfél kiválasztja a munkát, a napot és egy szabad időpontot. A szervizben
két emelő van, a rendszer kiszámolja, mikor van szabad emelő a munka teljes idejére. A szerelők egy napi táblán látják
a munkákat emelőnként, és ott állítják az állapotukat. A backend ASP.NET Core Web API C#-ban, Entity Framework Core-ral
és SQLite-tal. A frontend React és TypeScript. Az üzleti szabályokra unit tesztek, a teljes API-ra integrációs tesztek
vannak, és a GitHub Actions minden pushnál lefuttatja őket.”

## Hogyan indítsd el

1. Telepítsd: **.NET 10 SDK** és **Node.js 22**. A Visual Studio 2026 Community a .NET 10-et is felteszi, de
   VS Code-dal is megy.
2. Backend: `src/ServiceBooking.Api` mappában `dotnet run` → http://localhost:5080/swagger (itt kipróbálhatod az API-t).
3. Frontend: `web` mappában `npm install`, majd `npm run dev` → http://localhost:5173
4. Tesztek: a repó gyökerében `dotnet test`, a `web` mappában `npm test`.
5. Műhely nézet: a weboldalon a „Műhely” menü, a kulcs fejlesztéskor `dev-admin-key`.

**Fontos:** ezt a C# kódot nem tudtam lefordítani ott, ahol írtam, mert nem volt elérhető a .NET. A React részt
viszont végigteszteltem. Ha a `dotnet build` vagy a `dotnet test` hibát ír, másold be nekem a hibaüzenetet,
és kijavítom. Ugyanígy, ha GitHubra feltöltés után az Actions fülön piros lesz valami.

## A fájlok, sorrendben, ahogy egy kérés végigmegy rajtuk

Vegyünk egy foglalást (`POST /api/bookings`):

1. **`Program.cs`**: itt áll össze az app. Regisztrálja a szolgáltatásokat (Dependency Injection), az adatbázist, a
   Swaggert, és induláskor létrehozza az adatbázist a kezdő adatokkal (`SeedData`).
2. **`Controllers/BookingsController.cs`**: fogadja a HTTP kérést, és rögtön továbbadja a `BookingService`-nek. A
   controller szándékosan „vékony”: nincs benne üzleti logika.
3. **`Services/BookingService.cs`**: itt van minden szabály. Ez a legfontosabb fájl, ezt értsd a legjobban.
   - `ValidateCustomerData`: név, e-mail, telefonszám, rendszám. Az összes hibát egyszerre gyűjti össze, nem áll meg
     az elsőnél, így a felhasználó egyszerre látja mindet.
   - `CheckTimeRules`: 15 perces lépésköz, hétvége, nyitvatartás, legalább 2 órával előre, legfeljebb 60 nappal előre.
   - `FindFreeBay`: megnézi, melyik emelőt használják az átfedő foglalások, és visszaadja az első szabadot.
   - `Overlaps`: két időszak akkor fedi egymást, ha mindkettő a másik vége előtt kezdődik. Ezért a 9:00–10:00 és a
     10:00–11:00 **nem** ütközik. Erre külön teszt is van.
4. Ha szabály sérül, a service **`BookingException`**-t dob. A **`Filters/BookingExceptionFilter.cs`** egy helyen
   fordítja ezt HTTP kóddá (400, 404, 409, 422). Így a controllerekben nincs try/catch.
5. **`Data/AppDbContext.cs`**: az Entity Framework Core „térképe”: melyik C# osztály melyik táblának felel meg.
6. **`Contracts/`**: amit a kliens küld (Request) és amit visszakap (DTO). Az adatbázis-osztályokat (`Domain/`) nem
   adjuk ki közvetlenül, így az adatbázis szerkezete szabadon változhat.

További fájlok:
- **`Services/IClock.cs`**: a „mennyi az idő?” kérdés egy interfész mögött van. Élesben a valódi idő (Budapest),
  tesztben egy fix időpont (hétfő 10:00). Enélkül a „legalább 2 órával előre” szabályt nem lehetne pontosan tesztelni.
- **`Filters/AdminKeyAttribute.cs`**: a műhely végpontjai csak `X-Admin-Key` fejléccel érhetők el. Ha nincs kulcs
  beállítva, senki nem jut be (biztonságos alapértelmezés).
- **`Services/LicensePlateRules.cs`**: régi (ABC-123) és új (AA AA-123) magyar rendszám. Ugyanez a logika megvan a
  frontendben is (`web/src/format.ts`).

## A tesztek

- **`BookingServiceTests.cs`**: unit tesztek. Minden teszt saját, memóriában futó SQLite adatbázist kap. Figyeld meg a
  **határérték** teszteket (`Create_ExactlyTwoHoursAhead_IsAllowed`, `Create_EndingExactlyAtClosing_IsAllowed`,
  `Create_BackToBackBookings_DoNotConflict`). Ezek pont az ISTQB határérték-elemzés gyakorlati példái.
- **`ApiTests.cs`**: integrációs tesztek. A `WebApplicationFactory` a memóriában elindítja az egész appot, és valódi
  HTTP kéréseket küld neki. Ezek azt nézik, hogy a részek együtt is működnek-e (routing, JSON, szűrők, adatbázis).
- **`web/src/**/*.test.ts(x)`**: frontend tesztek Vitesttel. A `BookingPage.test.tsx` végigkattintja a foglalást egy
  hamis (mock) API-val.
- **`.github/workflows/ci.yml`**: minden pushnál lefut a build és az összes teszt. Ha zöld pipa van a GitHubon, működik.

## Valószínű interjúkérdések

**„Miért van külön a controller és a service?”** A controller csak a HTTP-vel foglalkozik, a service az üzleti
szabályokkal. Így a szabályokat HTTP nélkül, gyorsan lehet tesztelni, és ha holnap kellene egy mobilapp vagy egy
asztali kliens, ugyanazt a service-t használhatná.

**„Mi az a Dependency Injection?”** Az osztály nem maga hozza létre, amire szüksége van, hanem a konstruktorában
megkapja (pl. a `BookingService` az `AppDbContext`-et és az `IClock`-ot). A `Program.cs`-ben van megadva, mi micsoda.
Ezért tudok a tesztben valódi óra helyett `FakeClock`-ot adni neki.

**„Mi a különbség a 400, 404, 409 és 422 között?”** 400: rossz a bemenet. 404: nincs ilyen. 409: ütközés, az időpont
foglalt. 422: a kérés formailag jó, de az adott állapotban nem végrehajtható (pl. kész munkát nem lehet újraindítani).

**„Mi történik, ha két ember egyszerre foglalja le az utolsó szabad helyet?”** Jó kérdés, és ismerem a választ:
mindkét kérés láthatja szabadnak a helyet, mielőtt bármelyik elmentené. Ez egy versenyhelyzet (race condition). Le is
írtam hibajegyként (BUG-002 az e2e repóban). Javítani tranzakcióval vagy zárolással lehet. Ha ezt magadtól elmondod, az
nagyon jó benyomást kelt.

**„Miért SQLite?”** Egy demóhoz nem kell külön adatbázis-szerver. Az EF Core miatt PostgreSQL-re vagy SQL Serverre
cserélni csak a provider és a connection string átírása.

**„Miért `EnsureCreated` és nem migration?”** Demóhoz egyszerűbb. Valódi projektben migration kell, hogy az adatbázis
szerkezetét verziózni lehessen. Ez a README-ben is benne van a továbbfejlesztési listában.

**„Hogyan védted az admin részt?”** Egy kulccsal a fejlécben, amit fix idejű összehasonlítással ellenőrzök
(`CryptographicOperations.FixedTimeEquals`), hogy válaszidőből ne lehessen kitalálni. Valódi rendszerben
felhasználói fiókok kellenének (Identity/JWT). Ezt is tudom, és le is írtam.

**„Miért nem árulja el a lemondás, hogy létezik-e a foglalás?”** Ha a „nincs ilyen foglalás” és a „rossz e-mail”
másképp nézne ki, sorban ki lehetne próbálni a számokat, és kideríteni, mely foglalások léteznek. Ezért mindkettőre
ugyanaz a válasz.

## Őszintén, ha rákérdeznek

„AI-jal dolgozom, ezt a projektet is AI segítségével raktam össze, de minden részét átnéztem, lefuttattam, és el tudom
magyarázni.” Ma ez elfogadott, a kérdés az, hogy érted-e. Ezért **mielőtt beírod a CV-be, menj végig ezen a fájlon**,
és próbálj ki egy kis módosítást magad is. Például legyen három emelő: `appsettings.json` → `"BayCount": 3`. Nézd meg,
mi változik, és melyik teszt bukik el. Így tényleg a tiéd lesz.

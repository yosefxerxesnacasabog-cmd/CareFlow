# CareFlow — Diagnostic Booking Management

**ITSD81 Performance Task 4 · BSIT3Cx**

- Yosef Xerxes S. Nacasabog
- Jazz Dwight W. Salac

CareFlow is a Windows desktop application for staff who schedule diagnostic services. Staff can check the remaining slots for a date, book one to three distinct services for an existing patient, and look up confirmed bookings.

## Prerequisites

- Windows and the **.NET 10 SDK**. Run `dotnet --list-sdks` to check that a 10.x SDK is installed.
- PowerShell opened in the project folder, where `CareFlow.csproj` is located. Visual Studio is optional. To use it, open `CareFlow.csproj` as a project.

## First-time setup

```powershell
dotnet restore .\CareFlow.csproj
dotnet tool restore
dotnet build .\CareFlow.csproj --no-restore
dotnet ef database update --no-build
dotnet run --project .\CareFlow.csproj --no-build
```

The EF Core migration creates `%LOCALAPPDATA%\CareFlow.db` and seeds three fictional patients plus four diagnostic services: Complete Blood Count, Urinalysis, Chest X-Ray, and ECG. The app and migration tool share this database path. SQLite runs locally with no database server or credentials to configure.

For later launches, use the last command above if the project is already built, or press **Ctrl+F5** with the project open in Visual Studio. After receiving schema changes, rebuild and rerun `dotnet ef database update --no-build`. If the app reports that it cannot load patients or services, check that the migration completed successfully, then reopen the app.

## Book and verify a sample appointment

1. On **Book services**, select a fictional patient and choose today's date or a future date.
2. Check **Remaining slots** and select one to three distinct services with available capacity.
3. Select **Confirm booking**. A successful confirmation displays a booking number and refreshes the slot counts. CareFlow rechecks capacity during confirmation and saves the booking and all selected services in one transaction.
4. Open **Booking history** and find the booking number. Confirm that the patient, date, and all selected services appear. Close and reopen CareFlow, then check the same booking again.

## Project structure

| Path | Responsibility |
| --- | --- |
| `Views/` and `ViewModels/` | Avalonia XAML screens, selection state, commands, and feedback |
| `Services/` | Patient lookup, availability, validation, confirmation transactions, and history |
| `Models/` and `Data/` | Entity models, SQLite configuration, EF Core context, and migrations |
| `Tests/CareFlow.AcceptanceTests/` | Repeatable checks for all six assignment acceptance scenarios |

The app uses C# and .NET 10, Avalonia XAML with MVVM, dependency injection, and EF Core with SQLite. See the [relational design](docs/relational-design.md) for the tables, keys, and relationships.

## Run the acceptance checks

```powershell
dotnet run --project .\Tests\CareFlow.AcceptanceTests\CareFlow.AcceptanceTests.csproj
```

The runner uses disposable SQLite databases and reports each result from AC-01 through AC-06. See the [verification checklist](docs/verification.md) for the recorded results and how each scenario was checked.

## AI assistance

Our team is responsible for the final design, implementation, and verification of CareFlow. We used ChatGPT as a supporting tool for coding, troubleshooting, test development, and documentation.

# CareFlow

CareFlow is a Windows desktop app for ITSD81 Performance Task 4. Staff can check diagnostic service capacity, confirm a booking for an existing patient, and look up confirmed bookings.

## Project members

Section **BSIT3Cx**

- Yosef Xerxes S. Nacasabog
- Jazz Dwight W. Salac

## Set up and run

Install the **.NET 10 SDK**. To use Visual Studio, open `CareFlow.csproj` as a project.

From PowerShell in the repository root, run:

```powershell
dotnet restore .\CareFlow.csproj
dotnet tool restore
dotnet build .\CareFlow.csproj --no-restore
dotnet ef database update --no-build
dotnet run --project .\CareFlow.csproj --no-build
```

The migration creates `%LOCALAPPDATA%\CareFlow.db` and seeds three fictional patients and four diagnostic services. The app and migration tool use the same database path. Run the migration again after pulling schema changes. For subsequent launches, run the app from Visual Studio or use the final command above if the project is already built.

On **Book services**, choose a patient, date, and one to three services. Select **Confirm booking** to save the request. Use **Booking history** to see the patient, date, and services for each confirmed booking.

## Verify the project

```powershell
dotnet run --project .\Tests\CareFlow.AcceptanceTests\CareFlow.AcceptanceTests.csproj
```

The runner applies migrations to disposable databases and reports each acceptance result. See the [verification checklist](docs/verification.md) and [database diagram](docs/relational-design.md).

## AI assistance

OpenAI Codex assisted with the Avalonia scaffold and UI, EF Core models and migration, booking transaction logic, acceptance test runner, and documentation. The project members are responsible for reviewing, testing, and explaining the submitted implementation.

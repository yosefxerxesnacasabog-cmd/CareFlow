# CareFlow verification checklist

**Final review date** 17 November 2026

The desktop project rebuilt with zero warnings and zero errors. EF Core reported no changes pending, and the initial migration applied to a fresh SQLite file. All six acceptance scenarios passed using disposable databases.

| Scenario | Result | Evidence |
| --- | --- | --- |
| AC-01 | Pass | Compared displayed capacity data with saved bookings on two dates and checked full capacity on a third date with no bookings. |
| AC-02 | Pass | Ran the booking form's confirmation command and checked that SQLite stored one booking with both selected services. Verified that availability and history refreshed. |
| AC-03 | Pass | Submitted an unknown patient, unknown service, past date, invalid service counts, duplicates, and a fully booked service. Checked database row counts after rejection. |
| AC-04 | Pass | Read availability while one slot remained, filled it through competing requests, and checked that a later request based on the earlier availability was rejected. |
| AC-05 | Pass | Used a SQLite trigger to fail the second service insert. Checked that the booking and its service links were absent and capacities were intact. |
| AC-06 | Pass | A test process saved a booking and exited. A separate process loaded the patient, date, and both services through the startup view model. A separate desktop smoke test opened and closed CareFlow's main window in two processes and verified the saved history row in the reopened window. |

Run the [acceptance runner](../Tests/CareFlow.AcceptanceTests/Program.cs) using the command in the [README](../README.md). The desktop restart smoke test used the actual Avalonia window with a disposable database. The automated runner checks restart persistence through the startup view model.

## Final requirements

- [x] Desktop source builds with .NET 10, Avalonia XAML, and MVVM.
- [x] Dependency injection supplies services and data access. Views and view models delegate database operations to services.
- [x] Staff can check capacity by date, confirm multi-service bookings, and read confirmed booking history.
- [x] BR-01 through BR-03 and BR-06 cover patient, service count, local date, and duplicate validation.
- [x] BR-04, BR-05, BR-07, and BR-08 are enforced by the capacity calculation and confirmation transaction.
- [x] BR-09 and BR-10 are covered by saved history and validation or database feedback.
- [x] SQLite persistence uses EF Core migrations, fictional seed records, and asynchronous service interfaces.
- [x] The repository includes setup instructions, the relational design, verification evidence, and an AI assistance acknowledgment.
- [x] The README identifies both project members and their section.

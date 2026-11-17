# CareFlow relational design

```mermaid
erDiagram
    PATIENTS ||--o{ BOOKINGS : has
    BOOKINGS ||--o{ BOOKING_SERVICES : includes
    DIAGNOSTIC_SERVICES ||--o{ BOOKING_SERVICES : appears_in

    PATIENTS {
        int Id PK
        string FullName
    }

    BOOKINGS {
        int Id PK
        int PatientId FK
        date BookingDate
    }

    DIAGNOSTIC_SERVICES {
        int Id PK
        string Name
        int DailyCapacity
    }

    BOOKING_SERVICES {
        int BookingId PK, FK
        int DiagnosticServiceId PK, FK
    }
```

`BookingServices` links `Bookings` to `DiagnosticServices`. Its composite primary key prevents a duplicate service within a booking. The database permits zero or more linked rows during writes. The confirmation service requires one to three distinct services before committing a booking.

Each service has one daily capacity value. Remaining slots for a date are calculated from the booking-service links for that date. Confirmation reserves SQLite's writer before rechecking capacity and saves the booking and all its links in one transaction.

See [the EF Core mappings](../Data/CareFlowDbContext.cs) and [the initial migration](../Data/Migrations/20261116133123_InitialCreate.cs).

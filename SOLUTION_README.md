# Parking API — Solution README

## Technology Stack

- **ASP.NET Core Web API** — .NET 8
- **Entity Framework Core InMemory** — used as a temporary in-memory data store for testing purposes; no external database setup is required
- **Swagger / Swashbuckle** — available in Development mode for interactive API exploration
- **xUnit + FluentAssertions** — unit test framework

---

## Project Structure

```
parking-app/
├── ParkingApi.slnx                     # Solution file
│
├── ParkingApi/                         # Main API project
│   ├── Program.cs                      # App entry point; DI registration and database seeding
│   ├── Controllers/
│   │   └── ParkingController.cs        # HTTP endpoints — delegates to IParkingService
│   ├── Data/
│   │   └── ParkingDbContext.cs         # EF Core DbContext
│   ├── IServices/
│   │   └── IParkingService.cs          # Service interface
│   ├── Services/
│   │   └── ParkingService.cs           # Business logic and charge calculation
│   ├── Models/
│   │   ├── Entities/
│   │   │   ├── ParkingLot.cs           # Parking lot entity
│   │   │   ├── ParkingSpace.cs         # Individual space entity (FK → ParkingLot)
│   │   │   └── Vehicle.cs              # Vehicle entity (reserved for future use)
│   │   ├── Enums/
│   │   │   └── VehicleType.cs          # Unknown = 0, Small = 1, Medium = 2, Large = 3
│   │   ├── Requests/
│   │   │   ├── ParkVehicleRequest.cs
│   │   │   └── ExitVehicleRequest.cs
│   │   └── Responses/
│   │       ├── ParkVehicleResponse.cs
│   │       ├── ParkingLotStatusResponse.cs
│   │       └── ExitVehicleResponse.cs
│   └── ParkingApi.http                 # HTTP request file for manual testing
│
└── ParkingApi.Tests/                   # Unit test project
    └── Services/
        └── ParkingServiceTests.cs      # 23 tests covering all service methods
```

---

## Data Models

The domain is modelled around three objects:

- **`ParkingLot`** — represents the car park. Holds `Name`, `Location`, and `Capacity`. Has a one-to-many relationship with `ParkingSpace`.
- **`ParkingSpace`** — represents an individual space. Holds `SpaceNumber`, `IsOccupied`, `VehicleReg`, `VehicleType`, and `TimeIn`. Has a foreign key (`ParkingLotId`) back to `ParkingLot`.
- **`Vehicle`** — created for potential future use (e.g. vehicle history, multiple concurrent registrations). Not wired into the current flow.

Relationship: `ParkingLot` → *(1 : many)* → `ParkingSpace`

---

## Database Initialisation

The application uses an **EF Core InMemory database**. There is no external database, no migrations, and no connection strings to configure.

Seeding is performed at startup inside **`Program.cs`**. On first run it creates:

- One `ParkingLot` (Name: _"Main Car Park"_, Location: _"Main Street"_, Capacity: **10**)
- Ten `ParkingSpace` records (SpaceNumber 1–10), each linked to the lot via `ParkingLotId`

Because the database is in-memory, **all data is reset every time the application restarts**.

---

## How to Run Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Steps

```bash
# 1. Clone / open the repository
cd parking-app

# 2. Run the API
cd ParkingApi
dotnet run
```

The API starts on:
- **HTTP**: `http://localhost:5137`
- **HTTPS**: `https://localhost:7294`

Swagger UI launches automatically in Development mode:

```
http://localhost:5137/swagger
```

---

## API Endpoints

### `POST /parking` — Park a vehicle

Parks a vehicle in the first available space (lowest space number).

**Request body**
```json
{
  "vehicleReg": "ABC123",
  "vehicleType": 1
}
```
`vehicleType`: `1` = Small, `2` = Medium, `3` = Large

**Response `201 Created`**
```json
{
  "vehicleReg": "ABC123",
  "spaceNumber": 1,
  "timeIn": "2026-06-28T10:00:00Z"
}
```

| Error | HTTP Status |
|---|---|
| Invalid / missing vehicle type or registration | `400 Bad Request` |
| Vehicle already parked / no available spaces | `409 Conflict` |

---

### `GET /parking` — Get parking lot status

Returns the current number of available and occupied spaces.

**Response `200 OK`**
```json
{
  "availableSpaces": 9,
  "occupiedSpaces": 1
}
```

---

### `POST /parking/exit` — Exit a vehicle

Frees the vehicle's space and returns the total parking charge.

**Request body**
```json
{
  "vehicleReg": "ABC123"
}
```

**Response `200 OK`**
```json
{
  "vehicleReg": "ABC123",
  "vehicleCharge": 3.00,
  "timeIn": "2026-06-28T10:00:00Z",
  "timeOut": "2026-06-28T10:10:00Z"
}
```

| Error | HTTP Status |
|---|---|
| Vehicle not found / not currently parked | `404 Not Found` |

---

## Charge Calculation

```
charge = (totalMinutes × rate) + floor(totalMinutes ÷ 5) × £1.00
```

| Vehicle Type | Rate per minute | Example (10 min) |
|---|---|---|
| Small (1) | £0.10 | £1.00 + £2.00 bonus = **£3.00** |
| Medium (2) | £0.20 | £2.00 + £2.00 bonus = **£4.00** |
| Large (3) | £0.40 | £4.00 + £2.00 bonus = **£6.00** |

The £1.00 bonus is added for every complete 5-minute interval.

---

## Manual Testing

The `ParkingApi.http` file in the `ParkingApi/` folder contains ready-made requests for all three endpoints and can be executed directly inside VS Code (with the REST Client extension) or Visual Studio.

---

## Running Unit Tests

```bash
# From the repo root
cd parking-app
dotnet test ParkingApi.slnx

# Or from the test project directly
cd ParkingApi.Tests
dotnet test
```

Tests are written with **xUnit** and use **FluentAssertions** for readable assertions (e.g. `result.VehicleReg.Should().Be("ABC123")`). FluentAssertions v8 is used under the free community licence (non-commercial use).

**23 tests** covering:
- `ParkVehicleAsync` — valid parking, space assignment, all validation error cases
- `GetParkingLotStatusAsync` — available/occupied counts, missing lot
- `ExitVehicleAsync` — exit flow, space reset, charge calculation across all vehicle types and bonus boundaries

---

## Exception Handling

Standard .NET exception types are used — no custom exception classes:

| Exception | Scenario | HTTP response |
|---|---|---|
| `ArgumentException` | Invalid vehicle type or empty registration | `400 Bad Request` |
| `InvalidOperationException` | Vehicle already parked, or no available spaces | `409 Conflict` |
| `KeyNotFoundException` | Vehicle not found on exit, or parking lot not found | `404 Not Found` |
| `Exception` (fallback) | Any unexpected error | `500 Internal Server Error` |

---

## Assumptions

- A single parking lot with exactly 10 spaces is seeded on startup.
- Spaces are allocated by lowest space number first.
- Parking time is measured in minutes using `DateTime.UtcNow`; sub-minute durations are charged proportionally.
- The £1 bonus applies per complete 5-minute block (e.g. 9 minutes = 1 bonus, 10 minutes = 2 bonuses).
- A vehicle registration is treated as a unique identifier — the same reg cannot occupy two spaces simultaneously.
- Vehicle type is validated strictly as 1, 2, or 3; any other value (including 0) is rejected.
- All data is held in-memory and does not persist between application restarts.
- **Authentication and authorisation are not required** — all endpoints are publicly accessible.

---

## Questions I Would Have Asked

- **Multiple parking lots** — should the API support more than one lot?
- **Partial minutes** — should a vehicle parked for 90 seconds be charged for 1 minute or 2 minutes? Currently charged proportionally (1.5 minutes).
- **Authentication/authorisation** — should any endpoints be restricted (e.g. only authorised staff can call `/parking/exit`)?
- **Architecture and design patterns** — are there preferred patterns to follow, such as CQRS, Mediator, Repository pattern, or Clean Architecture layering? Should the service layer be further separated into application and domain layers?
- **Model structure and relations** — should `ParkingSpace` hold vehicle data directly (current approach) or reference a separate `Vehicle` entity?
- **SQL schema design** — if moving to a persistent database (MSSQL / Postgres), what are the expectations around indexing, constraints, and naming conventions? Should parking history be stored in a separate `ParkingSession` table rather than on `ParkingSpace` itself, to preserve audit data?

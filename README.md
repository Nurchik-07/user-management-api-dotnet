# User Management API

An ASP.NET Core API for creating, reading, updating, and deleting user records.
It validates names and email addresses and logs requests using middleware.
Records are kept in memory and are lost when the application stops.
Email addresses must be unique, ignoring letter case.

## Copilot review

Microsoft Copilot reviewed the API and suggested checking for duplicate email
addresses. The API now rejects duplicates in both creation and updates, using
case-insensitive comparisons inside the existing lock. Copilot also suggested
testing whitespace-only names; the existing Required validation rejects them,
which was verified with an HTTP test.

## Run

With the .NET 8 SDK installed, run `dotnet run` from this directory.
Use the address displayed by the application in the examples below.

```sh
curl http://localhost:5000/users
curl -X POST http://localhost:5000/users -H 'Content-Type: application/json' -d '{"name":"Alex","email":"alex@example.com"}'
curl http://localhost:5000/users/1
curl -X PUT http://localhost:5000/users/1 -H 'Content-Type: application/json' -d '{"name":"Alex Smith","email":"alex@example.com"}'
curl -X DELETE http://localhost:5000/users/1
```

The port may differ; use the URL printed by `dotnet run`. This educational
sample uses in-memory storage and does not implement authentication.

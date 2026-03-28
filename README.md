# BakeryAutomation

<p align="center">
  <img src="BakeryAutomation/Resources/Images/logo.png" alt="BakeryAutomation logo" width="180" />
</p>

BakeryAutomation is a Windows desktop application built with WPF and SQLite for managing bakery operations in a single place. It covers products, customer accounts, shipments, returns, payments, and reporting.

## Features
- Products: create, update, delete, and track price history
- Branches / customer accounts: manage account cards, due days, credit limits, and branch-specific pricing
- Shipments: batch-based delivery entries, same-receipt returns, waste tracking, and product or batch discounts
- Returns: create separate return receipts for later returns, either linked to a shipment or entered independently
- Payments: record branch-based payments and validate remaining balances for linked receipts
- Reports: daily summary, account statement, carried balance, and CSV export
- Settings: configure data file location, create backups, and restore backups

## Tech Stack
- .NET 8
- WPF
- SQLite
- xUnit

## Getting Started
1. Open `BakeryAutomationApp.sln` in Visual Studio 2022 on Windows.
2. Select the `Release` build configuration.
3. Build and run the application.

## Publish
The project includes a publish profile for the standard release output:

```powershell
dotnet publish BakeryAutomation\BakeryAutomation.csproj -c Release /p:PublishProfile=WinX64
```

Default publish folder:
`BakeryAutomation\bin\Release\net8.0-windows\publish\win-x64\`

Pre-release checklist:
`docs/RELEASE_CHECKLIST.md`

## Data
The application stores its database in:
`%AppData%\BakeryAutomation\bakery.db`

Additional settings file:
`%AppData%\BakeryAutomation\settings.json`

## Notes
- SQLite is used as the local database.
- Currency fields are stored as `decimal`.
- Discount fields are stored as percentages (`%`).

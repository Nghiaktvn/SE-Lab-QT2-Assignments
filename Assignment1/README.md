# Smart Ticketing System

This is the separate Assignment 1 solution: a C# WinForms ticket vending machine prototype backed by SQL Server. It demonstrates destination selection, card or wallet payment simulation, ticket barcode generation, receipt output, and a SQL persistence schema.

## Run the demo

1. Install Visual Studio 2022 with the **.NET desktop development** workload and SQL Server Express/Developer.
2. Run `database/01_create_database.sql` in SQL Server Management Studio.
3. Open `SmartTicketing.sln` and set `SmartTicketing.WinForms` as the startup project.
4. In `src/SmartTicketing.WinForms/appsettings.json`, set `ConnectionStrings:Ticketing` for your SQL Server. Do not commit real credentials.
5. Build and run. Use the in-app **Demo mode** payment simulator (no live bank credentials or money are used).

SQL persistence is enabled by default for the assessed C#/MSSQL flow. The app does not issue a ticket if the configured database save fails. For an offline UI walkthrough only, set `Demo:PersistToSqlServer` to `false`; the success screen then clearly identifies a locally issued demo ticket. Payment integration is simulated and does not contact Momo, VNPay, ZaloPay, or a card processor. Both the wallet QR and ticket barcode are scannable QR images; the Print Preview button shows the paper ticket before printing.

## Contents

- `src/SmartTicketing.WinForms`: the runnable visual-programming prototype.
- `database`: SQL Server schema and realistic sample rows.
- `docs`: requirements, UML source, UI/architecture, report content, Copilot prompt log, and demo checklist.
- `readme.txt`: submission-oriented quick-start and collaboration checklist.

The separate Assignment 1 brief referenced by the template was not among the supplied files. The solution follows the supplied Assignment 1 report and workbook instructions; compare it with the missing brief before submission if that brief has additional requirements.

From the workspace root, run `Run-Demo.ps1 -App Test -ButtonTest` for both unit suites, SQL fixtures and native button audits. The Lab 1 audit saves `docs/evidence/tvm-button-audit.json` and uses offline transactions, so it adds no demo ticket to SQL. The local setup script disables SQL Express AUTO_CLOSE to avoid slow repeated database recovery.

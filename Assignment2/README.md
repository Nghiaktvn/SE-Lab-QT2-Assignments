# B2B Wholesale Order Management System

Assignment 2 is a separate .NET solution with a responsive ASP.NET Core Razor Pages agent portal (Option B in the brief), WinForms back-office app, SQL Server schema/seed script, and business-logic test project. The portal supports agent login, catalog, multi-item atomic bulk-order placement, order history, and print invoice.

## Run

1. Install Visual Studio 2022 with **ASP.NET and web development**, **.NET desktop development**, and **Data storage and processing** workloads; install SQL Server Express/Developer.
2. Run `database/01_create_database.sql` with SQL Server Management Studio.
3. Set `ConnectionStrings:Wholesale` in `src/AgentPortal/appsettings.json`. Keep real secrets out of source control.
4. Start `src/AgentPortal` (`dotnet run --project src/AgentPortal`). Open the printed localhost URL.
5. Admin desktop: open `WholesaleManagement.sln`, set `Wholesale.AdminWinForms` as startup, build and run.
6. Run the test project with `dotnet test tests/Wholesale.Tests`.

Demo accounts in the seed script: Admin desktop username `admin` and Agent portal email `agent01@wholesale.local`; the seeded demo password is `Demo123!`. Replace the demo password hashes and local configuration before sharing. This demo password is for local classroom use only. The SQL transaction flow requires SQL Server plus the Microsoft.Data.SqlClient package. In the catalog, enter positive quantities for two or more products and submit once to see a true master-detail order; all selected stock rows are checked and deducted in a single serializable transaction. On the resulting invoice, **Confirm payment** records a classroom-only `Paid` state, timestamp and unique `DEMO-...` reference. It does not contact a payment provider or move real money. Cancelling an eligible paid order marks its classroom payment `Refunded` and restores the reserved stock once.

## Contents

- `src/AgentPortal`: agent web portal.
- `src/Wholesale.AdminWinForms`: admin login, item/agent add-edit-deactivate screens, stock updates, order statuses, dashboard, manifest print preview.
- `src/Wholesale.Domain`: order calculations, stock rules, and lockout logic.
- `tests/Wholesale.Tests`: meaningful business-logic tests.
- `database`: normalized schema and 15+ rows per table.
- `docs`: requirements, architecture/ERD, testing, division-of-labor template, and report.

GitHub URL and actual team member contributions must be completed in `readme.txt` before submission. Branch/PR history must be created in the team's GitHub account; it cannot be fabricated locally.

For a full repeatable check from the workspace root, serve the portal, set `PORTAL_URL` if it is not on port 5400, then run `Run-Demo.ps1 -App Test -BrowserTest -ButtonTest`. This checks the web controls and two-product checkout in Chrome, the Admin login/dashboard buttons in WinForms, unit rules and isolated SQL mutations. Results are in `docs/evidence/`. The setup script turns SQL Express AUTO_CLOSE off; the default connection uses local shared memory. Set a different connection string when using a remote server.

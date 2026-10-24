NORTHSTAR WHOLESALE — ASSIGNMENT 2
Confirmed member: Phạm Trung Nghĩa — 524H0172
GitHub account: https://github.com/Nghiaktvn
Project repository: pending (an account profile is not a repository/PR record).
Group number, remaining 2–4 real members, actual feature ownership, contribution percentages: pending group confirmation.
Percentages must total 100% and match Report.docx and A2_Team_Workbook.xlsx.
Setup:
1. Install .NET SDK 10 with Windows Desktop and ASP.NET runtimes, and SQL Server.
2. Run database/01_create_database.sql in sqlcmd or SSMS. The script is repeatable and preserves current records.
3. Configure src/Wholesale.AdminWinForms/App.config and src/AgentPortal/appsettings.json.
4. dotnet build WholesaleManagement.sln
5. dotnet run --project src/Wholesale.AdminWinForms
6. dotnet run --project src/AgentPortal --urls http://127.0.0.1:5400
7. dotnet test tests/Wholesale.Tests
8. SQL integration checks: dotnet run --project tests/Wholesale.IntegrationTests -- <absolute Assignment2 directory>
9. Optional browser checkout check: run the Portal, then add --browser to step 8; install Playwright and Chrome first. This creates and removes a separate test agent, products and order.
Demo Admin: admin / Demo123!
Demo Agent: agent01@wholesale.local / Demo123!
These are dummy classroom credentials. No public deployment is included.
Portal config supports environment ConnectionStrings__Wholesale.
The SQL harness creates uniquely named fixtures and removes only its own fixtures in finally. Identity values can advance on rollback.
The shipped-revenue dashboard excludes pending and cancelled orders; cancellation restores reservations once only.
30-second MP4/GIF and product photographs are bundled locally; motion can be paused. Photograph licenses/sources are recorded in design documentation.
SourceCode.zip excludes bin, obj, TestResults and local Data Protection keys. Saved evidence is under docs/evidence.
Optional hosted video demo link: pending.

SMART TICKETING — ASSIGNMENT 1
Confirmed member: Phạm Trung Nghĩa — 524H0172
GitHub account: https://github.com/Nghiaktvn
Project repository: pending (an account profile is not a repository/PR record).
Group number, remaining 2–4 real members, actual feature ownership, contribution percentages: pending group confirmation.
The contribution percentages must total 100% and match the report and A1_Team_Workbook.xlsx.
Setup:
1. Install .NET SDK 10, Windows Desktop runtime and SQL Server.
2. Run database/01_create_database.sql using sqlcmd or SSMS. It is repeatable and preserves existing records.
3. Set ConnectionStrings:Ticketing in src/SmartTicketing.WinForms/appsettings.json, or environment ConnectionStrings__Ticketing.
4. dotnet build SmartTicketing.sln
5. dotnet run --project src/SmartTicketing.WinForms
6. dotnet test tests/SmartTicketing.Tests
7. dotnet run --project tools/ModelEngineering -- <absolute Assignment1 folder>
8. Optional SQL ticket checks: dotnet run --project tests/SmartTicketing.IntegrationTests -- <absolute Assignment1 folder>
All card, bank-QR, wallet, timeout, reconciliation and refund outcomes are simulations. No real payment occurs.
The 30-second MP4/GIF and original artwork are local assets. Pause animation in the kiosk when desired.
Actual Visual Studio model/code operations and Code Map screenshots still require capture on a supporting installed edition.
Report.docx is also supplied as 524H0172_PhamTrungNghia.docx.
SourceCode.zip excludes bin, obj, TestResults and local secrets; saved evidence is under docs/evidence.

# QT2 Software Engineering Lab — Complete Working Package
**Phạm Trung Nghĩa — 524H0172, Trần Gia Triều**

Project repository: [Nghiaktvn/SE-Lab-QT2-Assignments](https://github.com/Nghiaktvn/SE-Lab-QT2-Assignments). The two assignments remain separate solutions and  folders.

## Run
Use .NET SDK 10 on Windows, with Windows Desktop and ASP.NET runtime support and SQL Server.
From this folder:
```powershell
.\Run-Demo.ps1 -SetupDatabase -App All
```
Individual commands and demo accounts are in each assignment's readme.txt.

## Assignment 1 — MetroGo Smart Ticketing
- Solution: Assignment1/SmartTicketing.sln.
- Native WinForms kiosk + domain library + MSSQL persistence.
- Card / bank QR / wallet simulators, approved/declined/unknown outcomes, same-reference save retry, same-ticket printer recovery, simulated refund, service-day barcode ticket, print preview.
- Local 3D art, optional 30-second motion banner and pause control.
- Config: Assignment1/src/SmartTicketing.WinForms/appsettings.json; optional ConnectionStrings__Ticketing override.
- Tests: 21 meaningful MSTest cases and 5 isolated SQL ticket checks.
- Engineering: model-to-code skeletons, compiled-code-to-model JSON and reflected DGML graph under Assignment1/engineering.
- Reports: Assignment1_519H0245-524H0172-524H0044_TranGiaTrieu_PhamTrungNghia_LeDat.docx
- Full workbook: Assignment1_519H0245-524H0172-524H0044_TranGiaTrieu_PhamTrungNghia_LeDat.xlsx

## Assignment 2 — Northstar Wholesale
- Solution: Assignment2/WholesaleManagement.sln.
- Admin WinForms workspace + ASP.NET Core Razor Pages Agent Portal (Option B) + MSSQL.
- Role-restricted sign-in with five-failure lockout, item/agent management, transactional partner/account registration, arbitrary stock adjustment, stale-editor protection, status changes, exactly-once cancellation restoration, manifest export/print, date/agent sales filters.
- Portal collection landing page, product photographs, card/list catalog, search/category filters, quantity steppers, live cart, atomic master/detail checkout, request replay protection, My Orders and ownership-protected printable invoice.
- Original 3D art and silent 30-second welcome clip. Pause/play and reduced-motion support.
- Config: Assignment2/src/Wholesale.AdminWinForms/App.config and Assignment2/src/AgentPortal/appsettings.json.
- Classroom Admin: admin / Demo123!; Agent: agent01@wholesale.local / Demo123!.
- Tests: 26 MSTest cases and 46 SQL/browser integration checks, including an isolated two-product checkout.
- Reports: Report.docx and Report.pdf.
- Full workbook: A2_Team_Workbook.xlsx (12 sheets).

## Verification
Both solutions built successfully in the final local pass. All 47 unit tests, 5 Lab 1 SQL checks, and 46 Lab 2 SQL/browser checks passed. Checks include controlled failure on the second order detail, request replay, concurrent last-unit checkout, ownership, cancellation, lockout and RowVersion conflicts. Fixtures were removed and baseline/final table counts matched. The GitHub workflow builds both solutions and runs their unit tests on Windows; SQL/browser integration still requires the documented local environment.
The actual upgraded browser demo created order #23 with 2 Arabica and 3 Robusta, total 1,045,000 VND. Screenshots and saved evidence are under each assignment's docs folder.
Run the test workflow with:
```powershell
.\Run-Demo.ps1 -App Test
```

## Design and media
design/MetroGo_Northstar_Design_System.svg can be imported into Figma as vector layers and embedded original art. An editable design handoff is linked in DELIVERY_NOTES.md; access should be checked before presentation.
design/image-prompts.md records the built-in image_gen prompts. design/photo-sources.json records the bundled real photograph sources and license pages.
The 30-second MP4/GIF clips are silent motion graphics with camera movement over original 3D artwork and three timed captions. They are not external live-action or character-motion footage.

## Submission files
Assignment1_Submission.zip and Assignment2_Submission.zip each contain the report, workbook, readme and clean SourceCode.zip.
Complete_Lab_Package.zip contains the two assignments, design resources and build/package helpers.
Build outputs, test-run folders, local Data Protection keys and caches are excluded

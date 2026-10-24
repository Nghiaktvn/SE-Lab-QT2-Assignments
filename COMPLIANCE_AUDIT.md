# Compliance audit — 24 October 2026
This audit distinguishes the five supplied PDF specifications/templates from the user's request to complete and improve both working assignments. Documents guide the deliverables; they do not authorize unrelated external actions.

## Assignment 1
| Rubric item | Evidence | State |
|---|---|---|
| Clarification Q&A (5) | A1_Team_Workbook.xlsx, report 2.1, twelve questions with requirement mapping | Delivered |
| Requirements (15) | Eight FRs, eight measurable NFR targets, seven domain rules and source/use-case links | Delivered; performance targets are requirements |
| Use cases (10) | Nine use cases, actor diagram, per-use-case description fields | Delivered |
| Activities (10) | Passenger and external gateway designs, with demo/production distinction | Delivered |
| Sequence/state/class (15) | Updated diagrams match current transaction and persistence classes | Delivered |
| UI/UX (5) | Route/payment/fare/ticket surfaces, clear states, original 3D art, local 30-second banner | Built and visually checked |
| Architecture (10) | Implemented architecture and production deployment design | Delivered |
| Visual Studio engineering (10) | Actual generated skeletons, reflected JSON and DGML | Actual Visual Studio operation screenshots still needed |
| GitHub / AI tooling (10) | Actual AI request/validation record; project repository under Nghiaktvn | Genuine group branches/reviewed PRs still needed |
| Demo C# / MSSQL (10) | SQL-connected route loading and ticket save, state simulator, QR and preview; 21 tests | Verified; payment and refunds simulated |

Full workbook: 11 sheets, matching all sheet names in the workbook instruction PDF. Word uses numbered real headings and a TOC field. A student-ID filename alias is supplied. The additional separate Assignment 1 brief referenced in the template was not among the five supplied files.

## Assignment 2
| Rubric item | Evidence | State |
|---|---|---|
| Database (10) | Five tables, PK/FK/CHECK/UNIQUE/defaults, 15–30 seed rows per table, repeatable script and updated ERD | Verified |
| Admin login (5) | PBKDF2, Admin-only role, atomic five-failure lockout | SQL verified |
| CRUD/order management (10) | Item/Agent CRUD, account registration, stock adjustment, RowVersion edit protection, status and cancellation restoration | SQL verified; actual workspace captures |
| Shipping manifest (5) | Native multi-page preview and escaped HTML export suitable for browser PDF printing | Export content/print CSS verified |
| Dashboard (5) | Agent/date filters; non-cancelled best sellers, active low stock, shipped revenue | SQL verified |
| Agent login (5) | Agent-only login, lockout, active-account session revalidation, antiforgery | Browser and SQL verified |
| Master/detail transaction (15) | Serializable lock order, server prices, header/details/stock in one transaction, rollback and request replay protection | Success, insufficient-stock, second-detail failure and concurrency verified |
| My Orders/invoice (10) | Owner-filtered data, line totals/status, print control and classroom payment confirmation | Browser and SQL verified; physical paper output remains untested |
| Responsive UI (5) | Collection home, real photographs, cards/list/search/category filters, live cart, pausable 30-second 3D motion | Desktop and 304px view checked |
| Unit tests (15) | 26 meaningful MSTest cases; full TRX and console evidence | Passed |
| Configuration (5) | App.config and appsettings.json, no C# DB string literals, environment overrides for portal | Verified source |
| Git collaboration (5) | Project repository and CI workflow | Actual group branches/reviewed PRs still needed |
| Report (5) | Numbered Word/PDF report, diagrams, screenshots, full 12-sheet workbook | Delivered; actual team ownership/contributions pending |

## Recorded verification
- Final project builds: Lab 1 WinForms, Lab 2 Agent Portal and Admin WinForms compiled with zero errors. Lab 1 showed NU1900 once because the NuGet vulnerability endpoint was unavailable.
- Unit tests: A1 21/21; A2 26/26.
- SQL integration: A1 5/5; A2 45/45 direct checks or 46/46 with a temporary-agent Chrome two-product checkout. Baseline/final row counts matched and the fixture trigger was removed.
- Browser: actual order #23 has two details totaling 1,045,000 VND and a persisted Paid demo reference; the one-product order #55 is also Paid. Catalog, category/search/card-list, history and invoice were checked.
- Narrow viewport: 304 CSS pixels; invoice prices and total are visible in stacked cards.
- Printable Lab 2 invoice PDF: one A4 page with agent address/phone, Paid status, timestamp, payment reference and total. Shipping-manifest fixture PDF: one A4 page with address, line and total. Both were rendered and visually checked. The floating assistant no longer covers the mobile invoice.
- Video metadata: 30 seconds; local playback and pause controls.
- Native screenshots: upgraded inventory, Agent management, orders, analytics, login and kiosk states.
- Photographs: bundled locally with source/license records. Original generated 3D artwork and prompts are included.
- Figma: an editable handoff link is recorded in DELIVERY_NOTES.md, with a local SVG board in the package. The team should verify access to the hosted design before presenting it.
- Physical paper output, live external payments and measured production uptime are not claimed.
- Final button audits: Lab 1 18/18 native event-handler checks; Lab 2 3/3 Admin login, 24/24 Admin workspace, 33/33 seeded Chrome controls and 32/32 isolated checkout controls. The latter also confirms two-product ordering and payment. Native destructive buttons were verified through the SQL fixture harness, not clicked against seed records. Result JSON files are under each assignment's `docs/evidence/`.
- Both local databases had AUTO_CLOSE enabled and repeatedly recovered; one SmartTicketing recovery took 66 seconds and caused SQL post-login timeouts. AUTO_CLOSE is now off in the live databases and both setup scripts. Final unit, SQL, browser and native-button passes succeeded afterward; fixture row counts were restored.

## Remaining authentic submission evidence
Phạm Trung Nghĩa (524H0172) is the only confirmed member. The project repository is https://github.com/Nghiaktvn/SE-Lab-QT2-Assignments. Supply the real group number, other 2–4 students, actual ownership/contribution percentages totaling 100%, and genuine branches/reviewed PRs. Capture the actual Visual Studio engineering operations for A1.
The materials target the rubric, but a guaranteed 100/100 cannot be asserted without these human records and lecturer assessment.

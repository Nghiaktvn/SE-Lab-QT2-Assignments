# Dual-platform demonstration
1. Run the repeatable SQL setup and configure App.config/appsettings.json.
2. Launch Admin and Portal. Show 3D welcome graphics, product photographs and pause control.
3. Verify wrong-role rejection, then Admin and Agent sign-in with the classroom accounts.
4. Admin: add/edit a product, enter an arbitrary stock adjustment, register an agent and its portal account together, edit an agent, and demonstrate safe deactivation.
5. Portal: switch cards/list; filter category; search; select two products; confirm live total.
6. Place a multi-product order and show the invoice and My Orders. Use **Print invoice / Save PDF** to prepare a copy, then press **Confirm payment**. Verify the Paid timestamp/reference on the invoice and the Paid badge in My Orders. The one-product order #55 and two-line order #23 are saved examples. Explain that this is a classroom simulation only; no real money moves.
7. Admin: refresh incoming orders; view details; advance Pending → Processing → Shipped; export a shipping manifest with address and totals.
8. Create a separate demo order and cancel it before shipment; show restored stock and rejection of a second cancellation.
9. Filter the dashboard by dates and agent; explain shipped revenue, non-cancelled best sellers and low-stock definition.
10. Run 26 MSTest cases and the 45-check SQL harness, or 46 checks with `--browser` while the portal is running. Explain payment replay/owner checks, controlled second-detail rollback, refund stock restoration and last-unit concurrency. Show the one-page A4 invoice and shipping-manifest PDFs from `docs/evidence/`.
11. Show real group GitHub branch/PR evidence and matching 100% contributions before submission.
12. For repeatable button evidence, run `Run-Demo.ps1 -App Test -BrowserTest -ButtonTest` with the portal serving at `PORTAL_URL`. Review `docs/evidence/*button-audit.json` and both `*smoke.json` files. The native audit dismisses editor dialogs without saving; SQL fixtures safely test the actual database changes.

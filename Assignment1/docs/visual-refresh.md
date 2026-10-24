# MetroGo visual refresh — 23 October 2026

The ticket-machine screen keeps its existing WinForms layout and required purchase flow. Forest, Sunset and Lilac now recolor the welcome banner, labels, receipt panel and primary/secondary actions together. Buttons have restrained hover and pressed states; palette changes do not alter fare selection, stock checks, payment callbacks, ticket issuance, printing or recovery behavior.

The design handoff includes an editable MetroGo ticket-machine frame in [Northstar Labs 1 + 2 UI in Figma](https://www.figma.com/design/XJaFLoJzp8NrexLNmOHBAk), beside the Lab 2 portal and admin sign-in frames.

The refreshed kiosk was launched against the configured local SQL database for evidence capture. It loaded 15 active destinations; see `docs/screenshots/tvm-start-v5.png`.

The product remains a .NET WinForms ticket machine as required by the assignment. The Notezy reference informs color, spacing and hierarchy; React/Tailwind are not substituted for the rubric's required stack.

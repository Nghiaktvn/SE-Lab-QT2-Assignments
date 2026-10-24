# Visual Studio model-to-code and Code Map demo

1. Open `SmartTicketing.sln` in Visual Studio 2022 and build `SmartTicketing.WinForms`.
2. Open `docs/uml.md`, select the class diagram block, and import/paste into the UML designer available in the installed Visual Studio edition or a PlantUML extension. Keep `TicketTransaction`, `Destination`, `Ticket`, `PurchaseService`, `IPaymentGateway`, `ITicketRepository`, and `ITicketPrinter` aligned with the C# implementation.
3. Forward engineering: in the class designer, use **Generate Code** for the domain classes and interfaces, then compare with the source under `src/SmartTicketing.WinForms`.
4. Backward engineering: add a property or method to a C# domain class, refresh the model from code, and capture the reflected member.
5. Code Map: right-click the solution or a project and select **Show on Code Map**. Expand `TicketMachineForm`, persistence, and gateway dependencies; capture the map.
6. Add the forward/backward/code-map screenshots to the report. Tool availability varies by Visual Studio edition and installed extensions, so use the equivalent UML extension if the built-in designer is absent.

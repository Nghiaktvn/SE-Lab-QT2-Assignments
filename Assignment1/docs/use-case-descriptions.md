# Detailed use case descriptions

| Use case | Preconditions | Main flow | Alternate or error flow | Postcondition |
|---|---|---|---|---|
| UC-01 Buy ticket | Kiosk ready, route data and printer available. | Start; select route; confirm fare; choose method; authorize; persist; print; show receipt. | Decline allows retry; timeout requires reconciliation; print fault creates compensation case. | At most one delivered ticket for a paid transaction. |
| UC-02 Pay by card | Route and fare confirmed. | Prompt tap or insert; send tokenized authorization; verify outcome. | Decline, cancel, or timeout returns a safe status. | Payment reference linked to transaction. |
| UC-03 Pay by bank / wallet QR | Route and fare confirmed. | Create expiring amount-bound QR; display; await signed callback. | Expiry or cancellation invalidates QR; late callback requires reconciliation. | A confirmed capture or no issued ticket. |
| UC-04 Diagnose kiosk | Authenticated operator and service mode. | Check printer, network, gateway, and recent faults. | Failed self-test sets out-of-service state. | Audited diagnostic event. |
| UC-05 Reconcile transaction | Payment result unknown or print compensation pending. | Query gateway; compare capture; retry print or refund. | Gateway unavailable remains queued. | No duplicate charge or ticket. |
| UC-06 Select destination | Active fare catalogue available. | Show active routes; select destination and fare snapshot. | Missing or stale catalogue shows unavailable guidance. | Valid route and fare assigned. |
| UC-07 Choose payment | Route and fare confirmed. | Choose card, bank QR, or wallet. | Cancel or timeout discards pending selection. | One method set for the attempt. |
| UC-08 Print ticket | Confirmed payment and working printer. | Generate unique opaque barcode; print route, fare, validity, and barcode. | Printer fault creates reprint or refund case. | Ticket linked to one paid transaction. |
| UC-09 View receipt | Payment and print results known. | Show route, fare, time, reference, and support guidance. | Unknown payment shows pending guidance, not success. | Passenger can identify transaction for support. |

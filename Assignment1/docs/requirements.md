# TVM Requirements and Assumptions

## Clarification questions and assumed answers

| ID | Stakeholder question | Assumption used in the prototype |
|---|---|---|
| Q1 | Which passenger types, routes, and fares are sold? | Adult single journey; an editable destination and fare catalogue is held in the database. |
| Q2 | Are cards and wallets real integrations? | Use PCI-compliant acquirer and wallet APIs in production; demo uses a deterministic sandbox simulator only. |
| Q3 | What happens on a declined, timed-out, or duplicate payment? | Do not issue a ticket; allow retry, use an idempotency reference, and show a plain-language error. |
| Q4 | When is a ticket valid and how is it validated? | One journey, valid for the service day; barcode encodes an opaque unique ticket ID, not personal data. |
| Q5 | Are refunds, cancellation, and change supported? | Before payment: cancel. After payment: operator-assisted refund, logged and linked to original payment. |
| Q6 | What accessibility and language support is required? | Vietnamese and English, high contrast, large touch targets, keyboard/service assistance option. |
| Q7 | Which personal/payment data may be retained? | Never store PAN, CVV, wallet credentials, or QR payload; retain token/reference and transaction audit only. |
| Q8 | What uptime and response targets apply? | Monthly 99.5% kiosk service availability; local UI response p95 <= 1 second; route list <= 2 seconds; payment response <= 15 seconds before safe timeout. |
| Q9 | How is cash handled? | Cash is out of scope because only card, QR, and wallet channels are specified. |
| Q10 | What should happen if network or printer fails? | Fail safely, issue no ticket without confirmed payment, show retry/help; printer failure triggers void/refund workflow. |
| Q11 | Which external systems are in scope? | Route/fare service, card acquirer, wallet gateway (Momo/VNPay/ZaloPay), ticket validator, monitoring. |
| Q12 | What retention and audit rules apply? | Retain non-sensitive transaction events for 12 months, subject to operator policy and applicable law. |

## Functional requirements

| ID | Requirement | Acceptance measure |
|---|---|---|
| FR-01 | Start a new transaction and display active destinations and fares. | Route list appears within 2 seconds; unavailable routes cannot be selected. |
| FR-02 | Let the passenger select destination and card, bank QR, or wallet QR payment. | Selection and fare remain visible through confirmation. |
| FR-03 | Authorize payment through the selected external gateway using an idempotency key. | Approved, declined, pending, and timeout outcomes are handled without duplicate charges. |
| FR-04 | Issue a paper ticket with unique machine-readable barcode only after confirmed payment. | One successful payment maps to exactly one ticket ID and print attempt. |
| FR-05 | Show a success receipt with route, fare, time, masked payment reference, and support guidance. | Receipt contains no raw card, bank QR, or wallet credentials. |
| FR-06 | Record transaction, gateway reference, printer result, and status for reconciliation. | Audit record is queryable by transaction ID; sensitive authentication data is absent. |
| FR-07 | Recover safely after interruption. | At startup, unresolved transactions are reconciled before retrying ticket issue. |
| FR-08 | Provide operator diagnostics and out-of-service state for printer, network, or gateway faults. | Fault is timestamped and surfaced to the monitoring service. |

## Non-functional requirements

| ID | Requirement | Target |
|---|---|---|
| NFR-01 Reliability | Monthly availability per kiosk, excluding planned maintenance. | >= 99.5%; measured as available minutes / scheduled service minutes. |
| NFR-02 UI performance | Local input acknowledgement and screen transition. | p95 <= 1 s, p99 <= 2 s. |
| NFR-03 Route performance | Route/fare catalogue load from cache or service. | p95 <= 2 s; stale cache visibly timestamped and maximum age 24 h. |
| NFR-04 Payment performance | Gateway completion or safe timeout. | 95% response <= 8 s; timeout at 15 s; no ticket on unknown result until reconciliation. |
| NFR-05 Printing | Ticket print after confirmed authorization. | Start within 2 s; printer failure must trigger alert and compensation path. |
| NFR-06 Security | Payment and service data protection. | TLS 1.2+, encrypted secrets, least privilege, no PAN/CVV storage, signed gateway callbacks, audit logs. |
| NFR-07 Accessibility | Touch use in public environment. | WCAG-aligned contrast, >= 44 px targets, readable text, bilingual labels and timeout extension. |
| NFR-08 Maintainability | Route, fare, gateway, and printer boundaries. | Ports/adapters, documented configuration, structured logs, automated unit/integration tests. |

## Domain rules

1. A paid transaction has exactly one ticket; no pending/declined transaction has a usable ticket.
2. A fare is taken from the selected active route at checkout and recorded as an immutable snapshot.
3. Transaction IDs and gateway idempotency keys are unique. Repeated callbacks must not issue duplicate tickets.
4. A barcode contains an opaque ticket identifier and is unique across all kiosks.
5. Reconciliation governs ambiguous gateway outcomes. A timeout is not a decline and is never silently charged again.
6. Wallet QR codes are short-lived, single-use, bound to amount and transaction, and invalidated on cancellation/expiry.
7. Printer failure after successful capture creates a compensation case; it never marks a ticket delivered.

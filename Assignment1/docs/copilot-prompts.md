# AI assistance and validation record

The following are actual user instructions given to Codex, an AI coding tool used to prepare this prototype. They are not presented as GitHub Copilot transcripts.

| Actual prompt or instruction | Assistance received | Human-review and verification status |
|---|---|---|
| “hãy giúp tôi hoàn tất cả yêu cầu trong này ... tách ra làm 2 file riêng ...” | Created separate C# solutions, SQL scripts, requirements, diagrams, and report files for both briefs. | Requirements were compared with each supplied PDF; both solutions were built with dotnet. |
| “chạy ngay trực tiếp cho tôi và cho tôi thấy được những gì bạn làm ...” | Ran solution builds, inspected local SQL availability, and attempted direct UI/web demos. | Builds passed; the first sandboxed SQL attempt failed. A later SQLEXPRESS run outside the sandbox verified database persistence. |
| “tiếp tục xem xét và cải thiện hết tất cả yêu cầu ... kiểm tra báo cáo 1 lần nữa ...” | Improved the wallet QR, ticket barcode and print preview, multi-line order transaction, Razor Pages, admin CRUD, and report coverage. | Builds passed; one ticket was persisted to SQL, one two-line order was placed with correct stock deductions, and 8/8 unit tests passed. |
| “kiểm tra và test tất cả chức năng đồng thời kiểm tra báo cáo cũng phải đạt đầy đủ như bảng rubric trong pdf đó” | Re-read both supplied PDF rubrics, rebuilt the A2 solution to an isolated output folder, reran its unit suite, and checked the current SQL demo records. | Both solution builds and eight MSTest cases passed; current SQL record counts and demo ticket/order values were checked with read-only queries. Visual Studio/GitHub/group evidence remains a team-supplied requirement. |

No real payment credentials or live payment calls were generated. The team should review every AI-produced artifact and add its genuine Copilot prompt history if Copilot was used. The SQL-connected ticket insert was verified with SQLEXPRESS; payment and printing remain simulated.

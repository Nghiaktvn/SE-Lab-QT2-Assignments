import fs from 'node:fs/promises';
import { SpreadsheetFile, Workbook } from 'file:///C:/Users/ADMIN/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const outDir='C:/SE_LAB1_2/Assignment1';
const wb=Workbook.create();
const qa=wb.worksheets.add('Clarification Q&A');
qa.showGridLines=false; qa.tabColor='#1568A8';
qa.getRange('A1:D1').merge(); qa.getRange('A1').values=[['TVM stakeholder clarification Q&A']];
qa.getRange('A2:D2').merge(); qa.getRange('A2').values=[['Assignment 1 | Explicit assumptions used for the classroom design. Confirm with stakeholders before production.']];
qa.getRange('A4:D4').values=[['ID','Clarifying question','Assumed answer','Design impact']];
qa.getRange('A5:D16').values=[
 ['Q1','Which passenger types, routes, and fares are sold?','Adult single journey; operator-maintained route and fare catalogue.','Destination and fare snapshot in each ticket transaction.'],
 ['Q2','Are card and wallet integrations live?','Production uses certified providers; current demo is simulation only.','Adapter boundaries; no real charge or credentials in demo.'],
 ['Q3','How are declined, timed-out, and duplicate payments handled?','Decline permits retry; timeout remains unknown until reconciliation; callbacks are idempotent.','No ticket until confirmed; unique idempotency reference.'],
 ['Q4','When is a ticket valid and how is it checked?','Single journey valid for the service day; barcode holds an opaque unique ticket ID.','Unique ticket identity; no personal data in barcode.'],
 ['Q5','Are refund and cancellation supported?','Cancel before payment; after capture use operator-audited reversal/refund.','Transaction state and compensation record.'],
 ['Q6','Which accessibility and language requirements apply?','Vietnamese/English, high contrast, large touch targets, accessible timeout.','Bilingual UI and accessibility acceptance criteria.'],
 ['Q7','What payment data is retained?','Never retain PAN, CVV, wallet credentials, or raw QR; retain masked provider reference.','Data minimization and secure audit log.'],
 ['Q8','What availability and response time are required?','99.5% monthly kiosk availability; UI p95 <=1s; routes <=2s; payment timeout 15s.','Measurable NFRs; monitor availability and latency.'],
 ['Q9','Is cash payment required?','Out of scope; brief specifies card, QR, and wallet.','No cash hardware or cash reconciliation.'],
 ['Q10','What if network or printer fails?','Fail safely; no ticket before confirmed payment; reconcile, reprint, or compensate.','Fault state and recovery workflow.'],
 ['Q11','Which external systems are integrated?','Route/fare API, card acquirer, Momo/VNPay/ZaloPay, validator, monitoring.','Deployment and activity diagram actors.'],
 ['Q12','How long are records retained?','Non-sensitive transaction events retained 12 months, subject to operator policy.','Retention configurable; audit policy requires approval.']
];
qa.getRange('A1:D1').format={fill:'#12345A',font:{bold:true,color:'#FFFFFF',size:17},verticalAlignment:'center'}; qa.getRange('A1:D1').format.rowHeight=34;
qa.getRange('A2:D2').format={fill:'#EAF1F8',font:{italic:true,color:'#42566D',size:10},wrapText:true,verticalAlignment:'center'};qa.getRange('A2:D2').format.rowHeight=32;
qa.getRange('A4:D4').format={fill:'#1568A8',font:{bold:true,color:'#FFFFFF'},wrapText:true,verticalAlignment:'center'};
qa.getRange('A5:D16').format={wrapText:true,verticalAlignment:'center',font:{size:10}}; qa.getRange('A4:D16').format.borders={preset:'all',style:'thin',color:'#D9D9D9'};
qa.getRange('A:A').format.columnWidth=8;qa.getRange('B:B').format.columnWidth=40;qa.getRange('C:C').format.columnWidth=54;qa.getRange('D:D').format.columnWidth=46;qa.getRange('A4:D16').format.autofitRows();qa.freezePanes.freezeRows(4);

const req=wb.worksheets.add('Requirements');req.showGridLines=false;req.tabColor='#39885A';
req.getRange('A1:D1').merge();req.getRange('A1').values=[['TVM requirement baseline']];
req.getRange('A3:D3').values=[['ID','Type','Requirement','Acceptance target']];
req.getRange('A4:D19').values=[
 ['FR-01','Functional','Start a purchase and show active destinations and fares.','Route list response <=2 seconds; inactive routes cannot be selected.'],
 ['FR-02','Functional','Select destination and card or wallet QR payment.','Fare and route remain visible until confirmation.'],
 ['FR-03','Functional','Authorize using an idempotency key and handle all outcomes.','Approve/decline/pending/timeout handled with no duplicate charge.'],
 ['FR-04','Functional','Print a barcode ticket only after confirmed payment.','Exactly one usable ticket per paid transaction.'],
 ['FR-05','Functional','Display route, fare, time, and masked reference on receipt.','No sensitive payment credential appears.'],
 ['FR-06','Functional','Audit payment, transaction, print, and status events.','Searchable by transaction ID; excludes PAN/CVV.'],
 ['FR-07','Functional','Recover and reconcile after interruption.','Unresolved transaction reconciled before issue/retry.'],
 ['FR-08','Functional','Expose operator diagnostics and kiosk out-of-service status.','Timestamped fault visible to monitoring.'],
 ['NFR-01','Reliability','Monthly service availability excluding planned maintenance.','>=99.5%; available scheduled minutes / total scheduled minutes.'],
 ['NFR-02','Performance','Local input and screen transition.','p95 <=1 second; p99 <=2 seconds.'],
 ['NFR-03','Performance','Route/fare catalogue load.','p95 <=2 seconds; cached data timestamped, max age 24 hours.'],
 ['NFR-04','Performance','Payment completion or safe timeout.','95% <=8 seconds; timeout at 15 seconds; reconcile unknown result.'],
 ['NFR-05','Reliability','Print after confirmed authorization.','Print starts <=2 seconds; failure initiates alert and compensation.'],
 ['NFR-06','Security','Protect payment and service data.','TLS 1.2+, encrypted secrets, least privilege, no PAN/CVV, signed callbacks.'],
 ['NFR-07','Accessibility','Support public touchscreen use.','Large targets >=44 px, readable contrast, bilingual UI, timeout extension.'],
 ['NFR-08','Maintainability','Isolate routes, gateways, persistence, and printer.','Ports/adapters, structured logs, documented config, automated tests.']
];
req.getRange('A1:D1').format={fill:'#12345A',font:{bold:true,color:'#FFFFFF',size:17},verticalAlignment:'center'};req.getRange('A1:D1').format.rowHeight=34;
req.getRange('A3:D3').format={fill:'#39885A',font:{bold:true,color:'#FFFFFF'},verticalAlignment:'center'};req.getRange('A3:D19').format={wrapText:true,verticalAlignment:'center',font:{size:10}};req.getRange('A3:D19').format.borders={preset:'all',style:'thin',color:'#D9D9D9'};
req.getRange('A:A').format.columnWidth=13;req.getRange('B:B').format.columnWidth=18;req.getRange('C:C').format.columnWidth=54;req.getRange('D:D').format.columnWidth=55;req.getRange('A3:D19').format.autofitRows();req.freezePanes.freezeRows(3);
await wb.recalculate();
for(const name of ['Clarification Q&A','Requirements']){const img=await wb.render({sheetName:name,autoCrop:'all',scale:1,format:'png'});await fs.writeFile(`${outDir}/${name.replaceAll(' ','_')}_preview.png`,new Uint8Array(await img.arrayBuffer()));}
const xlsx=await SpreadsheetFile.exportXlsx(wb);await xlsx.save(`${outDir}/TVM_Clarification_and_Requirements.xlsx`);
console.log((await wb.inspect({kind:'sheet',include:'id,name'})).ndjson);

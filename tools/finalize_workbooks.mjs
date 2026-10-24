import fs from 'node:fs/promises';
import {FileBlob,SpreadsheetFile} from 'file:///C:/Users/ADMIN/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
for(const n of [1,2]){
 const path=`C:/SE_LAB1_2/Assignment${n}/A${n}_Team_Workbook.xlsx`;
 const w=await SpreadsheetFile.importXlsx(await FileBlob.load(path));
 const updates=n===1?{'Artifact Tracker':[['D10','21 unit + 5 SQL checks passed']],'Marking':[['F13','21 unit tests and 5 SQL checks; kiosk demo verified']]}:{'Marking':[['F7','One-page A4 shipping-manifest PDF checked'],['F11','Browser checkout, owner isolation, Paid PDF checked'],['F12','304px invoice, no overflow or helper obstruction'],['F13','26/26 passed']],'AI Usage Log':[['E4','26 unit + 44 SQL/browser checks']]};
 for(const [name,cells] of Object.entries(updates))for(const [cell,value] of cells)w.worksheets.getItem(name).getRange(cell).values=[[value]];
 if(n===2){const tests=JSON.parse(await fs.readFile('C:/SE_LAB1_2/.audit/tests2.json','utf8'));const range=w.worksheets.getItem('Unit Tests').getRange(`A4:D${tests.length+3}`);range.values=tests.map(([name,result])=>[name,'Domain rules, passwords or local assistant; inspect test method','Assertion passes',result]);range.format.wrapText=true;range.format.rowHeight=55;}
 const log=w.worksheets.getItem(n===1?'AI Prompt Log':'AI Usage Log');log.getRange('A7:E7').values=[['23 October 2026','Codex','Add local AI and flexible palettes','TF-IDF guidance; read-only recommendations; three themes','21 A1 / 26 A2 unit tests; browser AI demo']];
 log.getRange('A7:E7').format.wrapText=true;log.getRange('A7:E7').format.rowHeight=70;
 log.getRange('A8:E8').values=[["24 October 2026","Codex","Verify transactions and printable evidence",n===1?"Idempotent ticket SQL harness and clean fixture":"Two-product browser checkout; paid A4 invoice; mobile overlap fix",n===1?"21 unit + 5 SQL checks":"26 unit + 44 SQL/browser checks"]];
 log.getRange('A8:E8').format.wrapText=true;log.getRange('A8:E8').format.rowHeight=70;
 if(n===2){const tests=w.worksheets.getItem('Transaction Tests');tests.getRange('A12:D12').values=[['Two-line browser checkout','Temporary Agent; two quantity fields; confirm payment','Paid invoice and My Orders; fixture cleaned','Passed']];tests.getRange('A12:D12').format.wrapText=true;tests.getRange('A12:D12').format.rowHeight=60;}
 await (await SpreadsheetFile.exportXlsx(w)).save(path);const preview=await w.render({sheetName:'Marking',range:'A3:G8',scale:1,format:'png'});await fs.writeFile(`C:/SE_LAB1_2/.audit/workbook${n}-review.png`,new Uint8Array(await preview.arrayBuffer()));console.log(`Updated workbook ${n}`);
}

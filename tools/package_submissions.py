"""Package reviewable source, reports, workbooks and evidence; never include local keys/build outputs."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[1]
SKIP={'.git','.audit','.vs','bin','obj','TestResults','__pycache__','keys','node_modules','tmp','tmp_render'}
LEGACY={'build_report.py','build_reports.py','build_workbook.mjs'}
def allowed(p):
    rel=p.relative_to(ROOT)
    return p.is_file() and not any(x in SKIP for x in rel.parts) and p.suffix.lower() not in {'.zip','.pyc','.user','.suo','.log','.ndjson'} and p.name!='SUBMISSION_MANIFEST.json'
def add(z,p,name):z.write(p,str(name).replace('\\','/'))
for n in (1,2):
    folder=ROOT/f'Assignment{n}';evidence=folder/'docs/evidence';evidence.mkdir(exist_ok=True)
    for trx in folder.glob('tests/**/TestResults/*.trx'):shutil.copy2(trx,evidence/trx.name)
    with ZipFile(folder/'SourceCode.zip','w',ZIP_DEFLATED) as z:
        for p in sorted(folder.rglob('*')):
            if allowed(p) and p.name not in LEGACY and (p.suffix.lower() not in {'.docx','.pdf','.xlsx'} or p.suffix.lower()=='.pdf' and evidence in p.parents):add(z,p,p.relative_to(ROOT))
        for p in sorted((ROOT/'design').rglob('*')):
            if allowed(p):add(z,p,p.relative_to(ROOT))
    with ZipFile(ROOT/f'Assignment{n}_Submission.zip','w',ZIP_DEFLATED) as z:
        for name in ['Report.docx','Report.pdf',f'A{n}_Team_Workbook.xlsx','readme.txt','SourceCode.zip']:
            add(z,folder/name,name)
        add(z,ROOT/'DELIVERY_NOTES.md','DELIVERY_NOTES.md')
        if n==1:add(z,folder/'524H0172_PhamTrungNghia.docx','524H0172_PhamTrungNghia.docx')
    print(f'Assignment{n}: source and submission archives created')
with ZipFile(ROOT/'Complete_Lab_Package.zip','w',ZIP_DEFLATED) as z:
    for p in sorted(ROOT.rglob('*')):
        if allowed(p):add(z,p,p.relative_to(ROOT))
manifest=[]
for p in [ROOT/'Assignment1_Submission.zip',ROOT/'Assignment2_Submission.zip',ROOT/'Complete_Lab_Package.zip']:
    with ZipFile(p) as z:
        invalid=[x for x in z.namelist() if any(part in SKIP for part in Path(x).parts)]
        if invalid:raise RuntimeError(f'Excluded paths leaked into {p.name}: {invalid}')
        bad=z.testzip()
        if bad:raise RuntimeError(f'Corrupt archive entry: {bad}')
    manifest.append({'file':p.name,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(ROOT/'SUBMISSION_MANIFEST.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(manifest,indent=2))

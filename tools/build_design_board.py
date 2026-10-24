from pathlib import Path
import base64,html
ROOT=Path(__file__).resolve().parents[1]
def embed(path):return 'data:image/png;base64,'+base64.b64encode((ROOT/path).read_bytes()).decode()
metro=embed('Assignment1/assets/metrogo-3d-hero.png');wholesale=embed('Assignment2/src/AgentPortal/wwwroot/media/northstar-3d-hero.png')
svg=['<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1600" height="1160" viewBox="0 0 1600 1160">', '<rect width="1600" height="1160" fill="#fffaf3"/>']
def text(x,y,value,size=18,color='#183638',weight='400'):
    svg.append(f'<text x="{x}" y="{y}" font-family="Arial,sans-serif" font-size="{size}" font-weight="{weight}" fill="{color}">{html.escape(value)}</text>')
def box(x,y,w,h,fill,r=20):svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"/>')
text(55,65,'METROGO × NORTHSTAR',35,weight='700');text(55,99,'Original visual system · 23 October 2026 · Import this SVG into Figma to edit the vector layers.',15,'#677a79')
for i,(name,color) in enumerate([('Forest','#183638'),('Teal','#24715d'),('Coral','#ed7f63'),('Butter','#f5d774'),('Lilac','#e9dffd'),('Cream','#fff0d9')]):
    x=55+i*248;box(x,140,225,78,color,16);text(x,244,name,15);text(x,267,color.upper(),12,'#677a79')
text(55,325,'COMPONENT LANGUAGE',13,'#677a79','700')
for x,label,color,ink in [(55,'Continue to payment →','#24715d','#ffffff'),(365,'Explore collection ↗','#ed7f63','#183638'),(675,'Order confirmed ✓','#dcebd6','#285e40'),(985,'Pending dispatch','#fff0d1','#8b6e24'),(1285,'Pause motion','#ede2f8','#5c4c79')]:
    box(x,345,260 if x<985 else 220,54,color,27);text(x+20,378,label,16,ink,'700')
box(55,445,725,500,'#fff0df',26);box(820,445,725,500,'#eee5fa',26)
text(83,495,'A little ticket. A big adventure.',25,weight='700');text(848,495,'A little goodness. A lot of possibilities.',23,weight='700')
svg.append(f'<image x="80" y="525" width="675" height="380" xlink:href="{metro}"/>');svg.append(f'<image x="845" y="525" width="675" height="380" xlink:href="{wholesale}"/>')
text(55,1000,'MOTION & ACCESSIBILITY',13,'#677a79','700');text(55,1035,'30-second silent clips • Pause/play • Reduced-motion preference • Local poster fallback',20)
text(55,1075,'Clear focus states • Explicit status feedback • Quantity controls • Search and category filters • Atomic checkout',17)
text(55,1122,'This board is an importable artifact. No connected Figma file has been edited or claimed.',13,'#677a79')
svg.append('</svg>');target=ROOT/'design/MetroGo_Northstar_Design_System.svg';target.write_text('\n'.join(svg),encoding='utf-8');print(target)

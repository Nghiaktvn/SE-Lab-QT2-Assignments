"""Rebuild the two 30-second motion-graphics clips from the original 3D artwork.
Requires FFmpeg. Existing image artwork is not changed.
"""
from pathlib import Path
import subprocess, shutil, sys, json
ROOT=Path(__file__).resolve().parents[1]
FFMPEG=sys.argv[1] if len(sys.argv)>1 else shutil.which('ffmpeg')
if not FFMPEG: raise SystemExit('Pass the path to the FFmpeg executable.')
jobs=[
('metrogo',ROOT/'Assignment1/assets/metrogo-3d-hero.png',ROOT/'Assignment1/assets',["Choose your destination.","Confirm a simulated payment.","Your ticket. Your next adventure."],'0x143e43'),
('northstar',ROOT/'Assignment2/src/AgentPortal/wwwroot/media/northstar-3d-hero.png',ROOT/'Assignment2/src/AgentPortal/wwwroot/media',["A little goodness. A lot of possibilities.","Build your next wholesale order.","Everyday essentials. Extraordinary business."],'0x283d3e')]
for name,art,out,lines,color in jobs:
 filters=["scale=1344:756:flags=lanczos","crop=1280:720:x='32+20*sin(t*2*PI/30)':y='18+12*sin(t*2*PI/15)'","setsar=1","drawbox=x=35:y=610:w=1210:h=80:color=white@0.88:t=fill"]
 for i,text in enumerate(lines):
  textfile=ROOT/'design'/f'{name}-caption-{i}.txt';textfile.write_text(text)
  filters.append(f"drawtext=fontfile='C\\:/Windows/Fonts/segoeui.ttf':textfile='{textfile.relative_to(ROOT).as_posix()}':fontcolor={color}:fontsize=30:x=(w-text_w)/2:y=633:enable='gte(t,{i*10})*lt(t,{(i+1)*10})'")
 target=out/f'{name}-welcome-30s.mp4'
 subprocess.run([FFMPEG,'-y','-loglevel','error','-loop','1','-framerate','30','-i',str(art),'-vf',','.join(filters),'-t','30','-an','-c:v','libx264','-preset','fast','-crf','23','-pix_fmt','yuv420p','-movflags','+faststart',str(target)],cwd=ROOT,check=True)
 gifout=(ROOT/'Assignment1/assets' if name=='metrogo' else ROOT/'Assignment2/assets')/f'{name}-welcome-30s.gif'
 subprocess.run([FFMPEG,'-y','-loglevel','error','-i',str(target),'-vf','fps=8,scale=400:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=96[p];[s1][p]paletteuse=dither=bayer:bayer_scale=5','-loop','0',str(gifout)],check=True)
 print(target,flush=True);print(gifout,flush=True)

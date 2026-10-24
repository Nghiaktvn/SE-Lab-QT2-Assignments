"""Render a 30-second composited mascot advertisement and a 4K export."""
from pathlib import Path
import subprocess, sys

ROOT = Path(__file__).resolve().parents[1]
MEDIA = ROOT / 'Assignment2/src/AgentPortal/wwwroot/media'
FF = sys.argv[1]
def run(args):
    subprocess.run([FF, '-y', '-hide_banner', '-loglevel', 'error', '-threads', '2', *args], cwd=ROOT, check=True)

for name, width in [('nova-3d-v2.webp', 800), ('nova-3d-avatar.webp', 180)]:
    run(['-i', str(MEDIA/'nova-3d-v2.png'), '-vf', f'scale={width}:-1', '-c:v', 'libwebp', '-quality', '88', str(MEDIA/name)])

copy = [('NICE TO MEET YOU', 'Hello, I am Nova.', 'A little help. A brighter day.'),
        ('FIND YOUR FAVORITES', 'Good things, together.', 'Coffee, pantry staples and everyday joy.'),
        ('YOUR NEXT GOOD THING', 'Make room for possibility.', 'Explore the Northstar collection.')]
filters = ["[0:v]format=yuv420p,drawbox=x=0:y=875:w=1920:h=205:color=0xd6e8db:t=fill[bg]",
           "[1:v]scale=1000:-1,format=rgba[art]",
           "[2:v]scale=440:-1,format=rgba[nova]",
           "[bg][art]overlay=x=970:y=180[scene]",
           "[scene][nova]overlay=x='820+50*sin(t*2*PI/15)':y='430+12*sin(t*2*PI/3)'[character]"]
font = "fontfile='C\\:/Windows/Fonts/segoeui.ttf'"
chain = '[character]'
for index, lines in enumerate(copy):
    for line_index, line in enumerate(lines):
        path = ROOT/'design'/f'story-{index}-{line_index}.txt'
        path.write_text(line, encoding='utf-8')
        size = [22, 51, 25][line_index]
        y = [330, 395, 480][line_index]
        alpha = f"if(lt(t,{index*10+0.6}),(t-{index*10})/0.6,if(gt(t,{index*10+9.4}),({index*10+10}-t)/0.6,1))"
        chain += f"drawtext={font}:textfile='{path.relative_to(ROOT).as_posix()}':fontsize={size}:fontcolor=0x234c40:x=110:y={y}:alpha='{alpha}':enable='gte(t,{index*10})*lt(t,{index*10+10})',"
chain += f"drawtext={font}:text='NORTHSTAR':fontsize=26:fontcolor=0x345b4a:x=110:y=950,drawtext={font}:text='A LITTLE GOODNESS. A LOT OF POSSIBILITIES.':fontsize=16:fontcolor=0x345b4a:x=110:y=995,drawtext=fontfile='C\\:/Windows/Fonts/seguisym.ttf':text='✦':fontsize=82:fontcolor=0xe5b955:x='1550+18*sin(t)':y='80+12*cos(t)',format=yuv420p[out]"
filters.append(chain)
run(['-filter_complex_threads','1','-f','lavfi','-i','color=c=0xfff5e9:s=1920x1080:r=24:d=30',
     '-loop','1','-framerate','24','-i',str(MEDIA/'northstar-3d-hero.png'),
     '-loop','1','-framerate','24','-i',str(MEDIA/'nova-3d-v2.png'),
     '-filter_complex',';'.join(filters),'-map','[out]','-t','30','-an','-c:v','libx264','-threads','2','-preset','fast','-crf','23','-movflags','+faststart',str(MEDIA/'northstar-story-web.mp4')])
print('Web film ready', flush=True)
run(['-ss','1','-i',str(MEDIA/'northstar-story-web.mp4'),'-frames:v','1','-q:v','3',str(MEDIA/'northstar-story-poster.jpg')])
run(['-i',str(MEDIA/'northstar-story-web.mp4'),'-vf','scale=3840:2160:flags=lanczos','-c:v','libx264','-threads','2','-preset','fast','-crf','25','-an','-movflags','+faststart',str(MEDIA/'northstar-story-4k.mp4')])
print('4K export ready (upscaled from the 1080p composition)', flush=True)

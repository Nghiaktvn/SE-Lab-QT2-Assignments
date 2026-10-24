from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

out = Path(__file__).with_name('sequence.png')
image = Image.new('RGB', (1230, 1430), '#ffffff')
draw = ImageDraw.Draw(image)
head = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 22)
body = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
xs = [95, 265, 435, 605, 775, 945, 1115]
names = ['Passenger', 'TicketMachine\nForm', 'Ticket\nTransaction', 'Ticket\nStore', 'Payment\nSimulator', 'SQL\nServer', 'Print\nPreview']
for x, name in zip(xs, names):
    draw.rounded_rectangle((x-76, 34, x+76, 112), radius=8, fill='#eaf3fb', outline='#2470a5', width=3)
    box = draw.multiline_textbbox((0, 0), name, font=head, align='center', spacing=2)
    draw.multiline_text((x-(box[2]-box[0])/2, 73-(box[3]-box[1])/2), name, fill='#17365d', font=head, align='center', spacing=2)
    for y in range(120, 1370, 18):
        draw.line((x, y, x, min(y+9, 1370)), fill='#8294a8', width=2)

messages = [
    (0, 1, 'select route + method', False),
    (1, 3, 'Routes()', False),
    (3, 1, 'active routes + fares', True),
    (1, 2, 'new(fare snapshot)', False),
    (1, 4, 'simulate outcome', False),
    (4, 1, 'approved / reference', True),
    (1, 2, 'MarkPaid + IssueTicket', False),
    (1, 3, 'Save(transaction)', False),
    (3, 5, 'idempotent INSERT', False),
    (5, 1, 'save confirmed', True),
    (1, 6, 'preview ticket', False),
    (1, 0, 'receipt + barcode', True),
]
for index, (source, target, label, response) in enumerate(messages):
    y = 190 + index * 98
    left, right = xs[source], xs[target]
    color = '#44718e' if response else '#17365d'
    if response:
        for start in range(min(left, right), max(left, right), 20):
            draw.line((start, y, min(start+11, max(left,right)), y), fill=color, width=3)
    else:
        draw.line((left, y, right, y), fill=color, width=3)
    direction = 1 if right > left else -1
    draw.polygon([(right, y), (right-direction*15, y-7), (right-direction*15, y+7)], fill=color)
    bbox = draw.textbbox((0, 0), label, font=body)
    center = (left + right) / 2
    draw.text((center-(bbox[2]-bbox[0])/2, y-30), label, fill='#17365d', font=body)

image.save(out)
print(out)

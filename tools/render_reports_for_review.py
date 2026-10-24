from pathlib import Path

import pypdfium2
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
for lab in ("Assignment1", "Assignment2"):
    pdf = pypdfium2.PdfDocument(root / lab / "Report.pdf")
    output = root / lab / "tmp_render" / "final_20261024"
    output.mkdir(parents=True, exist_ok=True)
    contact = Image.new("RGB", (900, ((len(pdf) + 3) // 4) * 320), "white")
    draw = ImageDraw.Draw(contact)
    for index in range(len(pdf)):
        image = pdf[index].render(scale=1.25).to_pil().convert("RGB")
        image.save(output / f"page-{index + 1}.png")
        contact.paste(image.resize((210, 285)), ((index % 4) * 225 + 8, (index // 4) * 320 + 23))
        draw.text(((index % 4) * 225 + 8, (index // 4) * 320 + 5), str(index + 1), fill="black")
    contact.save(output / "contact.png")
    print(f"{lab}: {len(pdf)} rendered pages")

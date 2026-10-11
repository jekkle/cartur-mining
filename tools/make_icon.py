"""Draws the 256x256 Thunderstore icon: a pickaxe over a chunk of copper ore.

Thunderstore rejects anything that is not exactly 256x256 PNG, and there is no art asset to crop
from, so the icon is drawn - same approach and background as the other Cartur icons.

    python tools/make_icon.py
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "package", "icon.png")

SIZE = 256
SCALE = 4

BACKGROUND = (38, 34, 30, 255)
ROCK = (104, 98, 92, 255)
ROCK_EDGE = (58, 54, 50, 255)
ORE = (212, 120, 62, 255)
ORE_EDGE = (120, 60, 26, 255)
HANDLE = (138, 96, 58, 255)
HANDLE_EDGE = (74, 48, 26, 255)
IRON = (176, 182, 188, 255)
IRON_EDGE = (78, 82, 88, 255)

s = SCALE
image = Image.new("RGBA", (SIZE * s, SIZE * s), BACKGROUND)
draw = ImageDraw.Draw(image)


def poly(points, fill, edge, width=5):
    draw.polygon([(x * s, y * s) for x, y in points], fill=fill, outline=edge, width=width * s)


# The deposit: a faceted rock with three veins of ore.
poly([(40, 236), (54, 172), (98, 140), (160, 146), (210, 178), (222, 236)], ROCK, ROCK_EDGE)
poly([(74, 200), (96, 176), (118, 194), (100, 216)], ORE, ORE_EDGE, 4)
poly([(132, 172), (160, 162), (170, 188), (146, 198)], ORE, ORE_EDGE, 4)
poly([(150, 214), (176, 204), (190, 224), (164, 232)], ORE, ORE_EDGE, 4)

# The pickaxe: handle from bottom right to top left, curved head across its top.
draw.line([(206 * s, 150 * s), (92 * s, 36 * s)], fill=HANDLE_EDGE, width=22 * s)
draw.line([(206 * s, 150 * s), (92 * s, 36 * s)], fill=HANDLE, width=14 * s)
head = [(40, 64), (70, 30), (100, 22), (132, 26), (162, 48), (128, 42), (100, 44), (72, 56)]
poly(head, IRON, IRON_EDGE)

image = image.resize((SIZE, SIZE), Image.LANCZOS)
image.save(OUT)
print("wrote", OUT, image.size)

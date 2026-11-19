"""Package rune-free stone; RuneLayout.cs places the small inscriptions at runtime."""
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'upload-artwork'/'pillar-rune-free-face-master.png'
im=Image.open(source).convert('RGBA').resize((384,1152),Image.Resampling.LANCZOS)
im.save(ROOT/'assets'/'runes.png')
(ROOT/'pillar-stone-input.rgba').write_bytes(im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print('Packaged rune-free stone and preview input; runtime glyph size and placement are independent.')

"""Package the checked DLL and approved store artwork without regenerating either."""
from pathlib import Path
from PIL import Image
import json,zipfile
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'manifest.json').read_text())
version=manifest['version_number']
assert Image.open(ROOT/'icon.png').size==(256,256)
archive=ROOT/('OctoberJemini-Portal_Atlas_Map-'+version+'.zip')
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for name in ('manifest.json','icon.png','README.md','CHANGELOG.md','PortalAtlas.dll'):z.write(ROOT/name,name)
print(archive)

"""Export approved generated artwork; draw the simple UI glyphs at source resolution."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets'
OUT.mkdir(parents=True,exist_ok=True)
GENERATED=Path('C:/Users/che/.codex/generated_images/01a1c71e-06e7-7c42-b749-ec790720fd09')
sources={'atlas':'exec-f29e5279-266c-43b7-a30e-cf9980c18817.png'}
for name,file in sources.items():
    im=Image.open(GENERATED/file).convert('RGBA')
    if im.getextrema()[3][0] == 255: raise RuntimeError('Expected genuinely transparent generated artwork')
    crop=im.crop(im.getchannel('A').getbbox())
    crop.thumbnail((232,232),Image.Resampling.LANCZOS)
    output=Image.new('RGBA',(256,256))
    output.alpha_composite(crop,((256-crop.width)//2,(256-crop.height)//2))
    output.save(OUT/(name+'.png'))

S=256
gold='#efc265'; white='#eadfca'; red='#ff4545'
def icon(name,draw):
    im=Image.new('RGBA',(S,S)); d=ImageDraw.Draw(im); draw(d)
    im.save(OUT/(name+'.png'))
def star(d,filled):
    pts=[]
    for i in range(10):
        a=-math.pi/2+i*math.pi/5; r=105 if i%2==0 else 46
        pts.append((128+math.cos(a)*r,132+math.sin(a)*r))
    if filled: d.polygon(pts,fill=gold)
    else: d.line(pts+[pts[0]],fill=white,width=14,joint='curve')
def portal(d):
    d.arc((48,25,208,185),180,360,fill='#20191a',width=55)
    d.arc((48,25,208,185),180,360,fill=red,width=33)
    for x in (48,175):
        d.rounded_rectangle((x-10,100,x+43,226),8,fill='#20191a')
        d.rectangle((x+1,100,x+32,213),fill=red)
    for x in (30,163): d.rounded_rectangle((x,205,x+64,237),6,fill=red,outline='#20191a',width=5)
    d.line((128,27,128,62),fill='#20191a',width=8)
from create_pillar_pin import build_icon
build_icon(OUT/'pillar-pin.png')
for obsolete in ('portal-pin.png',):
    old=OUT/obsolete
    if old.exists():old.unlink()
icon('star-on',lambda d:star(d,True));icon('star-off',lambda d:star(d,False))
def pencil(d):
    d.polygon([(57,167),(167,57),(201,91),(91,201)],fill=gold)
    d.polygon([(48,211),(57,171),(88,203)],fill=white)
    d.polygon([(180,43),(194,29),(227,62),(213,76)],fill=gold)
icon('rename',pencil)
icon('search',lambda d:(d.ellipse((36,30,176,170),outline=white,width=23),d.line((163,160,224,222),fill=white,width=27)))
def travel(d):
    d.line([(127,33),(218,33),(218,224),(126,224)],fill=gold,width=19)
    d.rectangle((27,113,144,142),fill=gold)
    d.polygon([(119,75),(179,127),(119,181)],fill=gold)
icon('travel',travel)
icon('close',lambda d:(d.line((49,49,207,207),fill=white,width=24),d.line((207,49,49,207),fill=white,width=24)))

# These are the exact packaged sprites, displayed at 96, 32 and 22 pixels.
names=[('atlas','Atlas item'),('pillar','Pillar build piece'),(None,'Portal map pin'),('pillar-pin','Pillar map pin'),('star-on','Favourite'),('star-off','Not favourited'),('rename','Rename'),('search','Search'),('travel','Travel'),('close','Close')]
sheet=Image.new('RGB',(1100,610),'#1d1d1c'); d=ImageDraw.Draw(sheet)
fontpath='C:/Windows/Fonts/seguisb.ttf'
title=ImageFont.truetype(fontpath,28);font=ImageFont.truetype(fontpath,18);small=ImageFont.truetype(fontpath,14)
d.text((30,20),'PORTAL ATLAS — PACKAGED IN-GAME ICONS',font=title,fill=gold)
d.text((30,61),'Transparent assets shown on charcoal • 96 px artwork / 32 px / 22 px',font=small,fill=white)
for i,(name,label) in enumerate(names):
    x=20+(i%5)*218; y=105+(i//5)*245
    if name:
        im=Image.open(OUT/(name+'.png'))
        for size,px,py in [(96,x+57,y),(32,x+63,y+113),(22,x+123,y+118)]:
            sheet.paste(im.resize((size,size),Image.Resampling.LANCZOS),(px,py),im.resize((size,size),Image.Resampling.LANCZOS))
    else:
        message='Rendered from\nthe actual model' if i==1 else 'Uses the native\nValheim portal icon'
        d.multiline_text((x+35,y+45),message,font=small,fill=white,align='center')
    d.text((x+105,y+165),label,font=font,fill=white,anchor='mt')
sheet.save(ROOT/'icon-preview.png')
(ROOT/'artwork-provenance.json').write_text(json.dumps({'mode':'Built-in imagegen for approved atlas and pillar artwork; code-drawn UI glyphs','sources':sources,'reference':'exec-70aa4a8c-21cf-44f6-860b-c83f4c6253e6.png','prompt':'Recreate each approved item alone with genuine transparency; preserve chunky faceted Valheim style. UI glyphs match the approved sheet.'},indent=2))
print('Exported 9 transparent sprites, including the restored illustrated pillar icon; portal pins use the native game symbol.')

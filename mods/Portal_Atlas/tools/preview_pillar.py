"""Shape/lettering preview from the exact exported mesh. Not game lighting/texturing."""
from pathlib import Path
import json,math,numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
data=json.loads((ROOT/'pillar-geometry.json').read_text())
runes=np.flipud(np.frombuffer((ROOT/'pillar-diffuse.rgba').read_bytes(),dtype=np.uint8).reshape(1152,384,4))
emission=np.flipud(np.frombuffer((ROOT/'pillar-emission.rgba').read_bytes(),dtype=np.uint8).reshape(1152,384,4))
native_path=ROOT/'native-stone-preview-only.png'
stone_texture=None
block_texture=np.array(Image.open(ROOT/'native-stone-preview-only.png').convert('RGBA'))
def render(yaw,width,height):
    a=math.radians(yaw);e=math.radians(12)
    right=np.array([math.cos(a),0,-math.sin(a)]);up=np.array([-math.sin(a)*math.sin(e),math.cos(e),-math.cos(a)*math.sin(e)])
    view=np.array([math.sin(a)*math.cos(e),math.sin(e),math.cos(a)*math.cos(e)])
    image=np.zeros((height,width,4),dtype=np.uint8);depth=np.full((height,width),-np.inf)
    scale=min(width/.78,height/1.95)
    for key,mesh in data.items():
        vertices=np.array(mesh['v']);uv=np.array(mesh['uv']);centred=vertices-np.array([0,.825,0]);z=centred@view
        projected=np.stack([width/2+centred@right*scale,height/2-centred@up*scale],axis=1)
        for tri in np.array(mesh['t']).reshape(-1,3):
            pts=vertices[tri];normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);normal/=np.linalg.norm(normal)
            if normal@view<=0:continue
            p=projected[tri];xmin=max(0,int(np.floor(p[:,0].min())));xmax=min(width-1,int(np.ceil(p[:,0].max())))
            ymin=max(0,int(np.floor(p[:,1].min())));ymax=min(height-1,int(np.ceil(p[:,1].max())))
            xx,yy=np.meshgrid(np.arange(xmin,xmax+1)+.5,np.arange(ymin,ymax+1)+.5)
            denominator=(p[1,1]-p[2,1])*(p[0,0]-p[2,0])+(p[2,0]-p[1,0])*(p[0,1]-p[2,1])
            if abs(denominator)<1e-8:continue
            w0=((p[1,1]-p[2,1])*(xx-p[2,0])+(p[2,0]-p[1,0])*(yy-p[2,1]))/denominator
            w1=((p[2,1]-p[0,1])*(xx-p[2,0])+(p[0,0]-p[2,0])*(yy-p[2,1]))/denominator;w2=1-w0-w1
            zz=w0*z[tri[0]]+w1*z[tri[1]]+w2*z[tri[2]]
            hit=(w0>=-1e-5)&(w1>=-1e-5)&(w2>=-1e-5)&(zz>depth[ymin:ymax+1,xmin:xmax+1])
            light=.5+.5*max(0,normal@np.array([.4,.8,.45]))
            color=np.array([110,102,97])
            coords=w0[:,:,None]*uv[tri[0]]+w1[:,:,None]*uv[tri[1]]+w2[:,:,None]*uv[tri[2]]
            pixels=None
            if key=='lettering':
                tx=np.clip((coords[:,:,0]*(runes.shape[1]-1)).astype(int),0,runes.shape[1]-1)
                ty=np.clip(((1-coords[:,:,1])*(runes.shape[0]-1)).astype(int),0,runes.shape[0]-1)
                hit&=runes[ty,tx,3]>64;color=np.array([255,125,30]);light=1
            elif stone_texture is not None:
                stone_uv=coords.copy()
                if key=='panels':
                    stone_uv[:,:,0]=.025+.45*((-.17+.34*coords[:,:,0])+.35)/.70
                    stone_uv[:,:,1]=.025+.225*(.25+.72*coords[:,:,1])/1.20
                tx=np.clip((stone_uv[:,:,0]*(stone_texture.shape[1]-1)).astype(int),0,stone_texture.shape[1]-1)
                ty=np.clip(((1-stone_uv[:,:,1])*(stone_texture.shape[0]-1)).astype(int),0,stone_texture.shape[0]-1)
                pixels=stone_texture[ty,tx].astype(float)
                if key=='panels':
                    rx=np.clip((coords[:,:,0]*(runes.shape[1]-1)).astype(int),0,runes.shape[1]-1)
                    ry=np.clip(((1-coords[:,:,1])*(runes.shape[0]-1)).astype(int),0,runes.shape[0]-1)
                    mask=runes[ry,rx,3:4].astype(float)/255
                    pixels=(pixels*.45*(1-mask)+np.array([155,56,9])*mask)*light+np.array([168,48,4])*mask
                else:pixels*=light
            # Same painted stone + rune pixels used by the actual material.
            if key=='panels':
                rx=np.clip((coords[:,:,0]*(runes.shape[1]-1)).astype(int),0,runes.shape[1]-1)
                ry=np.clip(((1-coords[:,:,1])*(runes.shape[0]-1)).astype(int),0,runes.shape[0]-1)
                paint=runes[ry,rx,:3].astype(float)
                pixels=paint*light+emission[ry,rx,:3].astype(float)*.28
            else:
                rx=np.clip((coords[:,:,0]*(block_texture.shape[1]-1)).astype(int),0,block_texture.shape[1]-1)
                ry=np.clip(((1-coords[:,:,1])*(block_texture.shape[0]-1)).astype(int),0,block_texture.shape[0]-1)
                pixels=block_texture[ry,rx,:3].astype(float)*light
            region=image[ymin:ymax+1,xmin:xmax+1]
            region[hit,:3]=np.clip(pixels[hit] if pixels is not None else color*light,0,255).astype('uint8');region[hit,3]=255
            depth[ymin:ymax+1,xmin:xmax+1][hit]=zz[hit]
    return Image.fromarray(image)
sheet=Image.new('RGB',(900,510),'#222523');d=ImageDraw.Draw(sheet)
title=ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',22);font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',16)
d.text((24,14),'SLIM PILLAR — LETTERING BAKED INTO THE STONE',font=title,fill='#eee7d8')
for i,(angle,label) in enumerate([(35,'Front / right'),(215,'Back / left'),(0,'Front')]):
    im=render(angle,280,360);sheet.paste(im,(10+i*300,58),im);d.text((150+i*300,435),label,font=font,fill='#eee7d8',anchor='mt')
d.text((24,475),'1.65 m tall • 20% narrower • actual faceted geometry and materials, with preview lighting',font=font,fill='#d5c8b2')
sheet.save(ROOT/'pillar-revision-preview.png')
render(35,128,128).save(ROOT/'pillar-shape-preview.png')
game=Image.new('RGB',(900,660),'#252a28');g=ImageDraw.Draw(game)
g.text((28,20),'REVISED PILLAR + BUILD-MENU PREVIEW',font=title,fill='#eee7d8')
large=render(35,460,520);game.paste(large,(12,60),large)
g.text((242,595),'1.65 m tall · 20% slimmer · engraved faces',font=font,fill='#eee7d8',anchor='mt')
g.text((640,95),'Build-menu size',font=title,fill='#eee7d8',anchor='mt')
tile=Image.new('RGBA',(80,80),(87,88,85,255));td=ImageDraw.Draw(tile);td.rectangle((0,0,79,79),outline=(155,158,151),width=1)
small=Image.open(ROOT/'assets/pillar.png').convert('RGBA').resize((72,72),Image.Resampling.LANCZOS);tile.alpha_composite(small,(4,4));game.paste(tile,(600,145))
g.text((640,238),'80 px tile / 72 px icon',font=font,fill='#d5c8b2',anchor='mt')
game.paste(tile.resize((200,200),Image.Resampling.NEAREST),(540,300))
g.text((640,520),'Enlarged for inspection',font=font,fill='#d5c8b2',anchor='mt')
g.text((28,635),'Actual faceted mesh and matte stone material; preview lighting, not an in-game screenshot.',font=font,fill='#d5c8b2')
game.save(ROOT/'pillar-and-build-menu-preview.png')
print('Rendered exact geometry from the built DLL, including all four rune panels.')

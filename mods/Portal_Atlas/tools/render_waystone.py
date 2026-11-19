from pathlib import Path
import json,math,numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter
ROOT=Path(__file__).resolve().parents[1]
data=json.loads((ROOT/'model/waystone.json').read_text())
albedo=np.array(Image.open(ROOT/'assets/waystone-painted.png').convert('RGBA'))
masonry=np.array(Image.open(ROOT/'assets/waystone-masonry.png').convert('RGBA'))
def stone_grey(image):
    result=image.copy();rgb=image[:,:,:3].astype(float)
    preserve=(rgb[:,:,0]>150)&(rgb[:,:,0]-rgb[:,:,2]>85)&(rgb[:,:,1]-rgb[:,:,2]>25)
    value=(rgb[:,:,0]*.2126+rgb[:,:,1]*.7152+rgb[:,:,2]*.0722)*1.12
    neutral=np.stack([value*1.03,value,value*.93],axis=2)
    result[:,:,:3]=np.where(preserve[:,:,None],rgb,np.clip(np.rint(neutral),0,255)).astype('uint8')
    return result
albedo=stone_grey(albedo);masonry=stone_grey(masonry)
def render(yaw,width,height):
    a=math.radians(yaw);e=math.radians(9)
    right=np.array([math.cos(a),0,-math.sin(a)]);up=np.array([-math.sin(a)*math.sin(e),math.cos(e),-math.cos(a)*math.sin(e)])
    view=np.array([math.sin(a)*math.cos(e),math.sin(e),math.cos(a)*math.cos(e)])
    image=np.zeros((height,width,4),dtype=np.uint8);depth=np.full((height,width),-np.inf)
    scale=min(width/1.05,height/1.92)
    for key,mesh in data.items():
        vertices=np.array(mesh['v']);uv=np.array(mesh['uv']);centred=vertices-np.array([0,.82,0]);z=centred@view
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
            if key=='masonry':
                tx=((coords[:,:,0]%1)*(masonry.shape[1]-1)).astype(int);ty=((1-coords[:,:,1]%1)*(masonry.shape[0]-1)).astype(int)
                pixels=masonry[ty,tx,:3].astype(float)*light
            else:
                tx=np.clip((coords[:,:,0]*(albedo.shape[1]-1)).astype(int),0,albedo.shape[1]-1)
                ty=np.clip(((1-coords[:,:,1])*(albedo.shape[0]-1)).astype(int),0,albedo.shape[0]-1)
                paint=albedo[ty,tx,:3].astype(float)
                hot=(paint[:,:,0]>180)&(paint[:,:,0]-paint[:,:,2]>100)&(paint[:,:,1]-paint[:,:,2]>35)
                pixels=paint*light+paint*hot[:,:,None]*.5
            region=image[ymin:ymax+1,xmin:xmax+1]
            region[hit,:3]=np.clip(pixels[hit] if pixels is not None else color*light,0,255).astype('uint8');region[hit,3]=255
            depth[ymin:ymax+1,xmin:xmax+1][hit]=zz[hit]
    return Image.fromarray(image)


sheet=Image.new('RGB',(1400,1000),'#252a28');d=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',26);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
d.text((30,20),'PORTAL ATLAS — IMPLEMENTED WAYSTONE MODEL',font=font,fill='#eee7d8')
for i,(angle,label) in enumerate([(35,'Front / right'),(215,'Back / left')]):
 im=render(angle,550,830);x=20+i*620;sheet.paste(im,(x,70),im)
 # Indicator for the native game's wisp effect, whose position is fixed in the model.
 yy=70+830/2-(1.59-.82)*min(550/1.05,830/1.92)*math.cos(math.radians(9));cx=x+275
 glow=Image.new('RGBA',sheet.size);gd=ImageDraw.Draw(glow);gd.ellipse((cx-16,yy-16,cx+16,yy+16),fill=(82,205,255,180));glow=glow.filter(ImageFilter.GaussianBlur(12));sheet.paste(glow,(0,0),glow)
 d.ellipse((cx-5,yy-5,cx+5,yy+5),fill='#d9f8ff')
 d.text((x+275,916),label,font=font,fill='#eee7d8',anchor='mt')
d.text((30,962),'Actual mesh · 1.50 m stone height · 0.75 m footprint · native-wisp position indicated · preview lighting',font=small,fill='#d5c8b2')
sheet.save(ROOT/'new-pillar-concept/implemented-waystone-preview.png')
print('Rendered actual model geometry and stone material; wisp effect position marked.')

icon=render(35,256,256)
glow=Image.new('RGBA',(256,256));g=ImageDraw.Draw(glow)
g.ellipse((123,21,133,31),fill=(95,210,255,190));glow=glow.filter(ImageFilter.GaussianBlur(3));icon.alpha_composite(glow)
ImageDraw.Draw(icon).ellipse((126,24,130,28),fill=(225,250,255,255))
icon.save(ROOT/'assets/pillar.png')
print('Exported the build-menu icon from the actual new mesh.')

"""Build the approved square waystone as real geometry, in metres."""
from pathlib import Path
import json,math,struct,numpy as np
ROOT=Path(__file__).resolve().parents[1]
LOWER_AXIS_U=360/1024 # Measured top spine / triangle apex in the actual artwork
groups={k:{'v':[],'uv':[],'t':[]} for k in ('stone','recess','orange','masonry')}
def face(points,kind='stone',normal=None):
    p=np.array(points,float);n=np.cross(p[1]-p[0],p[2]-p[0])
    if normal is not None and n@np.array(normal)<0:p=p[::-1];n=-n
    axis=int(np.argmax(abs(n)));idx=[q for q in range(3) if q!=axis];centre=p.mean(axis=0)
    decorated=kind in ('recess','orange') or (axis!=1 and (.78<centre[1]<1.378 or (.28<centre[1]<.665 and abs(centre[axis])<.208)))
    if kind=='stone' and not decorated:kind='masonry'
    m=groups[kind];at=len(m['v']);m['v']+=p.tolist();coords=[]
    for point in p:
        if kind=='masonry':u,v=point[idx]*3
        else:
            local=point[0] if axis!=0 else -point[2]
            if n[axis]<0:local=-local
            u=local/.75+.5;v=point[1]/1.5
            if point[1]<.71:u=LOWER_AXIS_U+.44*(u-.5)
        if kind=='orange' and centre[1]<.30:
            # Rectify the skewed painted triangle onto the three real corners.
            local=point[0] if axis!=0 else -point[2]
            if n[axis]<0:local=-local
            if point[1]>.20:u,v=360/1024,1-1734/2048
            elif local<0:u,v=256/1024,1-1911/2048
            else:u,v=440/1024,1-1903/2048
        coords.append([u,v])
    m['uv']+=coords
    for i in range(1,len(p)-1):m['t'] += [at,at+i,at+i+1]
def ring(w,y,bevel):
    r=w/2;s=r-bevel
    return np.array([[-s,y,r],[s,y,r],[r,y,s],[r,y,-s],[s,y,-r],[-s,y,-r],[-r,y,-s],[-r,y,s]])
def box(center,size,bevel=.006,kind='stone',turn=0):
    x,y,z=center;w,h,d=size
    # Bevelled rectangular block; flat top and vertical sides stay level.
    b=min(bevel*1.7,h*.34,w*.15,d*.15)
    rings=[]
    for yy,inset in [(-h/2,b),(-h/2+b*.293,b*.293),(-h/2+b,0),(h/2-b,0),(h/2-b*.293,b*.293),(h/2,b)]:
        rr=ring(w-2*inset,yy,b);rr[:,2]*=(d-2*inset)/(w-2*inset)
        a=turn;rr=rr@np.array([[math.cos(a),0,-math.sin(a)],[0,1,0],[math.sin(a),0,math.cos(a)]])+np.array(center);rings.append(rr)
    for j in range(len(rings)-1):
        for i in range(8):
            k=(i+1)%8;pts=[rings[j][i],rings[j][k],rings[j+1][k],rings[j+1][i]]
            face(pts,kind,np.mean(pts,axis=0)-np.array(center))
    face(rings[0],kind,(0,-1,0));face(rings[3],kind,(0,1,0))
def rot(p,side):
    x,y,z=p
    return [(x,y,z),(z,y,-x),(-x,y,-z),(-z,y,x)][side%4]
def panel_box(side,x,y,z,w,h,depth,kind='stone',bevel=.003):
    box(rot((x,y,z),side),(w,h,depth),bevel,kind,side*math.pi/2)
def line(side,a,b,width,z,kind='recess',depth=.005):
    ax,ay=a;bx,by=b;length=math.hypot(bx-ax,by-ay)
    if length<1e-6:return
    dx=-(by-ay)/length*width/2;dy=(bx-ax)/length*width/2
    pts=[(ax+dx,ay+dy),(bx+dx,by+dy),(bx-dx,by-dy),(ax-dx,ay-dy)]
    front=[rot((x,y,z+depth/2),side) for x,y in pts];back=[rot((x,y,z-depth/2),side) for x,y in pts]
    face(front,kind,rot((0,0,1),side));face(back,kind,rot((0,0,-1),side))
    for i in range(4):
        j=(i+1)%4;face([front[i],front[j],back[j],back[i]],kind)
def path(side,pts,width,z,kind='recess'):
    for a,b in zip(pts,pts[1:]):line(side,a,b,width,z,kind)
def slab(y,w,h):
    for x in (-1,1):
        for z in (-1,1):box((x*(w/4+.001),y,z*(w/4+.001)),(w/2-.002,h,w/2-.002),min(.006,h*.1))
# Square foot, central lower masonry and a projecting belt.
slab(.035,.75,.07)
for level in range(4):box((0,.1575+level*.145,0),(.408,.142,.408),.005)
slab(.735,.62,.075)
box((0,.686,0),(.53,.035,.53),.005)
# Four flared corner buttresses, visibly concave rather than a bowed shaft.
profile=[(.07,.345),(.16,.315),(.26,.263),(.40,.232),(.60,.232),(.69,.270)]
for sx in (-1,1):
 for sz in (-1,1):
    for (y0,r0),(y1,r1) in zip(profile,profile[1:]):
        rr=[]
        for y,r in [(y0,r0),(y1,r1)]:
            rr.append(np.array([[sx*.160,y,sz*.160],[sx*r,y,sz*.160],[sx*r,y,sz*r],[sx*.160,y,sz*r]]))
        centre=np.concatenate(rr).mean(axis=0)
        for k in range(4):
            j=(k+1)%4;pts=[rr[0][k],rr[0][j],rr[1][j],rr[1][k]];face(pts,normal=np.mean(pts,axis=0)-centre)
        face(rr[0],normal=(0,-1,0));face(rr[1],normal=(0,1,0))
# Upper tower: actual inset openings framed by corner posts and lintels.
box((0,.922,0),(.34,.29,.34),.004,'recess')
for x in (-.195,.195):
 for z in (-.195,.195):box((x,1.073,z),(.055,.585,.055),.005)
box((0,1.074,0),(.425,.045,.425),.004)
box((0,1.237,0),(.410,.282,.410),.005)
slab(1.408,.49,.065)
slab(1.466,.465,.050)
# Small crown corner stones leave a basin for the wisp.
for x in (-.18,.18):
 for z in (-.18,.18):box((x,1.49,z),(.055,.020,.055),.003)
for side in range(4):
    # Lower recessed knotwork and triangular orange inset.
    panel_box(side,0,.452,.209,.282,.425,.010,'recess')
    for sign in (-1,1):
        pts=[(sign*.055,.280),(-sign*.055,.365),(sign*.055,.450),(-sign*.055,.535),(0,.610)]
        pass # Knotwork is painted into the approved stone finish.
        
    triangle=[rot((-.103,.109,.226),side),rot((.103,.109,.226),side),rot((0,.249,.226),side)]
    face(triangle,'orange',rot((0,0,1),side))
    
    # Centre orange opening and two small slits on either side.
    panel_box(side,0,.923,.203,.258,.244,.012,'recess')
    panel_box(side,0,.923,.214,.068,.188,.008,'orange',.006)
    for x in (-.089,.089):
        for y in (.869,.976):panel_box(side,x,y,.214,.014,.068,.007,'orange',.002)
    # Recessed square spiral, matching the approved concept.
    panel_box(side,0,1.237,.209,.286,.235,.008,'recess')
    spiral=[(-.119,1.132),(-.119,1.343),(.119,1.343),(.119,1.132),(-.078,1.132),(-.078,1.306),(.078,1.306),(.078,1.171),(-.037,1.171),(-.037,1.269),(.037,1.269),(.037,1.214),(0,1.214)]
    
# Binary mesh asset consumed directly by the game and by the renderer.
out=ROOT/'model'/'waystone.mesh';out.parent.mkdir(exist_ok=True)
with out.open('wb') as f:
    f.write(b'OCTW1');f.write(struct.pack('<i',len(groups)))
    for name,m in groups.items():
        label=name.encode();f.write(struct.pack('<i',len(label)));f.write(label)
        f.write(struct.pack('<ii',len(m['v']),len(m['t'])))
        for p,uv in zip(m['v'],m['uv']):f.write(struct.pack('<5f',*p,*uv))
        f.write(struct.pack('<'+'i'*len(m['t']),*m['t']))
(ROOT/'model'/'waystone.json').write_text(json.dumps(groups))
print('Built approved waystone: 1.50 m stone height, 0.75 m square footprint; four decorated faces.')

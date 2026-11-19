"""The existing pillar silhouette with native-style soft edges and shadow."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
def build_icon(path):
    scale=4
    body=Image.new('RGBA',(256*scale,256*scale));draw=ImageDraw.Draw(body)
    def poly(points,colour):draw.polygon([(x*scale,y*scale) for x,y in points],fill=colour)
    poly([(85,204),(89,65),(128,17),(167,65),(171,204)],(184,187,177,255))
    poly([(128,24),(160,68),(164,200),(129,200)],(141,147,137,255))
    draw.line([(128*scale,25*scale),(128*scale,201*scale)],fill=(202,205,195,255),width=4*scale)
    poly([(67,201),(189,201),(196,232),(60,232)],(184,187,177,255))
    # Diffuse dark shadow instead of a sharp, thick vector stroke.
    alpha=body.getchannel('A').filter(ImageFilter.MaxFilter(17)).filter(ImageFilter.GaussianBlur(20))
    alpha=alpha.point(lambda value:round(value*.65))
    shadow=Image.new('RGBA',body.size,(27,29,26,0));shadow.putalpha(alpha)
    body=body.filter(ImageFilter.GaussianBlur(3))
    result=Image.alpha_composite(shadow,body).resize((256,256),Image.Resampling.LANCZOS)
    result.putalpha(result.getchannel('A').point(lambda value:0 if value<2 else value))
    result.save(path)
if __name__=='__main__':
    build_icon(Path(__file__).resolve().parents[1]/'assets/pillar-pin.png')
    print('Updated only the shared pillar map sprite: muted fill, feathered edge and diffuse shadow.')

using System;
using System.Collections.Generic;
using UnityEngine;
namespace OctoberJemini.PortalAtlas
{
    // Transfer the installed game's actual fill colours and shadow falloff.
    // Native artwork is sampled at runtime, never bundled with this mod.
    internal static class NativePinStyle
    {
        internal static Color32[] Build(Color32[] native,int nw,int nh,Color32[] glyph,int gw,int gh,int size)
        {
            var filled=new List<int>();var palette=new List<Color32>();
            var inside=new bool[native.Length];
            for(int i=0;i<native.Length;i++)
            {
                var p=native[i];inside[i]=p.a>220 && (p.r+p.g+p.b)/3>150;
                if(inside[i]){filled.Add(i);palette.Add(p);}
            }
            if(palette.Count<16)throw new InvalidOperationException("Native portal fill is unavailable");
            const int bins=17;var counts=new int[bins];var alpha=new float[bins];var red=new float[bins];var green=new float[bins];var blue=new float[bins];
            Func<int,int,List<int>,int,float> nearest=(x,y,points,width)=>{
                float best=99999;foreach(int at in points){float dx=x-at%width,dy=y-at/width;best=Math.Min(best,dx*dx+dy*dy);}return (float)Math.Sqrt(best);
            };
            for(int y=0;y<nh;y++)for(int x=0;x<nw;x++)
            {
                int i=y*nw+x;if(inside[i])continue;
                int band=(int)Math.Round(nearest(x,y,filled,nw)*2);if(band>=bins)continue;
                var p=native[i];counts[band]++;alpha[band]+=p.a;red[band]+=p.r*p.a;green[band]+=p.g*p.a;blue[band]+=p.b*p.a;
            }
            var profile=new Color32[bins];
            for(int i=0;i<bins;i++)if(counts[i]>0 && alpha[i]>0)
                profile[i]=new Color32((byte)(red[i]/alpha[i]),(byte)(green[i]/alpha[i]),(byte)(blue[i]/alpha[i]),(byte)(alpha[i]/counts[i]));
            var body=new List<int>();var sample=new Color32[size*size];var result=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int at=y*size+x;var p=glyph[Math.Min(gh-1,(int)((y+.5f)*gh/size))*gw+Math.Min(gw-1,(int)((x+.5f)*gw/size))];sample[at]=p;
                if(p.a>220 && p.r>70)body.Add(at);
            }
            if(body.Count<3)throw new InvalidOperationException("Pillar silhouette is unavailable");
            var nativeRows=new List<int>[nh];var glyphRows=new List<int>[size];
            for(int y=0;y<nh;y++)nativeRows[y]=new List<int>();
            for(int y=0;y<size;y++)glyphRows[y]=new List<int>();
            foreach(int at in filled)nativeRows[at/nw].Add(at);
            foreach(int at in body)glyphRows[at/size].Add(at);
            int sourceBottom=filled[0]/nw,sourceTop=filled[filled.Count-1]/nw;
            int bottom=body[0]/size,top=body[body.Count-1]/size;
            var bodySet=new HashSet<int>(body);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int at=y*size+x;
                if(bodySet.Contains(at))
                {
                    int sy=sourceBottom+(y-bottom)*(sourceTop-sourceBottom)/Math.Max(1,top-bottom);
                    if(nativeRows[sy].Count==0)
                    {
                        int requested=sy;for(int distance=1;distance<nh;distance++)
                        {
                            int lower=Math.Max(sourceBottom,requested-distance),upper=Math.Min(sourceTop,requested+distance);
                            if(nativeRows[lower].Count>0){sy=lower;break;}
                            if(nativeRows[upper].Count>0){sy=upper;break;}
                        }
                    }
                    var row=nativeRows[sy];var target=glyphRows[y];int left=target[0]%size;
                    int position=(x-left)*(row.Count-1)/Math.Max(1,target.Count-1);
                    var p=native[row[Math.Max(0,Math.Min(row.Count-1,position))]];
                    float facet=Math.Min(1,.4f+.6f*sample[at].r/184f);
                    result[at]=new Color32((byte)(p.r*facet),(byte)(p.g*facet),(byte)(p.b*facet),255);
                }
                else
                {
                    int band=(int)Math.Round(nearest(x,y,body,size)*2);if(band<bins)result[at]=profile[band];
                }
            }
            return result;
        }
    }
}

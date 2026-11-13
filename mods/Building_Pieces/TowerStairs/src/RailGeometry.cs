using System;
using System.Collections.Generic;
namespace OctoberJemini.TowerStairs {
 public struct RailBeam {
  public Point A,B; public double Size,TextureStart;
  public RailBeam(Point a,Point b,double size,double textureStart=0){A=a;B=b;Size=size;TextureStart=textureStart;}
 }
 public static class RailGeometry {
  public const double Height=1.0;
  public static Point Path(StairLayout l,int dir,bool free,bool curved,double t,double height) {
   if(!curved)return l.Edge(t,free,t*l.Rise+height,dir);
   double apothem=l.Length/(2*Math.Tan(l.Angle/2));
   double offset=free?(l.Inside?-l.Width:l.Width):0;
   double radius=(apothem+offset)/Math.Cos(l.Angle/2),a=(t-.5)*l.Angle;
   return new Point(dir*(l.Length/2+radius*Math.Sin(a)),t*l.Rise+height,-apothem+radius*Math.Cos(a));
  }
  public static List<RailBeam> Build(StairLayout l,int dir,bool free,bool curved) {
   var beams=new List<RailBeam>();int spans=curved?12:1;
   foreach(double height in new[]{.45,Height}) {
    double distance=height==Height?.613:.217;
    for(int i=0;i<spans;i++) {
     var a=Path(l,dir,free,curved,(double)i/spans,height);
     var b=Path(l,dir,free,curved,(double)(i+1)/spans,height);
     beams.Add(new RailBeam(a,b,height==Height?.20:.14,distance));
     distance+=Math.Sqrt(Math.Pow(b.X-a.X,2)+Math.Pow(b.Y-a.Y,2)+Math.Pow(b.Z-a.Z,2));
    }
   }
   var start=l.Edge(0,free,0,dir);var end=l.Edge(1,free,0,dir);
   double run=Math.Sqrt(Math.Pow(end.X-start.X,2)+Math.Pow(end.Z-start.Z,2));
   int posts=Math.Max(1,(int)Math.Ceiling(run/.75));
   for(int i=0;i<=posts;i++) {
    double t=(double)i/posts;
    // Start feet embed into the first tread; curved posts lean gently from
    // the actual polygon edge to the curved handrail above it.
    double foot=t==0?0:Math.Ceiling(t*l.Steps-1e-8)*l.Rise/l.Steps-.05;
    beams.Add(new RailBeam(l.Edge(t,free,foot,dir),Path(l,dir,free,curved,t,Height),.18,.131+i*.371));
   }
   return beams;
  }
 }
}

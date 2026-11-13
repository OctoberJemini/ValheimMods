using System;
using System.Collections.Generic;

namespace OctoberJemini.TowerStairs
{
    // Pure geometry: also compiled into the offline validation program.
    public struct Point
    {
        public double X, Y, Z;
        public Point(double x, double y, double z) { X=x; Y=y; Z=z; }
    }
    public sealed class StairGeometry
    {
        public const double Radius=4, Width=2, Rise=1, Angle=Math.PI/8, Thickness=0.3;
        public const int Steps=5, Divisions=3;
        public readonly List<Point> Vertices = new List<Point>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Point> UVs = new List<Point>();

        // The inner edge follows the chord of a 16-sided wall, not an arc
        // that would leave a crescent-shaped gap against straight wall pieces.
        public static Point Edge(double t, bool outer, double y, int direction)
        {
            return outer
                ? new Point(direction*(Radius+Width)*Math.Sin(Angle*t), y, (Radius+Width)*Math.Cos(Angle*t)-Radius)
                : new Point(direction*Radius*Math.Sin(Angle)*t, y, Radius*(Math.Cos(Angle)-1)*t);
        }

        internal void Quad(Point a, Point b, Point c, Point d, Point outward)
        {
            double ux=b.X-a.X, uy=b.Y-a.Y, uz=b.Z-a.Z;
            double vx=c.X-a.X, vy=c.Y-a.Y, vz=c.Z-a.Z;
            double nx=uy*vz-uz*vy, ny=uz*vx-ux*vz, nz=ux*vy-uy*vx;
            if (nx*outward.X+ny*outward.Y+nz*outward.Z<0) { var swap=b; b=d; d=swap; }
            int start=Vertices.Count;
            // The vanilla stone texture is an atlas, not a repeating tile.
            // Use only its solid lower-left stone panel (Unity UV origin is bottom-left).
            bool horizontal=Math.Abs(outward.Y)>0.5;
            bool useZ=Math.Abs(outward.X)>Math.Abs(outward.Z);
            foreach(var p in new[]{a,b,c,d})
            {
                Vertices.Add(p);
                double u=horizontal?Math.Abs(p.X):(useZ?p.Z+0.5:Math.Abs(p.X));
                double v=horizontal?p.Z+0.5:p.Y+Thickness;
                UVs.Add(new Point(0.025+u*0.25,0.025+v*0.125,0));
            }
            Triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }

        public static StairGeometry Build(int direction)
        {
            if(direction!=1 && direction!=-1) throw new ArgumentException("Direction must be +1 or -1.");
            var g=new StairGeometry();
            for(int step=0;step<Steps;step++)
            {
                double top=(step+1)*Rise/Steps;
                for(int sub=0;sub<Divisions;sub++)
                {
                    double t0=(step+(double)sub/Divisions)/Steps;
                    double t1=(step+(double)(sub+1)/Divisions)/Steps;
                    var a=Edge(t0,false,top,direction); var b=Edge(t0,true,top,direction);
                    var c=Edge(t1,true,top,direction); var d=Edge(t1,false,top,direction);
                    var e=Edge(t0,false,t0*Rise-Thickness,direction); var f=Edge(t0,true,t0*Rise-Thickness,direction);
                    var h=Edge(t1,false,t1*Rise-Thickness,direction); var i=Edge(t1,true,t1*Rise-Thickness,direction);
                    double mid=Angle*(t0+t1)/2;
                    var radial=new Point(direction*Math.Sin(mid),0,Math.Cos(mid));
                    g.Quad(a,b,c,d,new Point(0,1,0));
                    g.Quad(e,h,i,f,new Point(0,-1,0));
                    g.Quad(a,d,h,e,new Point(-radial.X,0,-radial.Z));
                    g.Quad(b,f,i,c,radial);
                    // Closed wedge cells give the physics mesh a solid underside.
                    g.Quad(a,e,f,b,new Point(-direction*Math.Cos(Angle*t0),0,Math.Sin(Angle*t0)));
                    g.Quad(d,c,i,h,new Point(direction*Math.Cos(Angle*t1),0,-Math.Sin(Angle*t1)));
                }
            }
            return g;
        }
    }
}

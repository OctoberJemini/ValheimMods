using System;

namespace OctoberJemini.TowerStairs

{

    public sealed class StairLayout

    {

        public string Id, Label;

        public double Length, Angle, Width, Rise;

        public int Steps;

        public bool Inside, Legacy, SmoothWalking;

        public static StairLayout[] All() {

            var list=new System.Collections.Generic.List<StairLayout>();

            list.Add(new StairLayout {Id="r4_outer",Label="R4",Legacy=true,Rise=1,Angle=Math.PI/8});

            foreach(bool small in new[]{true,false}) foreach(bool inside in new[]{false,true})

                list.Add(new StairLayout {Id=(small?"pole1_turn2_":"pole2_turn1_")+(inside?"inside":"outside"),

                    Label=(small?"2m / 2 clicks":"2m / 1 click")+(inside?" Inside":" Outside"),

                    Length=2,Angle=small?Math.PI/4:Math.PI/8,Width=inside&&small?1:2,

                    Rise=small&&inside?0.4:(small?0.6:1),Steps=small&&inside?2:(small?3:5),Inside=inside});

            // Keep the narrow inside option and add a separate wide piece.

            foreach(var layout in list)

                if(layout.Id=="pole1_turn2_inside") layout.Label="2m / 2 clicks Inside 1m wide";

            list.Add(new StairLayout {Id="pole2_turn2_inside_wide",Label="2m / 2 clicks Inside 2m wide",

                Length=2,Angle=Math.PI/4,Width=2,Rise=0.4,Steps=2,Inside=true});

            // Distinct prefab IDs preserve all previously placed 2m pieces.

            foreach(bool inside in new[]{false,true})

                list.Add(new StairLayout {Id="pole1_turn2_restored_"+(inside?"inside":"outside"),

                    Label="1m / 2 clicks "+(inside?"Inside":"Outside"),

                    Length=1,Angle=Math.PI/4,Width=inside?1:2,

                    Rise=inside?0.4:0.6,Steps=inside?2:3,Inside=inside});

            foreach(var layout in list)

                if(!layout.Legacy && layout.Length==2 && Math.Abs(layout.Angle-Math.PI/4)<1e-9) {

                    layout.Rise=1; layout.Steps=8; layout.SmoothWalking=true;

                }

            foreach(var layout in list) {

                if(!layout.Legacy && layout.Length==2)

                    layout.Label="2M-"+(layout.Angle>0.5?"45\u00b0":"22.5\u00b0")+" "+(layout.Inside?"Inner":"Outer")+(layout.Inside&&layout.Width==1?" Narrow":"");

            }

            list.Sort((a,b)=> {

                int activeA=!a.Legacy&&a.Length==2?0:1,activeB=!b.Legacy&&b.Length==2?0:1;

                int cmp=activeA.CompareTo(activeB);if(cmp!=0)return cmp;

                cmp=b.Angle.CompareTo(a.Angle);if(cmp!=0)return cmp;

                cmp=b.Inside.CompareTo(a.Inside);if(cmp!=0)return cmp;

                return a.Width.CompareTo(b.Width);

            });

            return list.ToArray();

        }

        // Wall edge is the pole centreline, aligned with local X. Free edge is

        // a parallel polygon offset; mitred ends meet after the specified turn.

        public Point Edge(double t,bool free,double y,int direction) {

            if(Legacy) return StairGeometry.Edge(t,free,y,direction);

            double offset=free?(Inside?-Width:Width):0;

            double bevel=offset*Math.Tan(Angle/2);

            return new Point(direction*(-bevel+t*(Length+2*bevel)),y,offset);

        }

        public StairGeometry Build(int direction,bool walkingCollision=false) {

            if(Legacy) return StairGeometry.Build(direction);

            if(direction!=1 && direction!=-1) throw new ArgumentException("Invalid direction");

            if(Inside && Width>=Length/(2*Math.Tan(Angle/2))) throw new ArgumentException("Stairs reach circle centre");

            var g=new StairGeometry();

            for(int step=0;step<Steps;step++) {

                double t0=(double)step/Steps,t1=(double)(step+1)/Steps,top=t1*Rise;

                double front=walkingCollision&&SmoothWalking?t0*Rise:top;

                var a=Edge(t0,false,front,direction);var b=Edge(t0,true,front,direction);

                var c=Edge(t1,true,top,direction);var d=Edge(t1,false,top,direction);

                var e=Edge(t0,false,t0*Rise-StairGeometry.Thickness,direction);var f=Edge(t0,true,t0*Rise-StairGeometry.Thickness,direction);

                var h=Edge(t1,false,t1*Rise-StairGeometry.Thickness,direction);var i=Edge(t1,true,t1*Rise-StairGeometry.Thickness,direction);

                double sign=Inside?-1:1;

                g.Quad(a,b,c,d,new Point(0,1,0));g.Quad(e,h,i,f,new Point(0,-1,0));

                g.Quad(a,d,h,e,new Point(0,0,-sign));g.Quad(b,f,i,c,new Point(0,0,sign));

                g.Quad(a,e,f,b,new Point(-direction,0,0));g.Quad(d,c,i,h,new Point(direction,0,0));

            }

            // Map every face into the atlas's filled stone panel, including negative-Z interiors.

            double minX=double.MaxValue,maxX=double.MinValue,minZ=double.MaxValue,maxZ=double.MinValue;

            foreach(var p in g.Vertices) {minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minZ=Math.Min(minZ,p.Z);maxZ=Math.Max(maxZ,p.Z);}

            double scale=Math.Min(0.62/Math.Max(maxX-minX,maxZ-minZ),0.29/Math.Max(maxZ-minZ,Rise+StairGeometry.Thickness));

            for(int q=0;q<g.Vertices.Count;q+=4) {

                var a=g.Vertices[q];var b=g.Vertices[q+1];var c=g.Vertices[q+2];

                double nx=(b.Y-a.Y)*(c.Z-a.Z)-(b.Z-a.Z)*(c.Y-a.Y),ny=(b.Z-a.Z)*(c.X-a.X)-(b.X-a.X)*(c.Z-a.Z),nz=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);

                bool horizontal=Math.Abs(ny)>Math.Max(Math.Abs(nx),Math.Abs(nz));

                for(int j=0;j<4;j++) {var p=g.Vertices[q+j];double u=Math.Abs(nx)>Math.Abs(nz)&&!horizontal?p.Z-minZ:p.X-minX;double v=horizontal?p.Z-minZ:p.Y+StairGeometry.Thickness;g.UVs[q+j]=new Point(0.025+u*scale,0.025+v*scale,0);}

            }

            return g;

        }

    }

}

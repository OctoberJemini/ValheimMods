using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace OctoberJemini.PortalAtlas
{
    internal sealed class WaystonePart
    {
        internal string name;
        internal Vector3[] vertices;
        internal Vector2[] uv;
        internal int[] triangles;
    }
    internal static class WaystoneModel
    {
        internal const float Height=1.50f,Footprint=.75f,CrownWispHeight=1.59f;
        internal const float SparkSpeedMin=.70f,SparkSpeedMax=.85f,SparkLifetimeMin=.80f,SparkLifetimeMax=.95f;
        internal const float SparkCloneScale=.5f,SparkSpawnRadius=.06f;
        internal static float SparkMaximumReach(){return SparkCloneScale*(SparkSpawnRadius+SparkSpeedMax*SparkLifetimeMax);}
        internal static Vector3 CrownPosition(float verticalOffset)
        {return new Vector3(0,CrownWispHeight+Math.Max(-.02f,Math.Min(.02f,verticalOffset)),0);}
        internal static WaystonePart[] Read(Stream stream)
        {
            using(var r=new BinaryReader(stream,Encoding.UTF8,true))
            {
                if(Encoding.ASCII.GetString(r.ReadBytes(5))!="OCTW1")throw new InvalidDataException("Invalid Atlas waystone mesh");
                int count=r.ReadInt32();if(count!=5)throw new InvalidDataException("Waystone must have five materials");
                var parts=new WaystonePart[count];
                for(int at=0;at<count;at++)
                {
                    int length=r.ReadInt32();if(length<1 || length>32)throw new InvalidDataException("Invalid mesh part name");
                    var p=new WaystonePart{name=Encoding.UTF8.GetString(r.ReadBytes(length))};
                    int n=r.ReadInt32(),t=r.ReadInt32();
                    if(n<3 || n>60000 || t<3 || t>200000 || t%3!=0)throw new InvalidDataException("Invalid mesh counts");
                    p.vertices=new Vector3[n];p.uv=new Vector2[n];p.triangles=new int[t];
                    for(int i=0;i<n;i++){p.vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());p.uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
                    for(int i=0;i<t;i++){p.triangles[i]=r.ReadInt32();if(p.triangles[i]<0 || p.triangles[i]>=n)throw new InvalidDataException("Invalid mesh index");}
                    parts[at]=p;
                }
                return parts;
            }
        }
    }
}

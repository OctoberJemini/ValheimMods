using System;

namespace OctoberJemini.RoofCorners45 {
    public static class CornerGeometry {
        // Vanilla outer corners have a 2m run, a slope-specific rise and
        // eaves meeting at (1,0,1). Retain the pitch and perpendicular
        // joins to rectangular roof panels while changing the turn to 45.
        // A full affine transform keeps triangles and convex colliders planar.
        public static void Map(double x,double y,double z,double rise,out double nx,out double ny,out double nz) {
            if(rise<=0)throw new ArgumentOutOfRangeException("rise");
            double a=Math.PI/8, t=Math.Tan(a), c=Math.Cos(a), s=Math.Sin(a);
            double diagonal=1/(Math.Sqrt(2)*c);
            double shear=-(diagonal-t*(c-s))*2/rise;
            double u=x-1, v=z-1;
            double px=t*c*u-t*s*v+shear*y;
            double pz=-t*s*u+t*c*v+shear*y;
            // Keep the first eave aligned to the original wall direction.
            nx=1+c*px+s*pz; ny=y; nz=1-s*px+c*pz;
        }
    }
}

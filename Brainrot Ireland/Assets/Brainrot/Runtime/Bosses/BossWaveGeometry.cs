using System;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Continuous XZ contact with a linearly expanding annulus, including chord crossings.</summary>
    public static class BossWaveGeometry
    {
        public static bool SweptRingHit(Vector3 from,Vector3 to,Vector3 center,float oldRadius,float newRadius,float halfWidth){
            if(!Finite(from.x)||!Finite(from.z)||!Finite(to.x)||!Finite(to.z)||!Finite(center.x)||!Finite(center.z)||!Finite(oldRadius)||!Finite(newRadius)||!Finite(halfWidth))return false;
            double ox=(double)from.x-center.x,oz=(double)from.z-center.z;
            double vx=(double)to.x-from.x,vz=(double)to.z-from.z;
            double r0=Math.Max(0,oldRadius),r1=Math.Max(0,newRadius),width=Math.Max(0,halfWidth),dr=r1-r0;
            double start=Math.Sqrt(ox*ox+oz*oz)-r0;
            double end=Math.Sqrt((ox+vx)*(ox+vx)+(oz+vz)*(oz+vz))-r1;
            if(Math.Abs(start)<=width||Math.Abs(end)<=width)return true;
            bool insideStart=start<-width,insideEnd=end<-width;
            if(insideStart&&insideEnd)return false;
            if(insideStart!=insideEnd)return true;
            // Both endpoints lie outside. The distance minus linear radius is convex;
            // an intermediate entry exists exactly when the swept point enters the outer disc.
            // Q(t) = |offset + velocity*t|^2 - (r0 + width + dr*t)^2.
            double outer=r0+width;
            double a=vx*vx+vz*vz-dr*dr;
            double b=2*(ox*vx+oz*vz-outer*dr);
            double cc=ox*ox+oz*oz-outer*outer;
            double minimum=Math.Min(cc,a+b+cc);
            if(a>0){double t=-b/(2*a);if(t>0&&t<1)minimum=Math.Min(minimum,(a*t+b)*t+cc);}
            return minimum<=1e-8;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}

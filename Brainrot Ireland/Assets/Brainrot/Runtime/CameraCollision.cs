using System;
using UnityEngine;

namespace Brainrot
{
    /// <summary>Non-allocating environment camera obstruction. Actor colliders never push the camera into the player.</summary>
    public sealed class CameraCollision : IDisposable
    {
        readonly RaycastHit[] hits=new RaycastHit[256];
        readonly Collider[] overlaps=new Collider[256];
        readonly Transform owner;
        readonly SphereCollider probe;
        float distance;
        bool initialized;
        public CameraCollision(Transform owner)
        {
            this.owner=owner;
            var helper=new GameObject("Camera collision probe"){hideFlags=HideFlags.HideAndDontSave};
            if(owner!=null)helper.transform.SetParent(owner,false);
            probe=helper.AddComponent<SphereCollider>();probe.enabled=false;probe.isTrigger=true;
        }
        public void Reset(){initialized=false;distance=0;}
        bool Blocks(Collider collider)
        {
            if(collider==null||collider==probe||collider.isTrigger)return false;
            var transform=collider.transform;
            if(owner!=null&&(transform==owner||transform.IsChildOf(owner)))return false;
            return collider.GetComponentInParent<EnemyController>()==null;
        }
        Vector3 ResolveOverlap(Vector3 position,float radius,float padding,int mask)
        {
            probe.radius=radius;
            // ComputePenetration supports a sphere against terrain/mesh colliders. Explicit poses keep the probe disabled.
            for(int iteration=0;iteration<6;iteration++)
            {
                int count=Physics.OverlapSphereNonAlloc(position,radius,overlaps,mask,QueryTriggerInteraction.Ignore);
                bool moved=false;
                for(int i=0;i<count;i++)
                {
                    var collider=overlaps[i];if(!Blocks(collider))continue;
                    if(Physics.ComputePenetration(probe,position,Quaternion.identity,collider,collider.transform.position,collider.transform.rotation,out var direction,out var depth)&&depth>0)
                    {
                        position+=direction*(depth+padding);moved=true;
                    }
                }
                if(!moved)break;
            }
            return position;
        }
        /// <summary>Pull inward immediately, then recover the normal shoulder distance smoothly using unscaled time.</summary>
        public Vector3 Resolve(Vector3 pivot,Vector3 desired,float radius,float padding,float returnSpeed,int mask,float deltaTime)
        {
            radius=Mathf.Clamp(radius,.05f,1);padding=Mathf.Clamp(padding,.005f,.4f);
            var origin=ResolveOverlap(pivot,radius,padding,mask);
            Vector3 delta=desired-origin;float length=delta.magnitude;
            if(length<.0001f){initialized=false;return origin;}
            Vector3 direction=delta/length;float allowed=length;
            int count=Physics.SphereCastNonAlloc(origin,radius,direction,hits,length,mask,QueryTriggerInteraction.Ignore);
            // A saturated buffer cannot prove a clear path. A close camera is preferable to crossing unseen geometry.
            if(count>=hits.Length)allowed=0;
            else for(int i=0;i<count;i++)if(Blocks(hits[i].collider))allowed=Mathf.Min(allowed,Mathf.Max(0,hits[i].distance-padding));
            if(!initialized||allowed<distance){distance=allowed;initialized=true;}
            else distance=Mathf.Lerp(distance,allowed,1-Mathf.Exp(-Mathf.Max(.1f,returnSpeed)*Mathf.Max(0,deltaTime)));
            distance=Mathf.Clamp(distance,0,allowed);
            return ResolveOverlap(origin+direction*distance,radius,padding,mask);
        }
        public void Dispose()
        {
            if(probe==null)return;
            if(Application.isPlaying)UnityEngine.Object.Destroy(probe.gameObject);
            else UnityEngine.Object.DestroyImmediate(probe.gameObject);
        }
    }
}

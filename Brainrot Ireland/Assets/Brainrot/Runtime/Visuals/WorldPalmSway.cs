using UnityEngine;
namespace Brainrot
{
    /// <summary>One crown transform per palm; no per-frond updates, physics or allocations.</summary>
    public sealed class WorldPalmSway : MonoBehaviour
    {
        public float Phase;
        Quaternion baseline;bool cached;
        void OnEnable(){if(!cached){baseline=transform.localRotation;cached=true;}}
        public static Quaternion Offset(float time,float phase)
        {
            float t=time*.65f+phase;
            // A shared slow gust with a per-tree phase feels like wind crossing the island.
            // Scaled time naturally freezes the crowns when gameplay is paused.
            float gust=.75f+.25f*Mathf.Sin(time*.19f);
            return Quaternion.Euler((Mathf.Sin(t)*1.8f+Mathf.Sin(t*2.13f)*.28f)*gust,Mathf.Sin(t*.63f)*1.2f,Mathf.Cos(t*.81f)*1.4f*gust);
        }
        void Update(){
            if(Time.deltaTime<=0)return;
            var rotation=baseline*Offset(Time.time,Phase);
            if(transform.localRotation!=rotation)transform.localRotation=rotation;
        }
        void OnDisable(){if(cached)transform.localRotation=baseline;}
    }
}

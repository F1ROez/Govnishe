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
            return Quaternion.Euler(Mathf.Sin(t)*1.8f,Mathf.Sin(t*.63f)*1.2f,Mathf.Cos(t*.81f)*1.4f);
        }
        void Update(){transform.localRotation=baseline*Offset(Time.time,Phase);}
        void OnDisable(){if(cached)transform.localRotation=baseline;}
    }
}

using UnityEngine;
namespace Brainrot
{
    /// <summary>One crown transform per palm; no per-frond updates, physics or allocations.</summary>
    public sealed class WorldPalmSway : MonoBehaviour
    {
        public float Phase;
        void Update(){float t=Time.time*.65f+Phase;transform.localRotation=Quaternion.Euler(Mathf.Sin(t)*1.8f,Mathf.Sin(t*.63f)*1.2f,Mathf.Cos(t*.81f)*1.4f);}
    }
}

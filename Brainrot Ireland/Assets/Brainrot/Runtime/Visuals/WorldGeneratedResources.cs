using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    /// <summary>Owns only procedural Unity objects created for this island, never imported or built-in assets.</summary>
    public sealed class WorldGeneratedResources : MonoBehaviour
    {
        readonly HashSet<Object> owned = new HashSet<Object>();
        public void Own(Object value) { if(value!=null)owned.Add(value); }
        void OnDestroy()
        {
            // A replacement island may already have assigned its own skybox by the time Destroy runs.
            if(RenderSettings.skybox!=null&&owned.Contains(RenderSettings.skybox))RenderSettings.skybox=null;
            foreach(var value in owned)if(value!=null)Destroy(value);
            owned.Clear();
        }
    }
}

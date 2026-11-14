using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Brainrot
{
    /// <summary>Small restrained grading stack; camera setup happens once after the arena bootstrap.</summary>
    public sealed class WorldAtmosphere : MonoBehaviour
    {
        VolumeProfile profile;
        UniversalAdditionalCameraData cameraData;int quality=-1;
        public static VolumeProfile CreateProfile(bool bloomAllowed=true)
        {
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Мягкое тропическое освещение";
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(-.12f);color.contrast.Override(4);color.saturation.Override(5);color.colorFilter.Override(new Color(1,.985f,.96f));
            var balance=profile.Add<WhiteBalance>(true);balance.temperature.Override(3);balance.tint.Override(1);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.12f);vignette.smoothness.Override(.7f);vignette.color.Override(new Color(.08f,.17f,.23f));
            {var bloom=profile.Add<Bloom>(true);bloom.active=bloomAllowed;bloom.intensity.Override(.17f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.52f);bloom.highQualityFiltering.Override(false);}
            return profile;
        }
        IEnumerator Start()
        {
            var template=Resources.Load<VolumeProfile>("IslandAtmosphere");
            if(template==null)profile=CreateProfile(SaveSystem.Data.Quality>0);
            else{
                profile=ScriptableObject.CreateInstance<VolumeProfile>();
                foreach(var component in template.components)if(component!=null)profile.components.Add(Instantiate(component));
                if(profile.TryGet<Bloom>(out var bloom))bloom.active=SaveSystem.Data.Quality>0;
            }
            var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.sharedProfile=profile;volume.weight=1;
            // WorldBuilder precedes PlayerController's camera. Search only during bootstrap, never every frame afterwards.
            for(int attempt=0;attempt<30;attempt++){
                foreach(var camera in Camera.allCameras){
                    if(camera.name!="Arena Camera")continue;
                    cameraData=camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;ApplyQuality();
                    yield break;
                }
                yield return null;
            }
        }
        void Update(){if(profile!=null&&quality!=SaveSystem.Data.Quality)ApplyQuality();}
        void ApplyQuality(){quality=SaveSystem.Data.Quality;if(profile!=null&&profile.TryGet<Bloom>(out var bloom))bloom.active=quality>0;if(cameraData!=null)cameraData.antialiasing=quality>0?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.None;}
        void OnDestroy(){if(profile!=null){foreach(var component in profile.components)if(component!=null)Destroy(component);Destroy(profile);}}
    }
}

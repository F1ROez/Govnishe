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
        public const string PresetName="Яркий тропический день — v2";
        static T Component<T>(VolumeProfile profile) where T:VolumeComponent {if(profile.TryGet<T>(out var value))return value;return profile.Add<T>(true);}
        public static void ApplyDaylight(VolumeProfile profile,bool bloomAllowed=true)
        {
            profile.name=PresetName;
            var tone=Component<Tonemapping>(profile);tone.active=true;tone.mode.Override(TonemappingMode.Neutral);
            var color=Component<ColorAdjustments>(profile);color.active=true;color.postExposure.Override(.30f);color.contrast.Override(-4);color.saturation.Override(14);color.colorFilter.Override(Color.white);
            var balance=Component<WhiteBalance>(profile);balance.active=true;balance.temperature.Override(2);balance.tint.Override(0);
            var vignette=Component<Vignette>(profile);vignette.active=false;vignette.intensity.Override(0);vignette.smoothness.Override(.7f);
            var bloom=Component<Bloom>(profile);bloom.active=bloomAllowed;bloom.intensity.Override(.13f);bloom.threshold.Override(1.3f);bloom.scatter.Override(.48f);bloom.highQualityFiltering.Override(false);
        }
        public static VolumeProfile CreateProfile(bool bloomAllowed=true)
        {
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();ApplyDaylight(profile,bloomAllowed);return profile;
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

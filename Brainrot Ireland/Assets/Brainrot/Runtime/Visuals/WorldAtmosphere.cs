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
        bool authoredBloom;
        DepthOfField pauseFocus;
        bool blurred,originalDepth;
        UniversalAdditionalCameraData cameraData;int quality=-1;
        bool ownsCamera,originalPost,originalDithering;
        AntialiasingMode originalAA;AntialiasingQuality originalAAQuality;
        public const string PresetName="Сочный остров — цвет, объём и мягкое солнце — v6";
        static T Component<T>(VolumeProfile profile) where T:VolumeComponent {if(profile.TryGet<T>(out var value))return value;return profile.Add<T>(true);}
        public static void ApplyDaylight(VolumeProfile profile,bool bloomAllowed=true)
        {
            profile.name=PresetName;
            var tone=Component<Tonemapping>(profile);tone.active=true;tone.mode.Override(TonemappingMode.Neutral);
            var color=Component<ColorAdjustments>(profile);color.active=true;color.postExposure.Override(.025f);color.contrast.Override(6);color.saturation.Override(8);color.colorFilter.Override(Color.white);
            var balance=Component<WhiteBalance>(profile);balance.active=true;balance.temperature.Override(3);balance.tint.Override(0);
            // Near-neutral split tones enrich shaded silhouettes without tinting white foam or the UI.
            var split=Component<SplitToning>(profile);split.active=true;
            split.shadows.Override(new Color(.47f,.50f,.53f));split.highlights.Override(new Color(.53f,.51f,.47f));split.balance.Override(4);
            var vignette=Component<Vignette>(profile);vignette.active=false;vignette.intensity.Override(0);vignette.smoothness.Override(.7f);
            var bloom=Component<Bloom>(profile);bloom.active=bloomAllowed;bloom.intensity.Override(.18f);bloom.threshold.Override(1.25f);bloom.scatter.Override(.42f);bloom.highQualityFiltering.Override(false);
        }
        public static VolumeProfile CreateProfile(bool bloomAllowed=true)
        {
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();ApplyDaylight(profile,bloomAllowed);return profile;
        }
        IEnumerator Start()
        {
            var template=Resources.Load<VolumeProfile>("IslandAtmosphere");
            if(template==null)profile=CreateProfile();
            else{
                profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name=template.name;
                foreach(var component in template.components)if(component!=null)profile.components.Add(Instantiate(component));
                // Upgrade our own old preset in memory, leaving an artist's renamed/custom profile untouched.
                // This also applies when the user replaces scripts without rebuilding the generated scene.
                if(template.name.StartsWith("Сочный остров —")&&template.name!=PresetName)
                    ApplyDaylight(profile,!profile.TryGet<Bloom>(out var oldBloom)||oldBloom.active);
            }
            // Quality may disable an authored effect, but must not turn on one the owner disabled in the profile.
            authoredBloom=profile.TryGet<Bloom>(out var configuredBloom)&&configuredBloom.active;
            pauseFocus=Component<DepthOfField>(profile);pauseFocus.active=false;
            pauseFocus.mode.Override(DepthOfFieldMode.Gaussian);pauseFocus.gaussianStart.Override(0);
            pauseFocus.gaussianEnd.Override(10);pauseFocus.gaussianMaxRadius.Override(1.2f);
            var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.sharedProfile=profile;volume.weight=1;
            // WorldBuilder precedes PlayerController's camera. Search only during bootstrap, never every frame afterwards.
            for(int attempt=0;attempt<30;attempt++){
                foreach(var camera in Camera.allCameras){
                    if(camera.name!="Arena Camera")continue;
                    cameraData=camera.GetUniversalAdditionalCameraData();ConfigureCamera();
                    yield break;
                }
                yield return null;
            }
        }
        void Update(){
            if(profile!=null&&quality!=SaveSystem.Data.Quality)ApplyQuality();
            bool paused=quality>0&&GameManager.Instance!=null&&(GameManager.Instance.State==GameState.Paused||GameManager.Instance.State==GameState.RunShop);
            if(paused==blurred||pauseFocus==null)return;
            blurred=paused;pauseFocus.active=paused;
            if(cameraData!=null)cameraData.requiresDepthTexture=originalDepth||paused;
        }
        void ConfigureCamera(){
            if(cameraData==null||ownsCamera)return;
            originalDepth=cameraData.requiresDepthTexture;originalPost=cameraData.renderPostProcessing;
            originalDithering=cameraData.dithering;originalAA=cameraData.antialiasing;originalAAQuality=cameraData.antialiasingQuality;
            ownsCamera=true;cameraData.renderPostProcessing=true;ApplyQuality();
        }
        void RestoreCamera(){
            if(!ownsCamera)return;ownsCamera=false;
            if(cameraData==null)return;
            cameraData.requiresDepthTexture=originalDepth;cameraData.renderPostProcessing=originalPost;
            cameraData.dithering=originalDithering;cameraData.antialiasing=originalAA;cameraData.antialiasingQuality=originalAAQuality;
        }
        void OnEnable(){if(profile!=null)ConfigureCamera();}
        void OnDisable(){
            blurred=false;if(pauseFocus!=null)pauseFocus.active=false;RestoreCamera();
        }
        void ApplyQuality(){
            quality=SaveSystem.Data.Quality;
            if(profile!=null&&profile.TryGet<Bloom>(out var bloom)){
                bloom.active=authoredBloom&&quality>0;bloom.highQualityFiltering.Override(quality>=2);
            }
            if(cameraData!=null){
                // SMAA preserves the sand grain and model texture better than FXAA at full resolution.
                cameraData.antialiasing=quality>=2?AntialiasingMode.SubpixelMorphologicalAntiAliasing:quality==1?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.None;
                cameraData.antialiasingQuality=AntialiasingQuality.High;
                cameraData.dithering=quality>0;
            }
        }
        void OnDestroy(){RestoreCamera();if(profile!=null){foreach(var component in profile.components)if(component!=null)Destroy(component);Destroy(profile);}}
    }
}

using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Brainrot.Tests
{
    public sealed class DaylightProfileTests
    {
        [Test]public void BrightPresetUpgradesOldDarkProfileWithoutDuplicatingComponents(){
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            try{
                var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(-.12f);
                var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.12f);
                WorldAtmosphere.ApplyDaylight(profile,false);int count=profile.components.Count;
                WorldAtmosphere.ApplyDaylight(profile,false);
                Assert.AreEqual(count,profile.components.Count);Assert.AreEqual(WorldAtmosphere.PresetName,profile.name);
                Assert.Greater(color.postExposure.value,0);Assert.Greater(color.saturation.value,5);Assert.AreEqual(0,vignette.intensity.value);
                Assert.IsTrue(profile.TryGet<Bloom>(out var bloom));Assert.IsFalse(bloom.active);
            }finally{foreach(var component in profile.components)if(component!=null)Object.DestroyImmediate(component);Object.DestroyImmediate(profile);}
        }
    }
}

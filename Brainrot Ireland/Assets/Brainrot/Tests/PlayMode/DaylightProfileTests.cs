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
                Assert.IsTrue(profile.TryGet<SplitToning>(out var split));Assert.IsTrue(split.active);
                Assert.Greater(split.shadows.value.b,split.shadows.value.r,"Тени должны быть немного холоднее света.");
                Assert.Greater(split.highlights.value.r,split.highlights.value.b);
                Assert.Less(bloom.intensity.value,.3f,"Свечение не должно скрывать предупреждения атак.");
                Assert.IsFalse(profile.TryGet<DepthOfField>(out _),"Размытие глубины добавляется отдельно только для паузы.");
            }finally{foreach(var component in profile.components)if(component!=null)Object.DestroyImmediate(component);Object.DestroyImmediate(profile);}
        }
    }
}

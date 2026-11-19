using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace Brainrot.Tests
{
    public sealed class PolishSettingsTests
    {
        [Test]public void LegacySettingsKeepRecordsAndDefaultShakeToEnabled()
        {
            string directory=Path.Combine(Path.GetTempPath(),"brainrot-settings-"+Guid.NewGuid().ToString("N"));
            try{using(SaveSystem.UseStorageDirectory(directory)){
                File.WriteAllText(Path.Combine(directory,"brainrot-save.json"),"{\"BestKills\":29,\"Volume\":0.4}");
                SaveSystem.Load();Assert.AreEqual(29,SaveSystem.Data.BestKills);Assert.AreEqual(1,SaveSystem.Data.CameraShakeStrength);
                SaveSystem.Data.CameraShakeStrength=0;SaveSystem.Save();SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.CameraShakeStrength);
            }}finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]public void RepairingCorruptPrimaryPreservesLastReadableSettingsBackup()
        {
            string directory=Path.Combine(Path.GetTempPath(),"brainrot-settings-"+Guid.NewGuid().ToString("N"));
            try{using(SaveSystem.UseStorageDirectory(directory)){
                string path=Path.Combine(directory,"brainrot-save.json");SaveSystem.Data.BestKills=10;SaveSystem.Save();
                SaveSystem.Data.BestKills=20;SaveSystem.Save();File.WriteAllText(path,"bad");SaveSystem.Load();
                Assert.AreEqual(10,SaveSystem.Data.BestKills);SaveSystem.Data.BestKills=30;SaveSystem.Save();
                Assert.AreEqual(10,JsonUtility.FromJson<SaveData>(File.ReadAllText(path+".bak")).BestKills);
                Assert.IsFalse(File.Exists(path+".tmp"));
            }}finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]public void GraphicsPresetsChangeRealUrpCostsWithoutTurningOffHdr()
        {
            var low=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var high=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try{
                low.supportsHDR=high.supportsHDR=true;GameQuality.Configure(low,0);GameQuality.Configure(high,2);
                Assert.Less(low.renderScale,high.renderScale);Assert.Less(low.shadowDistance,high.shadowDistance);
                Assert.AreEqual(1,low.msaaSampleCount);Assert.AreEqual(1,high.msaaSampleCount);Assert.IsTrue(low.supportsHDR);Assert.IsTrue(high.supportsHDR);
                Assert.Less(low.mainLightShadowmapResolution,high.mainLightShadowmapResolution);
                Assert.AreEqual(1,low.shadowCascadeCount);Assert.AreEqual(2,high.shadowCascadeCount);
                Assert.Greater(high.cascade4Split.x,0);Assert.Less(high.cascade4Split.x,high.cascade4Split.y);
                Assert.Less(high.cascade4Split.y,high.cascade4Split.z);Assert.Less(high.cascade4Split.z,1);
            }finally{UnityEngine.Object.DestroyImmediate(low);UnityEngine.Object.DestroyImmediate(high);}
        }
        [Test]public void ReleasingQualityRestoresEveryTouchedPipelineSlot()
        {
            if(QualitySettings.names.Length<2)Assert.Ignore("Требуются минимум два уровня качества.");
            if(!(QualitySettings.renderPipeline is UniversalRenderPipelineAsset)&&!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))Assert.Ignore("Требуется настроенный URP-проект.");
            GameQuality.Release();int initial=QualitySettings.GetQualityLevel(),count=Mathf.Min(3,QualitySettings.names.Length);
            var originals=new RenderPipelineAsset[count];
            try{
                for(int i=0;i<count;i++){QualitySettings.SetQualityLevel(i,true);originals[i]=QualitySettings.renderPipeline;}
                QualitySettings.SetQualityLevel(initial,true);
                for(int i=0;i<count;i++)GameQuality.Apply(i);
                GameQuality.Release();Assert.AreEqual(initial,QualitySettings.GetQualityLevel());
                for(int i=0;i<count;i++){QualitySettings.SetQualityLevel(i,true);Assert.AreEqual(originals[i],QualitySettings.renderPipeline,"Оригинальный override уровня "+i+" должен сохраниться.");}
            }finally{GameQuality.Release();for(int i=0;i<count;i++){QualitySettings.SetQualityLevel(i,true);QualitySettings.renderPipeline=originals[i];}QualitySettings.SetQualityLevel(initial,true);}
        }
    }
}

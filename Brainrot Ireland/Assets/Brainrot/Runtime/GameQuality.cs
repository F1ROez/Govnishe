using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Brainrot
{
    /// <summary>Runtime-only URP variants; never edits the imported pipeline or author materials.</summary>
    public static class GameQuality
    {
        static readonly UniversalRenderPipelineAsset[] variants=new UniversalRenderPipelineAsset[3];
        static RenderPipelineAsset previousOverride;
        static readonly Dictionary<int,RenderPipelineAsset> originalLevels=new Dictionary<int,RenderPipelineAsset>();
        static int previousLevel;
        static bool captured;

        public static void Apply(int level)
        {
            level=Mathf.Clamp(level,0,2);
            if(!captured){previousOverride=QualitySettings.renderPipeline;previousLevel=QualitySettings.GetQualityLevel();captured=true;}
            var source=previousOverride as UniversalRenderPipelineAsset;
            if(source==null)source=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if(source==null)return;
            if(variants[level]==null){
                var pipeline=Object.Instantiate(source);pipeline.name="Island Runtime Quality "+level;pipeline.hideFlags=HideFlags.DontSave;
                Configure(pipeline,level);variants[level]=pipeline;
            }
            int index=Mathf.Clamp(level,0,Mathf.Max(0,QualitySettings.names.Length-1));
            QualitySettings.SetQualityLevel(index,true);
            if(!originalLevels.ContainsKey(index))originalLevels.Add(index,QualitySettings.renderPipeline);
            QualitySettings.renderPipeline=variants[level];
        }

        public static void Configure(UniversalRenderPipelineAsset pipeline,int level)
        {
            if(pipeline==null)return;level=Mathf.Clamp(level,0,2);
            pipeline.renderScale=level==0?.70f:level==1?.85f:1;
            pipeline.msaaSampleCount=1; // Post AA already runs on medium/high; avoid a second full MSAA resolve.
            pipeline.mainLightShadowmapResolution=level==0?512:level==1?1024:2048;
            pipeline.shadowDistance=level==0?24:level==1?35:45;
            pipeline.shadowCascadeCount=level==0?1:2;
            pipeline.cascade2Split=.32f;
            pipeline.cascade4Split=new Vector3(.12f,.30f,.58f);
            pipeline.cascadeBorder=.16f;
            pipeline.useSRPBatcher=true;
        }

        public static void Release()
        {
            // Unity stores a distinct override on every quality level, not a single global override.
            // Restore every touched slot before destroying our clones, including inactive slots.
            foreach(var entry in originalLevels){
                QualitySettings.SetQualityLevel(entry.Key,true);
                bool own=false;
                for(int i=0;i<variants.Length;i++)if(variants[i]!=null&&QualitySettings.renderPipeline==variants[i])own=true;
                if(own)QualitySettings.renderPipeline=entry.Value;
            }
            if(captured)QualitySettings.SetQualityLevel(previousLevel,true);
            originalLevels.Clear();
            for(int i=0;i<variants.Length;i++){
                var pipeline=variants[i];variants[i]=null;if(pipeline==null)continue;
                if(Application.isPlaying)Object.Destroy(pipeline);else Object.DestroyImmediate(pipeline);
            }
            previousOverride=null;captured=false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Release();Application.quitting-=Release;Application.quitting+=Release;}
    }
}

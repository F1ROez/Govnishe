#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
namespace Brainrot
{
    /// <summary>Opt-in CPU-frame/memory CSV for manual alpha acceptance; absent from non-development players.</summary>
    public sealed class AlphaDiagnostics:MonoBehaviour
    {
        readonly float[] samples=new float[512];
        StreamWriter writer;
        int sampleCount,frames,errors,warnings,previousCollections;
        double sum;
        float nextWrite,started;
        string path;
        static AlphaDiagnostics instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            bool requested=Environment.GetEnvironmentVariable("BRAINROT_ALPHA_DIAGNOSTICS")=="1";
            foreach(string arg in Environment.GetCommandLineArgs())if(arg=="-brainrotDiagnostics")requested=true;
            if(!requested||instance!=null)return;
            instance=new GameObject("Alpha diagnostics — opt in").AddComponent<AlphaDiagnostics>();
            DontDestroyOnLoad(instance.gameObject);
        }
        void Awake()
        {
            started=Time.realtimeSinceStartup;nextWrite=started+1;previousCollections=GC.CollectionCount(0);
            try{
                Directory.CreateDirectory(Application.persistentDataPath);
                path=Path.Combine(Application.persistentDataPath,"alpha-profile-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff",CultureInfo.InvariantCulture)+".csv");
                writer=new StreamWriter(path,false);
                writer.WriteLine("utc,realtime_s,state,run_s,wave,kills,alive,corpses,frames,sampled,mean_frame_ms,p50_frame_ms,p95_frame_ms,unity_allocated_bytes,managed_bytes,gc0_delta,warnings,errors");writer.Flush();
                Application.logMessageReceived+=OnLog;Debug.Log("ALPHA_DIAGNOSTICS_CSV: "+path);
            }catch(Exception exception)when(exception is IOException||exception is UnauthorizedAccessException){Debug.LogWarning("Диагностика не записывается: "+exception.Message);enabled=false;}
        }
        void OnLog(string condition,string stack,LogType type){if(type==LogType.Warning)warnings++;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Update()
        {
            if(writer==null)return;
            float frame=Time.unscaledDeltaTime*1000;
            if(frame>=0&&!float.IsNaN(frame)&&!float.IsInfinity(frame)){frames++;sum+=frame;if(sampleCount<samples.Length)samples[sampleCount++]=frame;}
            float now=Time.realtimeSinceStartup;if(now<nextWrite)return;nextWrite=now+1;
            Array.Sort(samples,0,sampleCount);
            var game=GameManager.Instance;int collections=GC.CollectionCount(0);
            try{
                writer.WriteLine(string.Join(",",new[]{DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture),F(now-started),game!=null?game.State.ToString():"NoGame",F(game!=null?game.Elapsed:0),I(game!=null?game.Wave:0),I(game!=null?game.Kills:0),I(game!=null&&game.Spawner!=null?game.Spawner.Active.Count:0),I(game!=null&&game.Spawner!=null?game.Spawner.Corpses.Count:0),I(frames),I(sampleCount),F(frames>0?(float)(sum/frames):0),F(Percentile(.50f)),F(Percentile(.95f)),Profiler.GetTotalAllocatedMemoryLong().ToString(CultureInfo.InvariantCulture),GC.GetTotalMemory(false).ToString(CultureInfo.InvariantCulture),I(collections-previousCollections),I(warnings),I(errors)}));
                writer.Flush();
            }catch(Exception exception)when(exception is IOException||exception is UnauthorizedAccessException){Debug.LogWarning("Запись диагностики остановлена: "+exception.Message);Close();enabled=false;}
            previousCollections=collections;frames=sampleCount=0;sum=0;
        }
        float Percentile(float p)=>sampleCount>0?samples[Mathf.Clamp(Mathf.CeilToInt(sampleCount*p)-1,0,sampleCount-1)]:0;
        static string F(float n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
        static string I(int n)=>n.ToString(CultureInfo.InvariantCulture);
        void Close(){Application.logMessageReceived-=OnLog;if(writer!=null){try{writer.Dispose();}catch(IOException){}writer=null;}}
        void OnApplicationQuit(){Close();}
        void OnDestroy(){Close();if(instance==this)instance=null;}
    }
}
#endif

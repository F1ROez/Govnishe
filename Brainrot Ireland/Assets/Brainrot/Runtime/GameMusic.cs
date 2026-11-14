using System;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Persistent music and UI audio; world effects remain owned by GameAudio.</summary>
    public sealed class GameMusic : MonoBehaviour
    {
        public enum PlaybackMode { Silent, Menu, Gameplay, Paused }
        static GameMusic instance;
        static bool muted;
        public static bool IsMuted=>muted;
        AudioSource menuSource,gameplaySource,buttonSource;
        AudioClip menuClip,buttonClip;
        AudioClip[] tracks=Array.Empty<AudioClip>();
        MusicShuffleBag order;
        bool gameplayPaused,waitingForStart;
        float startDeadline;
        public PlaybackMode Mode { get; private set; }
        public AudioClip CurrentGameplayTrack=>gameplaySource!=null?gameplaySource.clip:null;
        public static GameMusic Instance=>instance;

        static GameMusic Ensure()
        {
            if(!Application.isPlaying)return null;
            if(instance!=null)return instance;
            instance=UnityEngine.Object.FindFirstObjectByType<GameMusic>();
            if(instance==null)instance=new GameObject("Музыка и звуки интерфейса").AddComponent<GameMusic>();
            return instance;
        }
        void Awake()
        {
            if(instance!=null&&instance!=this){Destroy(gameObject);return;}
            instance=this;DontDestroyOnLoad(gameObject);
            menuSource=Source("Меню",.75f,true);gameplaySource=Source("Игра",.62f,false);buttonSource=Source("Кнопки",.85f,false);
            menuClip=Resources.Load<AudioClip>("Audio/Menu");buttonClip=Resources.Load<AudioClip>("Audio/Button");
            tracks=Resources.LoadAll<AudioClip>("Audio/Gameplay");
            Array.Sort(tracks,(a,b)=>StringComparer.Ordinal.Compare(a.name,b.name));order=new MusicShuffleBag(tracks.Length);
            menuSource.clip=menuClip;
        }
        AudioSource Source(string title,float gain,bool loop)
        {
            var child=new GameObject(title);child.transform.SetParent(transform,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=0;source.volume=gain;source.loop=loop;source.ignoreListenerPause=true;return source;
        }
        public static void ToggleMute(){muted=!muted;ApplyVolume();}
        public static void ApplyVolume(){AudioListener.volume=muted?0:Mathf.Clamp01(SaveSystem.Data.Volume);}
        public static void SetMenu(){var music=Ensure();if(music!=null)music.EnterMenu();}
        public static void SetGameplay(){var music=Ensure();if(music!=null)music.EnterGameplay();}
        public static void SetPaused(bool paused){var music=Ensure();if(music==null)return;if(paused)music.PauseGameplay();else music.EnterGameplay();}
        public static void RestartGameplay()
        {
            var music=Ensure();if(music==null)return;music.gameplaySource.Stop();music.gameplaySource.clip=null;music.gameplayPaused=false;music.waitingForStart=false;
            music.Mode=PlaybackMode.Paused;music.EnterGameplay();
        }
        public static void PlayButtonClick(){var music=Ensure();if(music!=null&&music.buttonClip!=null)music.buttonSource.PlayOneShot(music.buttonClip);}
        void EnterMenu()
        {
            ApplyVolume();
            if(Mode==PlaybackMode.Menu)return;
            HoldGameplay();Mode=PlaybackMode.Menu;
            if(menuClip!=null){menuSource.UnPause();if(!menuSource.isPlaying)menuSource.Play();}
        }
        void EnterGameplay()
        {
            ApplyVolume();
            if(Mode==PlaybackMode.Gameplay)return;
            menuSource.Pause();Mode=PlaybackMode.Gameplay;
            if(gameplayPaused&&gameplaySource.clip!=null){gameplaySource.UnPause();gameplayPaused=false;waitingForStart=true;startDeadline=Time.unscaledTime+3;}
            else StartNextTrack();
        }
        void HoldGameplay()
        {
            if(Mode!=PlaybackMode.Gameplay)return;
            gameplayPaused=gameplaySource.clip!=null&&(gameplaySource.isPlaying||waitingForStart);gameplaySource.Pause();
        }
        void PauseGameplay(){HoldGameplay();menuSource.Pause();Mode=PlaybackMode.Paused;}
        void StartNextTrack()
        {
            int next=order!=null?order.Next():-1;
            if(next<0)return;
            gameplaySource.clip=tracks[next];gameplaySource.Play();gameplayPaused=false;waitingForStart=true;startDeadline=Time.unscaledTime+3;
        }
        void Update()
        {
            float target=muted?0:Mathf.Clamp01(SaveSystem.Data.Volume);if(!Mathf.Approximately(AudioListener.volume,target))AudioListener.volume=target;
            if(Mode!=PlaybackMode.Gameplay||tracks.Length==0)return;
            if(gameplaySource.isPlaying){waitingForStart=false;return;}
            // A streaming clip may need several frames before it can start; do not skip every frame while loading.
            if(waitingForStart&&Time.unscaledTime<startDeadline)return;
            waitingForStart=false;StartNextTrack();
        }
        void OnDestroy(){if(instance==this)instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){instance=null;muted=false;}
    }

    /// <summary>Visits every track once per cycle, without repeating at cycle boundaries.</summary>
    public sealed class MusicShuffleBag
    {
        readonly int[] bag;readonly System.Random random;
        int cursor,last=-1;
        public MusicShuffleBag(int count,int? seed=null){bag=new int[Math.Max(0,count)];random=new System.Random(seed??Environment.TickCount);cursor=bag.Length;}
        public int Next()
        {
            if(bag.Length==0)return -1;
            if(cursor>=bag.Length){
                for(int i=0;i<bag.Length;i++)bag[i]=i;
                for(int i=bag.Length-1;i>0;i--){int j=random.Next(i+1);int value=bag[i];bag[i]=bag[j];bag[j]=value;}
                if(bag.Length>1&&bag[0]==last){int j=random.Next(1,bag.Length);int value=bag[0];bag[0]=bag[j];bag[j]=value;}
                cursor=0;
            }
            last=bag[cursor++];return last;
        }
    }
}

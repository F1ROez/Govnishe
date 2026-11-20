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
        static bool focused=true,applicationPaused;
        public static bool IsMuted=>muted;
        AudioSource menuSource,gameplaySource,buttonSource,feedbackSource;
        AudioClip menuClip,buttonClip,purchaseClip,rejectClip;
        bool ownsButtonClip;float nextButtonAt,nextFeedbackAt;
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
            menuSource=Source("Меню",.75f,true);gameplaySource=Source("Игра",.62f,false);buttonSource=Source("Кнопки",.85f,false);feedbackSource=Source("Покупки",.42f,false);
            menuClip=Resources.Load<AudioClip>("Audio/Menu");buttonClip=Resources.Load<AudioClip>("Audio/Button");
            if(buttonClip==null){buttonClip=CreateFeedback("Нажатие",0);ownsButtonClip=true;}
            purchaseClip=CreateFeedback("Покупка улучшения",1);rejectClip=CreateFeedback("Недоступная покупка",2);
            tracks=Resources.LoadAll<AudioClip>("Audio/Gameplay");
            Array.Sort(tracks,(a,b)=>StringComparer.Ordinal.Compare(a.name,b.name));order=new MusicShuffleBag(tracks.Length);
            menuSource.clip=menuClip;menuSource.volume=gameplaySource.volume=0;
        }
        AudioSource Source(string title,float gain,bool loop)
        {
            var child=new GameObject(title);child.transform.SetParent(transform,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=0;source.volume=gain;source.loop=loop;source.ignoreListenerPause=false;return source;
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
        public static void PlayButtonClick(){
            var music=Ensure();if(music==null||AudioListener.pause||Time.unscaledTime<music.nextButtonAt)return;
            // One persistent voice: rapid presses must not accumulate overlapping PlayOneShot voices.
            music.nextButtonAt=Time.unscaledTime+.04f;music.buttonSource.clip=music.buttonClip;music.buttonSource.Play();
        }
        public static void PlayPurchase(bool success){
            var music=Ensure();if(music==null||AudioListener.pause||Time.unscaledTime<music.nextFeedbackAt)return;
            music.nextFeedbackAt=Time.unscaledTime+.08f;music.feedbackSource.clip=success?music.purchaseClip:music.rejectClip;music.feedbackSource.Play();
        }
        static AudioClip CreateFeedback(string name,int style){
            const int rate=22050;float length=style==1?.24f:style==2?.13f:.045f;int size=Mathf.CeilToInt(length*rate);var data=new float[size];
            for(int i=0;i<size;i++){
                float p=i/(float)(size-1),t=i/(float)rate,envelope=Mathf.Sin(Mathf.PI*p)*Mathf.Exp(-p*3);
                float frequency=style==1?660:style==2?180:820;
                float signal=Mathf.Sin(2*Mathf.PI*frequency*t)*.32f;
                if(style==1)signal+=Mathf.Sin(2*Mathf.PI*990*t)*Mathf.Sin(Mathf.PI*p)*.20f;
                else if(style==2)signal+=Mathf.Sin(2*Mathf.PI*147*t)*.10f;
                data[i]=signal*envelope;
            }
            var clip=AudioClip.Create(name,size,1,rate,false);clip.SetData(data,0);return clip;
        }
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
            Mode=PlaybackMode.Gameplay;
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
            gameplaySource.volume=0;
            int next=order!=null?order.Next():-1;
            if(next<0)return;
            gameplaySource.clip=tracks[next];gameplaySource.Play();gameplayPaused=false;waitingForStart=true;startDeadline=Time.unscaledTime+3;
        }
        void Update()
        {
            float target=muted?0:Mathf.Clamp01(SaveSystem.Data.Volume);if(!Mathf.Approximately(AudioListener.volume,target))AudioListener.volume=target;
            // Fade music using real time so opening a paused menu never freezes the transition.
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*8);
            menuSource.volume=Mathf.Lerp(menuSource.volume,Mode==PlaybackMode.Menu?.75f:0,blend);
            gameplaySource.volume=Mathf.Lerp(gameplaySource.volume,Mode==PlaybackMode.Gameplay?.62f:0,blend);
            if(Mode!=PlaybackMode.Menu&&menuSource.isPlaying&&menuSource.volume<.003f)menuSource.Pause();
            if(Mode!=PlaybackMode.Gameplay||tracks.Length==0||AudioListener.pause)return;
            if(gameplaySource.isPlaying){waitingForStart=false;return;}
            // A streaming clip may need several frames before it can start; do not skip every frame while loading.
            if(waitingForStart&&Time.unscaledTime<startDeadline)return;
            waitingForStart=false;StartNextTrack();
        }
        void OnDestroy(){
            if(instance!=this)return;instance=null;
            if(ownsButtonClip&&buttonClip!=null)Destroy(buttonClip);
            if(purchaseClip!=null)Destroy(purchaseClip);if(rejectClip!=null)Destroy(rejectClip);
        }
        void OnApplicationFocus(bool value){focused=value;AudioListener.pause=!focused||applicationPaused;}
        void OnApplicationPause(bool value){applicationPaused=value;AudioListener.pause=!focused||applicationPaused;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){instance=null;muted=false;focused=true;applicationPaused=false;AudioListener.pause=false;}
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

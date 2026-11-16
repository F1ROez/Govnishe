using System;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Original synthesized effects, generated once. Bounded persistent voices survive UI/scene changes.</summary>
    public sealed class GameAudio:MonoBehaviour
    {
        const int VoiceCount=10,SampleRate=22050;
        static GameAudio instance;
        readonly AudioSource[] voices=new AudioSource[VoiceCount];
        readonly double[] started=new double[VoiceCount];
        AudioSource charge;
        AudioClip legacy,swing,heavySwing,hit,heavyHit,chargeLoop,waveStart,waveDone,challenge;
        float chargeTarget,chargePitch;
        int priorityCursor;
        public static GameAudio Instance=>instance;
        public static void Prewarm(){Ensure();}
        public int ActiveVoices{get{int count=0;foreach(var voice in voices)if(voice!=null&&voice.isPlaying)count++;return count;}}
        public bool Charging=>charge!=null&&charge.isPlaying;

        static GameAudio Ensure()
        {
            if(!Application.isPlaying)return null;
            if(instance!=null)return instance;
            var go=new GameObject("Звуки боя и событий");instance=go.AddComponent<GameAudio>();return instance;
        }
        void Awake()
        {
            if(instance!=null&&instance!=this){Destroy(gameObject);return;}
            instance=this;DontDestroyOnLoad(gameObject);
            for(int i=0;i<voices.Length;i++)voices[i]=Source("Эффект "+(i+1));
            charge=Source("Нарастание замаха");charge.loop=true;
            legacy=Create("Короткий сигнал",.11f,0);
            swing=Create("Свист биты",.19f,1);heavySwing=Create("Мощный взмах",.29f,2);
            hit=Create("Деревянный удар",.16f,3);heavyHit=Create("Тяжёлый удар",.32f,4);
            chargeLoop=Create("Петля подготовки",.4f,5);
            waveStart=Create("Начало волны",.42f,6);waveDone=Create("Волна завершена",.55f,7);challenge=Create("Испытание выполнено",.62f,8);
            charge.clip=chargeLoop;
        }
        AudioSource Source(string title)
        {
            var go=new GameObject(title);go.transform.SetParent(transform,false);var audio=go.AddComponent<AudioSource>();
            audio.playOnAwake=false;audio.spatialBlend=0;audio.loop=false;audio.ignoreListenerPause=false;return audio;
        }
        static AudioClip Create(string title,float seconds,int style)
        {
            int size=Mathf.CeilToInt(seconds*SampleRate);var data=new float[size];var random=new System.Random(7919+style*113);float low=0;
            for(int i=0;i<size;i++){
                float t=i/(float)SampleRate,p=i/(float)(size-1),noise=(float)(random.NextDouble()*2-1);low=Mathf.Lerp(low,noise,.16f);
                float envelope=Mathf.Sin(Mathf.PI*p),sample;
                switch(style){
                    case 1:sample=(noise-low)*Mathf.Pow(envelope,2)*.20f+low*envelope*.20f;break;
                    case 2:sample=low*Mathf.Pow(envelope,1.2f)*.45f+Mathf.Sin(2*Mathf.PI*(120*t-90*t*t))*envelope*.22f;break;
                    case 3:sample=(Mathf.Sin(2*Mathf.PI*190*t)*.55f+low*.6f+noise*.2f)*Mathf.Exp(-p*9)*Mathf.Min(1,p*35);break;
                    case 4:sample=(Mathf.Sin(2*Mathf.PI*(92*t-38*t*t))*.7f+Mathf.Sin(2*Mathf.PI*220*t)*.15f+low*.8f+noise*.16f)*Mathf.Exp(-p*7)*Mathf.Min(1,p*60);break;
                    // Integer periods and equal end envelopes avoid clicks at the loop seam.
                    case 5:sample=(Mathf.Sin(2*Mathf.PI*140*t)*.45f+Mathf.Sin(2*Mathf.PI*210*t)*.12f)*(.9f+.1f*Mathf.Cos(2*Mathf.PI*2.5f*t));break;
                    case 6:case 7:case 8:
                        int step=Mathf.Min(style==6?1:2,Mathf.FloorToInt(p*(style==6?2:3)));
                        float note=style==6?(step==0?220:330):(step==0?440:step==1?554.365f:659.255f);
                        float local=p*(style==6?2:3)-step;
                        sample=(Mathf.Sin(2*Mathf.PI*note*t)*.58f+Mathf.Sin(2*Mathf.PI*note*2*t)*.12f)*Mathf.Sin(Mathf.PI*Mathf.Clamp01(local))*(1-p*.35f);break;
                    default:sample=Mathf.Sin(2*Mathf.PI*220*t)*Mathf.Exp(-p*6)*Mathf.Min(1,p*30);break;
                }
                data[i]=Mathf.Clamp(sample,-.85f,.85f);
            }
            var clip=AudioClip.Create(title,size,1,SampleRate,false);clip.SetData(data,0);return clip;
        }
        bool MayPlay=>!AudioListener.pause&&(GameManager.Instance==null||GameManager.Instance.IsPlaying);
        void Emit(AudioClip clip,float volume,float pitch=1)
        {
            if(clip==null||!MayPlay)return;
            int selected=-1;double earliest=double.MaxValue;
            for(int i=0;i<voices.Length;i++){
                int index=(priorityCursor+i)%voices.Length;
                if(!voices[index].isPlaying){selected=index;break;}
                if(started[index]<earliest){earliest=started[index];selected=index;}
            }
            var audio=voices[selected];audio.Stop();audio.clip=clip;audio.pitch=Mathf.Clamp(pitch,.5f,2);audio.volume=Mathf.Clamp01(volume);audio.Play();
            started[selected]=AudioSettings.dspTime;priorityCursor=(selected+1)%voices.Length;
        }
        public static void Play(float frequency,float duration,float volume)
        {
            var audio=Ensure();if(audio==null)return;
            audio.Emit(audio.legacy,volume,Mathf.Clamp(frequency/220,.5f,2));
        }
        public static void Swing(bool heavy){var audio=Ensure();if(audio!=null)audio.Emit(heavy?audio.heavySwing:audio.swing,heavy?.46f:.28f);}
        public static void Impact(bool heavy,bool critical,int hits)
        {
            if(hits<=0)return;var audio=Ensure();if(audio==null)return;
            // Exactly one impact per swing: additional targets add a capped amount of weight.
            audio.Emit(heavy?audio.heavyHit:audio.hit,(heavy?.76f:.48f)+Mathf.Min(3,hits-1)*.035f,critical?1.08f:1);
        }
        public static void BeginCharge(){var audio=Ensure();if(audio==null||!audio.MayPlay)return;audio.chargeTarget=.025f;audio.chargePitch=.8f;audio.charge.volume=0;audio.charge.pitch=.8f;if(!audio.charge.isPlaying)audio.charge.Play();}
        public static void SetCharge(float progress,bool ready)
        {
            if(!ready){EndCharge();return;}
            var audio=Ensure();if(audio==null||!audio.MayPlay)return;
            if(!audio.charge.isPlaying)BeginCharge();progress=Mathf.Clamp01(progress);
            audio.chargeTarget=Mathf.Lerp(.025f,.14f,progress);audio.chargePitch=Mathf.Lerp(.8f,1.3f,progress);
        }
        public static void EndCharge(){if(instance==null||instance.charge==null)return;instance.chargeTarget=0;instance.charge.volume=0;instance.charge.Stop();}
        public static void PlayWave(bool complete=false){var audio=Ensure();if(audio!=null)audio.Emit(complete?audio.waveDone:audio.waveStart,.24f);}
        public static void PlayChallenge(){var audio=Ensure();if(audio!=null)audio.Emit(audio.challenge,.26f);}
        public static void StopGameplayEffects()
        {
            if(instance==null)return;EndCharge();foreach(var voice in instance.voices)if(voice!=null)voice.Stop();
        }
        void Update()
        {
            if(!MayPlay){StopGameplayEffects();return;}
            if(charge.isPlaying){float speed=1-Mathf.Exp(-Time.unscaledDeltaTime*15);charge.volume=Mathf.Lerp(charge.volume,chargeTarget,speed);charge.pitch=Mathf.Lerp(charge.pitch,chargePitch,speed);}
        }
        void OnDestroy()
        {
            if(instance!=this)return;instance=null;
            foreach(var clip in new[]{legacy,swing,heavySwing,hit,heavyHit,chargeLoop,waveStart,waveDone,challenge})if(clip!=null)Destroy(clip);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){instance=null;}
    }
}

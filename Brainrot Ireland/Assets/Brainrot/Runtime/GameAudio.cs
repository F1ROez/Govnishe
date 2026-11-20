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
        AudioClip legacy,swing,heavySwing,hit,heavyHit,chargeLoop,waveStart,waveDone,challenge,healing,coin,throwWhoosh,catchTap,dash,hurt,market;
        float chargeTarget,chargePitch,lastCoinAt,coinPitch=1;
        int priorityCursor;bool suspended;
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
            coin=Create("Звон монеты",.13f,10);healing=Create("Подбор сердечка",.28f,9);charge.clip=chargeLoop;
            throwWhoosh=Create("Вращающийся бросок",.28f,11);catchTap=Create("Возврат в ладонь",.10f,12);
            dash=Create("Воздушный рывок",.18f,13);hurt=Create("Получение урона",.18f,14);market=Create("Появление лавки",.38f,15);
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
                // Float sin(pi) may be slightly negative: fractional powers would create NaN samples.
                float envelope=Mathf.Max(0,Mathf.Sin(Mathf.PI*p)),sample;
                switch(style){
                    case 1:sample=(noise-low)*Mathf.Pow(envelope,2)*.20f+low*envelope*.20f;break;
                    case 2:sample=low*Mathf.Pow(envelope,1.2f)*.45f+Mathf.Sin(2*Mathf.PI*(120*t-90*t*t))*envelope*.22f;break;
                    case 3:sample=(Mathf.Sin(2*Mathf.PI*190*t)*.55f+low*.6f+noise*.2f)*Mathf.Exp(-p*9)*Mathf.Min(1,p*35);break;
                    case 4:sample=(Mathf.Sin(2*Mathf.PI*(92*t-38*t*t))*.7f+Mathf.Sin(2*Mathf.PI*220*t)*.15f+low*.8f+noise*.16f)*Mathf.Exp(-p*7)*Mathf.Min(1,p*60);break;
                    // Integer periods and equal end envelopes avoid clicks at the loop seam.
                    case 5:sample=(Mathf.Sin(2*Mathf.PI*140*t)*.45f+Mathf.Sin(2*Mathf.PI*210*t)*.12f)*(.9f+.1f*Mathf.Cos(2*Mathf.PI*2.5f*t));break;
                    case 10:sample=(Mathf.Sin(2*Mathf.PI*1245*t)*.4f+Mathf.Sin(2*Mathf.PI*2490*t)*.15f)*Mathf.Exp(-p*5)*Mathf.Min(1,p*30);break;
                    case 11:sample=(noise-low)*envelope*.22f+low*envelope*.28f*(.55f+.45f*Mathf.Sin(2*Mathf.PI*15*t));break;
                    case 12:sample=(Mathf.Sin(2*Mathf.PI*280*t)*.40f+low*.38f)*Mathf.Exp(-p*8)*Mathf.Min(1,p*45);break;
                    case 13:sample=(noise-low)*Mathf.Pow(envelope,1.4f)*.20f+Mathf.Sin(2*Mathf.PI*(240*t+450*t*t))*envelope*.12f;break;
                    case 14:sample=(Mathf.Sin(2*Mathf.PI*(118*t-100*t*t))*.48f+low*.34f)*Mathf.Exp(-p*6)*Mathf.Min(1,p*35);break;
                    case 15:sample=(Mathf.Sin(2*Mathf.PI*440*t)*.34f+Mathf.Sin(2*Mathf.PI*659.255f*t)*.19f)*envelope*Mathf.Exp(-p*2);break;
                    case 9:sample=(Mathf.Sin(2*Mathf.PI*(660*t+540*t*t))*.5f+Mathf.Sin(2*Mathf.PI*1320*t)*.12f)*envelope*(1-p);break;
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
        public static void PlayThrow(){var audio=Ensure();if(audio!=null)audio.Emit(audio.throwWhoosh,.32f);}
        public static void PlayCatch(){var audio=Ensure();if(audio!=null)audio.Emit(audio.catchTap,.20f);}
        public static void PlayDash(){var audio=Ensure();if(audio!=null)audio.Emit(audio.dash,.18f);}
        public static void PlayHurt(){var audio=Ensure();if(audio!=null)audio.Emit(audio.hurt,.28f);}
        public static void PlayMarketAppear(){var audio=Ensure();if(audio!=null)audio.Emit(audio.market,.20f);}
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
        public static void PlayCoin(){var audio=Ensure();if(audio==null)return;audio.coinPitch=Time.unscaledTime-audio.lastCoinAt<.5f?Mathf.Min(1.4f,audio.coinPitch+.055f):1;audio.lastCoinAt=Time.unscaledTime;audio.Emit(audio.coin,.16f,audio.coinPitch);}
        public static void PlayHealing(){var audio=Ensure();if(audio!=null)audio.Emit(audio.healing,.24f);}
        public static void PlayChallenge(){var audio=Ensure();if(audio!=null)audio.Emit(audio.challenge,.26f);}
        public static void StopGameplayEffects()
        {
            if(instance==null)return;EndCharge();foreach(var voice in instance.voices)if(voice!=null)voice.Stop();
        }
        void Update()
        {
            if(!MayPlay){if(!suspended){StopGameplayEffects();suspended=true;}return;}
            suspended=false;
            if(charge.isPlaying){float speed=1-Mathf.Exp(-Time.unscaledDeltaTime*15);charge.volume=Mathf.Lerp(charge.volume,chargeTarget,speed);charge.pitch=Mathf.Lerp(charge.pitch,chargePitch,speed);}
        }
        void OnDestroy()
        {
            if(instance!=this)return;instance=null;
            foreach(var clip in new[]{legacy,swing,heavySwing,hit,heavyHit,chargeLoop,waveStart,waveDone,challenge,healing,coin,throwWhoosh,catchTap,dash,hurt,market})if(clip!=null)Destroy(clip);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){instance=null;}
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Brainrot
{
    /// <summary>Live, text-independent quest data. Values can be compared without allocating HUD strings.</summary>
    public struct ChallengeCardData
    {
        public int Id,Current,Target;
        public string Title,Detail,Icon;
        public bool Complete,Percent;
        public bool Visible=>Id!=0;
        public float Progress=>Target>0?Mathf.Clamp01((float)Current/Target):0;
    }

    public static class QuestHUD
    {
        public static Text Label(Transform parent,string name,string text,int size,Vector2 min,Vector2 max,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=go.GetComponent<Text>();label.text=text;label.fontSize=size;label.alignment=alignment;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;CartoonUI.Typography(label,true);label.resizeTextMinSize=Mathf.Max(9,size-3);
            label.GetComponent<Outline>().effectDistance=new Vector2(1,-1);label.GetComponent<Shadow>().effectDistance=new Vector2(0,-1.5f);return label;
        }
        public static HudProgressMeter Meter(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        {
            var rim=CartoonUI.Image(name,parent,min,max,new Color(1,.79f,.18f));
            var track=CartoonUI.Image("Утопленный желоб",rim.transform,new Vector2(.008f,.12f),new Vector2(.992f,.88f),new Color(.015f,.12f,.22f));
            var fill=CartoonUI.Image("Прогресс",track.transform,new Vector2(.005f,.09f),new Vector2(.995f,.91f),color);
            CartoonUI.Image("Блик",fill.transform,new Vector2(.025f,.57f),new Vector2(.975f,.92f),new Color(.8f,1,1,.65f));
            var meter=rim.gameObject.AddComponent<HudProgressMeter>();meter.Fill=fill;return meter;
        }
    }

    /// <summary>Rounded progress without changing the track or allocating formatted strings every frame.</summary>
    public sealed class HudProgressMeter:MonoBehaviour
    {
        public Image Fill;
        float shown,target;bool initialized;
        public float Value=>target;
        public void Set(float value,bool snap=false)
        {
            target=float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
            if(!initialized||snap){shown=target;initialized=true;Apply();}
        }
        public void Tick(float dt){if(Mathf.Approximately(shown,target))return;shown=Mathf.MoveTowards(shown,target,Mathf.Max(0,dt)*1.65f);Apply();}
        void Update(){Tick(Time.unscaledDeltaTime);}
        void Apply()
        {
            if(Fill==null)return;
            var rect=Fill.rectTransform;var right=rect.anchorMax;float x=Mathf.Lerp(.005f,.995f,shown);if(!Mathf.Approximately(right.x,x)){right.x=x;rect.anchorMax=right;}
            bool visible=shown>.0001f;if(Fill.gameObject.activeSelf!=visible)Fill.gameObject.SetActive(visible);
        }
    }

    public sealed class HudQuestCard:MonoBehaviour
    {
        Text title,detail,counter,check;
        Image icon,body;
        HudProgressMeter meter;
        ChallengeCardData shown;
        float celebrateUntil;
        int lastId;
        public bool Complete=>shown.Complete;
        public int QuestId=>shown.Id;
        public void ResetPresentation(){lastId=0;shown=default;celebrateUntil=0;transform.localScale=Vector3.one;}
        public void Build()
        {
            var rim=CartoonUI.Image("Золотой кант",transform,Vector2.zero,Vector2.one,new Color(.98f,.73f,.18f));
            body=CartoonUI.Image("Морская эмаль",rim.transform,new Vector2(.009f,.035f),new Vector2(.991f,.965f),new Color(.035f,.24f,.38f,.97f));
            CartoonUI.Image("Верхний блик",body.transform,new Vector2(.02f,.92f),new Vector2(.98f,.98f),new Color(.35f,.93f,1,.55f));
            icon=CartoonUI.Image("Знак испытания",body.transform,new Vector2(.035f,.43f),new Vector2(.18f,.92f),Color.white,CartoonUI.Heavy);icon.preserveAspect=true;
            title=QuestHUD.Label(body.transform,"Название","",14,new Vector2(.20f,.59f),new Vector2(.95f,.95f));
            detail=QuestHUD.Label(body.transform,"Условие","",11,new Vector2(.04f,.32f),new Vector2(.96f,.62f));detail.color=new Color(.78f,.94f,1);
            meter=QuestHUD.Meter(body.transform,"Выполнение",new Vector2(.045f,.12f),new Vector2(.69f,.29f),new Color(.04f,.84f,1));
            counter=QuestHUD.Label(body.transform,"Счётчик","",12,new Vector2(.71f,.08f),new Vector2(.96f,.31f),TextAnchor.MiddleRight);
            check=QuestHUD.Label(body.transform,"Выполнено","✓",22,new Vector2(.04f,.43f),new Vector2(.18f,.91f),TextAnchor.MiddleCenter);check.color=new Color(.65f,1,.35f);check.gameObject.SetActive(false);
        }
        public void Present(ChallengeCardData value)
        {
            if(!value.Visible){if(gameObject.activeSelf)gameObject.SetActive(false);return;}
            if(!gameObject.activeSelf)gameObject.SetActive(true);
            bool switched=value.Id!=lastId;
            if(switched){lastId=value.Id;shown=default;transform.localScale=Vector3.one;}
            if(value.Complete&&!shown.Complete&&!switched)celebrateUntil=Time.unscaledTime+.65f;
            if(value.Title!=shown.Title)title.text=value.Title;
            if(value.Detail!=shown.Detail||value.Complete!=shown.Complete)detail.text=value.Complete?"ИСПЫТАНИЕ ВЫПОЛНЕНО!":value.Detail;
            if(value.Current!=shown.Current||value.Target!=shown.Target||value.Complete!=shown.Complete||switched)
                counter.text=value.Complete?"ГОТОВО":value.Percent?value.Current+"%":value.Current+" / "+value.Target;
            if(value.Icon!=shown.Icon||switched)icon.sprite=value.Icon=="Heart"?CartoonUI.Heart:TropicalUIArt.Get(value.Icon);
            if(value.Complete!=shown.Complete||switched){check.gameObject.SetActive(value.Complete);icon.enabled=!value.Complete;body.color=value.Complete?new Color(.06f,.35f,.20f,.98f):new Color(.035f,.24f,.38f,.97f);}
            meter.Set(value.Progress,switched);shown=value;
        }
        void Update()
        {
            float remaining=celebrateUntil-Time.unscaledTime;
            float pop=remaining>0?Mathf.Sin((1-remaining/.65f)*Mathf.PI)*.035f:0;
            if(!Mathf.Approximately(transform.localScale.x,1+pop))transform.localScale=Vector3.one*(1+pop);
        }
    }

    /// <summary>Unobtrusive start/clear ribbon; it never intercepts gameplay input.</summary>
    public sealed class HudWaveNotice:MonoBehaviour
    {
        CanvasGroup group;Text title,subtitle;float started,duration;
        public void Build()
        {
            group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=group.interactable=false;group.alpha=0;
            TropicalUIArt.Place("Морская лента",transform,Vector2.zero,Vector2.one,"StageRibbon");
            title=QuestHUD.Label(transform,"Событие волны","",21,new Vector2(.12f,.43f),new Vector2(.88f,.94f),TextAnchor.MiddleCenter);
            subtitle=QuestHUD.Label(transform,"Описание","",12,new Vector2(.10f,.12f),new Vector2(.90f,.46f),TextAnchor.MiddleCenter);
        }
        public void Show(int wave,bool complete,string eventTitle)
        {
            title.text=complete?"ВОЛНА "+wave+" ПРОЙДЕНА!":"ВОЛНА "+wave;
            subtitle.text=complete?"ПЕРЕВЕДИ ДУХ. ОКЕАН ЕЩЁ НЕ УСПОКОИЛСЯ":!string.IsNullOrEmpty(eventTitle)?eventTitle:wave==2?"ОСЬМИНОГ ВЫХОДИТ ИЗ ВОДЫ":wave==3?"ПРИБЛИЖАЕТСЯ ОРКАЛАДОН":"ДЕРЖИ ОБОРОНУ ОСТРОВА";
            started=Time.unscaledTime;duration=complete?2.6f:2.2f;group.alpha=0;
        }
        public void Hide(){duration=0;if(group!=null)group.alpha=0;}
        void Update()
        {
            float age=Time.unscaledTime-started;
            float fade=Mathf.Clamp01(age/.18f)*Mathf.Clamp01((duration-age)/.35f);
            group.alpha=fade;transform.localScale=Vector3.one*Mathf.Lerp(.94f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.25f)));
        }
    }
}

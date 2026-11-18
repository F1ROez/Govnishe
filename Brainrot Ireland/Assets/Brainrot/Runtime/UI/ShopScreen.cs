using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Brainrot
{
    /// <summary>Four permanent upgrades with real prices and persistent ranks; skins remain a separate future catalogue.</summary>
    public sealed class ShopScreen:MonoBehaviour
    {
        Transform host;GameObject content;UnityAction back;bool skins;string feedback="";float feedbackUntil;Text footer;
        const string PermanentNote="ПОСТОЯННЫЕ УЛУЧШЕНИЯ: БОНУСЫ РАБОТАЮТ В КАЖДОЙ ВОЛНЕ И КАЖДОМ НОВОМ ЗАБЕГЕ.";
        static readonly string[] Icons={"IslandBat","IslandHealth","IslandShoe","IslandStopwatch"};
        public static ShopScreen Show(Transform parent,UnityAction onBack){
            var go=new GameObject("Магазин острова",typeof(RectTransform));go.transform.SetParent(parent,false);ReferenceScreens.Rect((RectTransform)go.transform,0,0,1,1);
            var page=go.AddComponent<ShopScreen>();page.host=go.transform;page.back=onBack;page.Build();return page;
        }
        void Update(){if(footer!=null&&feedback.Length>0&&Time.unscaledTime>=feedbackUntil){feedback="";footer.text=PermanentNote;}}
        void Build(int focus=-1){
            if(content!=null){content.SetActive(false);Destroy(content);}
            content=new GameObject("Содержимое магазина",typeof(RectTransform));content.transform.SetParent(host,false);ReferenceScreens.Rect((RectTransform)content.transform,0,0,1,1);
            var frame=ReferenceScreens.Panel(content.transform,"МАГАЗИН ОСТРОВА",.12f,.155f,.76f,.82f,46,-.245f);
            ReferenceScreens.Wallet(frame,.665f,.075f,.295f,.082f,true);
            var upgrades=ReferenceScreens.SeaButton(frame,"УЛУЧШЕНИЯ",.05f,.072f,.29f,.10f,()=>{skins=false;feedback="";Build();},29,!skins,null,false);
            var skinTab=ReferenceScreens.SeaButton(frame,"СКИНЫ",.342f,.072f,.23f,.10f,()=>{skins=true;feedback="";Build();},29,skins,null,false);
            Button selected=null;
            if(skins){
                var portrait=ReferenceScreens.Art(frame,"Portrait",.40f,.245f,.20f,.31f);portrait.preserveAspect=true;
                ReferenceScreens.SeaText(frame,"СКИНЫ ПОЯВЯТСЯ ПОЗЖЕ",32,.12f,.60f,.76f,.07f);
                ReferenceScreens.SeaText(frame,"Новые образы Сахура будут здесь.",23,.12f,.70f,.76f,.055f);
            }else{
                foreach(var item in MetaProgression.Catalog){
                    int id=item.Id,rank=MetaProgression.Rank(id),price=MetaProgression.Cost(id);bool capped=rank>=item.MaxRank;
                    float x=.045f+(id%2)*.475f,y=.212f+(id/2)*.28f;
                    // Sand texture is clipped by a rounded cream inset; no text, price or progress is baked into the artwork.
                    var rim=ReferenceScreens.Surface(frame,item.Title,x,y,.435f,.26f,new Color(.76f,.38f,.075f)).transform;
                    ReferenceScreens.Surface(rim,"Светлая кромка",.003f,.003f,.994f,.98f,new Color(1,.94f,.70f));
                    var mask=ReferenceScreens.Surface(rim,"Карточка",.006f,.009f,.988f,.97f,Color.white);var clipping=mask.gameObject.AddComponent<Mask>();clipping.showMaskGraphic=false;
                    ReferenceScreens.Art(mask.transform,"IslandSandTile",0,0,1,1);
                    var card=mask.transform;
                    var icon=ReferenceScreens.Art(card,Icons[id],.025f,.07f,.285f,.85f);icon.preserveAspect=true;
                    var title=ReferenceScreens.SeaText(card,item.Title,23,.345f,.065f,.49f,.20f);title.alignment=TextAnchor.MiddleLeft;
                    var rankPlate=ReferenceScreens.SlicedArt(card,"IslandSecondary",.835f,.055f,.14f,.21f).transform;
                    ReferenceScreens.SeaText(rankPlate,rank+"/"+item.MaxRank,20,.02f,.10f,.96f,.80f);
                    var description=ReferenceScreens.SeaText(card,item.Description,18,.345f,.29f,.615f,.25f);description.alignment=TextAnchor.MiddleLeft;
                    var buy=ReferenceScreens.SeaButton(card,capped?"МАКСИМУМ":"КУПИТЬ • "+price,.34f,.59f,.625f,.32f,()=>{
                        bool bought=MetaProgression.Buy(id);
                        feedback=bought?"КУПЛЕНО НАВСЕГДА! УЛУЧШЕНИЕ ДЕЙСТВУЕТ ВО ВСЕХ НОВЫХ ЗАБЕГАХ.":"ПОКУПКА НЕ СОХРАНЕНА. МОНЕТЫ ОСТАЛИСЬ В КОШЕЛЬКЕ.";
                        feedbackUntil=Time.unscaledTime+3;Build(id);
                    },28,true,null,false);
                    buy.interactable=!capped&&SaveSystem.Data.Coins>=price;
                    if(id==focus&&buy.interactable)selected=buy;
                }
            }
            footer=ReferenceScreens.SeaText(frame,feedback.Length>0?feedback:PermanentNote,17,.065f,.785f,.87f,.065f);
            var backButton=ReferenceScreens.SeaButton(frame,"НАЗАД",.275f,.865f,.45f,.15f,back,44,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(selected!=null?selected.gameObject:focus<0?(skins?skinTab:upgrades).gameObject:backButton.gameObject);
        }
    }
}

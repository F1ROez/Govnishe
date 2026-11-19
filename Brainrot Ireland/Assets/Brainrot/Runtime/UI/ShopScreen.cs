using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Brainrot
{
    /// <summary>Separate cosmetic menu and the active attempt's between-wave upgrade stall.</summary>
    public sealed class ShopScreen:MonoBehaviour
    {
        Transform host;GameObject content;UnityAction back;bool runShop;string feedback="";float feedbackUntil;Text footer,wallet;
        int shownBalance=int.MinValue;readonly System.Collections.Generic.List<Button> purchases=new System.Collections.Generic.List<Button>(4);
        const string RunNote="УЛУЧШЕНИЯ ДЕЙСТВУЮТ СРАЗУ И ДО КОНЦА ЭТОГО ЗАБЕГА.";
        static readonly string[] Icons={"IslandBat","IslandHealth","IslandShoe","IslandStopwatch"};
        public static ShopScreen Show(Transform parent,UnityAction onBack)=>Create(parent,onBack,false);
        public static ShopScreen ShowRun(Transform parent,UnityAction onBack)=>Create(parent,onBack,true);
        static ShopScreen Create(Transform parent,UnityAction onBack,bool activeRun){
            var go=new GameObject(activeRun?"Лавка между волнами":"Магазин скинов",typeof(RectTransform));go.transform.SetParent(parent,false);ReferenceScreens.Rect((RectTransform)go.transform,0,0,1,1);
            var page=go.AddComponent<ShopScreen>();page.host=go.transform;page.back=onBack;page.runShop=activeRun;page.Build();return page;
        }
        bool RunContext=>runShop&&GameManager.Instance!=null&&GameManager.Instance.HasRun&&GameManager.Instance.State==GameState.RunShop;
        void Update(){
            if(!runShop)return;
            if(footer!=null&&feedback.Length>0&&Time.unscaledTime>=feedbackUntil){feedback="";footer.text=RunNote;}
            UpdateBalance();
        }
        void UpdateBalance(){
            var game=GameManager.Instance;int balance=game!=null?game.RunBalance:0;
            if(shownBalance==balance)return;shownBalance=balance;
            if(wallet!=null)wallet.text="МОНЕТЫ "+balance;
            for(int i=0;i<purchases.Count;i++)purchases[i].interactable=RunContext&&MetaProgression.Rank(i)<MetaProgression.Catalog[i].MaxRank&&balance>=MetaProgression.Cost(i);
        }
        void Build(int focus=-1){
            if(content!=null){content.SetActive(false);Destroy(content);}purchases.Clear();shownBalance=int.MinValue;wallet=null;
            content=new GameObject("Содержимое магазина",typeof(RectTransform));content.transform.SetParent(host,false);ReferenceScreens.Rect((RectTransform)content.transform,0,0,1,1);
            var frame=ReferenceScreens.Panel(content.transform,runShop?"ЛАВКА ОСТРОВА":"МАГАЗИН СКИНОВ",.12f,.155f,.76f,.82f,46,-.245f);
            if(!runShop){BuildSkins(frame);return;}
            if(!RunContext){
                ReferenceScreens.SeaText(frame,"ЛАВКА ОТКРЫВАЕТСЯ МЕЖДУ ВОЛНАМИ",30,.10f,.30f,.80f,.13f);
                ReferenceScreens.SeaText(frame,"Подойди к прилавку после зачистки и нажми E.",23,.15f,.47f,.70f,.11f);
                Back(frame);return;
            }
            var badge=ReferenceScreens.SlicedArt(frame,"IslandSecondary",.665f,.075f,.295f,.082f).transform;
            var coin=ReferenceScreens.Art(badge,"IslandCoin",-.04f,-.27f,.29f,1.54f);coin.preserveAspect=true;
            wallet=ReferenceScreens.SeaText(badge,"",24,.235f,.12f,.71f,.76f);
            ReferenceScreens.SeaText(frame,"ПОДГОТОВЬСЯ К НОВОЙ ВОЛНЕ",25,.055f,.078f,.57f,.078f);
            Button selected=null;
            foreach(var item in MetaProgression.Catalog){
                int id=item.Id,rank=MetaProgression.Rank(id),price=MetaProgression.Cost(id);bool capped=rank>=item.MaxRank;
                float x=.045f+(id%2)*.475f,y=.212f+(id/2)*.28f;
                var rim=ReferenceScreens.Surface(frame,item.Title,x,y,.435f,.26f,new Color(.76f,.38f,.075f)).transform;
                ReferenceScreens.Surface(rim,"Светлая кромка",.003f,.003f,.994f,.98f,new Color(1,.94f,.70f));
                var mask=ReferenceScreens.Surface(rim,"Карточка",.006f,.009f,.988f,.97f,Color.white);var clipping=mask.gameObject.AddComponent<Mask>();clipping.showMaskGraphic=false;
                ReferenceScreens.Art(mask.transform,"IslandSandTile",0,0,1,1);var card=mask.transform;
                var icon=ReferenceScreens.Art(card,Icons[id],.025f,.07f,.285f,.85f);icon.preserveAspect=true;
                var title=ReferenceScreens.SeaText(card,item.Title,23,.345f,.065f,.49f,.20f);title.alignment=TextAnchor.MiddleLeft;
                var rankPlate=ReferenceScreens.SlicedArt(card,"IslandSecondary",.835f,.055f,.14f,.21f).transform;
                ReferenceScreens.SeaText(rankPlate,rank+"/"+item.MaxRank,20,.02f,.10f,.96f,.80f);
                var description=ReferenceScreens.SeaText(card,item.Description,18,.345f,.29f,.615f,.25f);description.alignment=TextAnchor.MiddleLeft;
                var buy=ReferenceScreens.SeaButton(card,capped?"МАКСИМУМ":"КУПИТЬ • "+price,.34f,.59f,.625f,.32f,()=>{
                    bool bought=RunContext&&MetaProgression.Buy(id);
                    feedback=bought?"ГОТОВО! УЛУЧШЕНИЕ УЖЕ РАБОТАЕТ В ЭТОМ ЗАБЕГЕ.":"НЕДОСТАТОЧНО МОНЕТ ИЛИ ЛАВКА УЖЕ ЗАКРЫТА.";
                    feedbackUntil=Time.unscaledTime+2.6f;Build(id);
                },28,true,null,false);
                purchases.Add(buy);
                if(id==focus&&!capped)selected=buy;
            }
            footer=ReferenceScreens.SeaText(frame,feedback.Length>0?feedback:RunNote,18,.065f,.785f,.87f,.065f);
            var backButton=Back(frame);UpdateBalance();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(selected!=null&&selected.interactable?selected.gameObject:backButton.gameObject);
        }
        void BuildSkins(Transform frame){
            var portrait=ReferenceScreens.Art(frame,"Portrait",.405f,.22f,.19f,.34f);portrait.preserveAspect=true;
            ReferenceScreens.SeaText(frame,"НОВЫЕ ОБРАЗЫ САХУРА",34,.12f,.585f,.76f,.075f);
            ReferenceScreens.SeaText(frame,"СКИНЫ ПОЯВЯТСЯ ПОЗЖЕ",25,.12f,.685f,.76f,.065f);
            ReferenceScreens.SeaText(frame,"Боевые улучшения покупаются в лавке между волнами.",19,.10f,.775f,.80f,.055f);
            var button=Back(frame);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        Button Back(Transform frame)=>ReferenceScreens.SeaButton(frame,runShop?"ВЕРНУТЬСЯ НА ОСТРОВ":"НАЗАД",.235f,.865f,.53f,.15f,back,runShop?28:44,true);
    }
}

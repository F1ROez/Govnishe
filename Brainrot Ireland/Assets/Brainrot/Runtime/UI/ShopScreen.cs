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
        int shownBalance=int.MinValue,selectedOffer;readonly System.Collections.Generic.List<Button> purchases=new System.Collections.Generic.List<Button>(4);
        readonly System.Collections.Generic.List<int> purchaseIds=new System.Collections.Generic.List<int>(3);
        readonly System.Collections.Generic.List<Image> cardEdges=new System.Collections.Generic.List<Image>(3);
        readonly System.Collections.Generic.List<Image> dots=new System.Collections.Generic.List<Image>(3);
        const string RunNote="УЛУЧШЕНИЯ ДЕЙСТВУЮТ СРАЗУ И ДО КОНЦА ЭТОГО ЗАБЕГА.";
        static readonly string[] Icons={"IslandBatBare","IslandHealthBare","IslandShoeBare","IslandStopwatchBare"};
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
            if(wallet!=null)wallet.text=balance.ToString();
            for(int i=0;i<purchases.Count;i++){int id=purchaseIds[i];purchases[i].interactable=RunContext&&!game.Economy.IsOfferSold(id)&&MetaProgression.Rank(id)<MetaProgression.Catalog[id].MaxRank&&balance>=MetaProgression.Cost(id);}
        }
        void Build(int focus=-1){
            if(content!=null){content.SetActive(false);Destroy(content);}purchases.Clear();purchaseIds.Clear();cardEdges.Clear();dots.Clear();shownBalance=int.MinValue;wallet=null;
            content=new GameObject("Содержимое магазина",typeof(RectTransform));content.transform.SetParent(host,false);ReferenceScreens.Rect((RectTransform)content.transform,0,0,1,1);
            if(!runShop){BuildSkins(ReferenceScreens.Panel(content.transform,"МАГАЗИН СКИНОВ",.18f,.16f,.64f,.77f));return;}
            ReferenceScreens.Surface(content.transform,"Морское стекло",0,0,1,1,new Color(.012f,.12f,.16f,.87f));
            var header=ReferenceScreens.Art(content.transform,"IslandTitle",.19f,-.015f,.62f,.275f);
            ReferenceScreens.Text(header.transform,"ЛАВКА ОСТРОВА",43,.16f,.33f,.68f,.34f);
            var walletFrame=ReferenceScreens.Surface(content.transform,"Золотая кромка кошелька",.845f,.055f,.12f,.095f,new Color(1,.73f,.18f));
            var walletFace=ReferenceScreens.Surface(walletFrame.transform,"Тёмная эмаль",.018f,.025f,.964f,.95f,new Color(.07f,.085f,.073f));
            ReferenceScreens.Art(walletFace.transform,"IslandCoin",.08f,.16f,.28f,.68f).preserveAspect=true;
            wallet=ReferenceScreens.Text(walletFace.transform,"",30,.39f,.10f,.55f,.80f);
            if(!RunContext){
                ReferenceScreens.Text(content.transform,"ЛАВКА ОТКРЫВАЕТСЯ МЕЖДУ ВОЛНАМИ",30,.15f,.37f,.70f,.13f);
                ReferenceScreens.Text(content.transform,"Подойди к прилавку после зачистки и нажми E.",23,.20f,.52f,.60f,.10f);
                ReferenceScreens.SeaButton(content.transform,"НАЗАД",.36f,.80f,.28f,.09f,back,25,true,null,false);return;
            }
            var game=GameManager.Instance;game.Economy.EnsureOffers(game.Wave);var offers=game.Economy.OfferIds;
            int count=offers.Count;
            for(int index=0;index<count;index++){
                int id=offers[index];var item=MetaProgression.Catalog[id];int rank=MetaProgression.Rank(id),price=MetaProgression.Cost(id);bool sold=game.Economy.IsOfferSold(id);
                float width=.245f,gap=.019f,start=.5f-(count*width+(count-1)*gap)*.5f,x=start+index*(width+gap);
                var edge=ReferenceScreens.Surface(content.transform,item.Title+" — кромка",x,.285f,width,.525f,new Color(.04f,.64f,.73f));cardEdges.Add(edge);
                edge.sprite=IslandThemeSurface.Round;
                var glass=ReferenceScreens.Surface(edge.transform,"Морская карточка",.009f,.006f,.982f,.986f,Color.white);glass.sprite=IslandThemeSurface.Sea;var card=glass.transform;
                ReferenceScreens.Surface(card,"Верхний блик",.035f,.012f,.93f,.008f,new Color(.40f,.91f,.96f,.28f));
                var rankFrame=ReferenceScreens.SlicedArt(card,"IslandSecondary",.72f,.035f,.23f,.09f).transform;
                ReferenceScreens.SeaText(rankFrame,rank+"/"+item.MaxRank,22,.02f,.08f,.96f,.84f);
                var icon=ReferenceScreens.Art(card,Icons[id],.20f,.09f,.60f,.33f);icon.preserveAspect=true;
                ReferenceScreens.Text(card,item.Title,25,.045f,.455f,.91f,.11f);
                ReferenceScreens.Text(card,item.Description,20,.075f,.575f,.85f,.155f);
                var buy=ReferenceScreens.SeaButton(card,sold?"ПРОДАНО":price.ToString(),.15f,.79f,.70f,.145f,()=>{
                    bool bought=RunContext&&MetaProgression.Buy(id);
                    feedback=bought?"КУПЛЕНО — УЛУЧШЕНИЕ УЖЕ РАБОТАЕТ":"НЕДОСТАТОЧНО МОНЕТ ИЛИ ПРЕДЛОЖЕНИЕ УЖЕ ПРОДАНО";
                    feedbackUntil=Time.unscaledTime+2.6f;Build(id);
                },30,true,null,false);
                buy.gameObject.name="Купить улучшение "+id;
                if(!sold)ReferenceScreens.Art(buy.targetGraphic.transform,"IslandCoin",.19f,.22f,.16f,.56f).preserveAspect=true;
                var priceLabel=buy.GetComponentInChildren<Text>();ReferenceScreens.Rect(priceLabel.rectTransform,sold?.08f:.38f,.14f,sold?.84f:.45f,.72f);
                int selection=index;glass.raycastTarget=true;
                var cardEvents=card.gameObject.AddComponent<EventTrigger>();var hover=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};hover.callback.AddListener(_=>SelectOffer(selection,false));cardEvents.triggers.Add(hover);
                var events=buy.gameObject.AddComponent<EventTrigger>();
                foreach(var eventType in new[]{EventTriggerType.PointerEnter,EventTriggerType.Select}){
                    var entry=new EventTrigger.Entry{eventID=eventType};entry.callback.AddListener(_=>SelectOffer(selection,false));events.triggers.Add(entry);
                }
                purchases.Add(buy);purchaseIds.Add(id);
                var dot=CartoonUI.Image("Предложение "+(index+1),content.transform,Vector2.zero,Vector2.one,Color.white,CartoonUI.Disc);
                ReferenceScreens.Rect(dot.rectTransform,.5f+(index-(count-1)*.5f)*.033f-.007f,.865f,.014f,.022f);dots.Add(dot);
                if(id==focus)selectedOffer=index;
            }
            if(count==0)ReferenceScreens.Text(content.transform,"ВСЕ УЛУЧШЕНИЯ ЭТОГО ЗАБЕГА МАКСИМАЛЬНЫ",29,.16f,.41f,.68f,.15f);
            Arrow(.055f,false);Arrow(.89f,true);
            footer=ReferenceScreens.Text(content.transform,feedback.Length>0?feedback:RunNote,15,.16f,.93f,.68f,.035f);
            ReferenceScreens.SeaButton(content.transform,"НАЗАД · ESC",.39f,.975f,.22f,.045f,back,14,false,null,false);
            UpdateBalance();SelectOffer(selectedOffer);
        }
        void SelectOffer(int index,bool focus=true){
            if(purchases.Count==0)return;selectedOffer=(index%purchases.Count+purchases.Count)%purchases.Count;
            for(int i=0;i<cardEdges.Count;i++){
                cardEdges[i].color=i==selectedOffer?new Color(1,.73f,.13f):new Color(.04f,.64f,.73f);
                dots[i].color=i==selectedOffer?new Color(1,.74f,.12f):new Color(.31f,.55f,.57f,.65f);
            }
            if(focus&&EventSystem.current!=null&&purchases[selectedOffer].interactable)EventSystem.current.SetSelectedGameObject(purchases[selectedOffer].gameObject);
        }
        void Arrow(float x,bool next){
            var hit=ReferenceScreens.Surface(content.transform,next?"Следующее предложение":"Предыдущее предложение",x,.48f,.055f,.105f,Color.clear);hit.raycastTarget=true;
            var rim=CartoonUI.Image("Круглая золотая рамка",hit.transform,Vector2.zero,Vector2.one,new Color(.90f,.62f,.15f),CartoonUI.Disc);
            var face=CartoonUI.Image("Тёмная кнопка",rim.transform,new Vector2(.06f,.06f),new Vector2(.94f,.94f),new Color(.10f,.14f,.14f),CartoonUI.Disc);
            var symbol=IslandMenuSymbol.Place(face.transform,IslandSymbol.Play,.13f,.12f,.74f,.76f,new Color(1,.78f,.23f));if(!next)symbol.transform.localScale=new Vector3(-1,1,1);
            var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;
            var motion=hit.gameObject.AddComponent<CartoonButtonMotion>();motion.VisualTarget=rim.transform;motion.BaseColor=face.color;button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(()=>SelectOffer(selectedOffer+(next?1:-1)));button.interactable=purchases.Count>1;
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

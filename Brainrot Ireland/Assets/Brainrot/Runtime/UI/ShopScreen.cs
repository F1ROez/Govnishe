using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Brainrot
{
    /// <summary>Persistent purchases between runs; every card states its next-run effect and capped rank.</summary>
    public sealed class ShopScreen:MonoBehaviour
    {
        Transform host;GameObject content;UnityAction back;bool skins,continuing;string feedback="";
        public static ShopScreen Show(Transform parent,UnityAction onBack,bool hasContinuingRun=false){
            var go=new GameObject("Магазин острова",typeof(RectTransform));go.transform.SetParent(parent,false);ReferenceScreens.Rect((RectTransform)go.transform,0,0,1,1);
            var page=go.AddComponent<ShopScreen>();page.host=go.transform;page.back=onBack;page.continuing=hasContinuingRun;page.Build();return page;
        }
        void Build(){
            if(content!=null){content.SetActive(false);Destroy(content);}
            content=new GameObject("Содержимое магазина",typeof(RectTransform));content.transform.SetParent(host,false);ReferenceScreens.Rect((RectTransform)content.transform,0,0,1,1);
            var frame=ReferenceScreens.Panel(content.transform,"МАГАЗИН ОСТРОВА",.18f,.07f,.64f,.87f);
            var coins=ReferenceScreens.SeaText(frame,"МОНЕТЫ  "+SaveSystem.Data.Coins,23,.60f,.205f,.33f,.06f);coins.alignment=TextAnchor.MiddleRight;
            ReferenceScreens.SeaButton(frame,"УЛУЧШЕНИЯ",.07f,.205f,.25f,.065f,()=>{skins=false;feedback="";Build();},17,!skins);
            ReferenceScreens.SeaButton(frame,"СКИНЫ",.335f,.205f,.20f,.065f,()=>{skins=true;feedback="";Build();},17,skins);
            if(skins){
                ReferenceScreens.Art(frame,"Portrait",.40f,.36f,.20f,.25f).preserveAspect=true;
                ReferenceScreens.SeaText(frame,"СКИНЫ ПОЯВЯТСЯ ПОЗЖЕ",24,.12f,.65f,.76f,.07f);
                ReferenceScreens.SeaText(frame,"Новые образы Сахура будут здесь.",17,.12f,.73f,.76f,.045f);
            }else{
                int index=0;
                foreach(var item in MetaProgression.Catalog){
                    int id=item.Id;int rank=MetaProgression.Rank(id);int price=MetaProgression.Cost(id);bool capped=rank>=item.MaxRank;
                    float x=.065f+(index%2)*.455f,y=.315f+(index/2)*.235f;
                    var card=ReferenceScreens.Surface(frame,item.Title,x,y,.415f,.21f,new Color(.89f,.82f,.65f)).transform;
                    var name=ReferenceScreens.SeaText(card,item.Title,20,.065f,.045f,.68f,.20f);name.alignment=TextAnchor.MiddleLeft;
                    ReferenceScreens.SeaText(card,rank+" / "+item.MaxRank,15,.74f,.05f,.20f,.17f);
                    var description=ReferenceScreens.SeaText(card,item.Description,15,.065f,.27f,.87f,.30f);description.alignment=TextAnchor.MiddleLeft;
                    for(int pip=0;pip<item.MaxRank;pip++)ReferenceScreens.Surface(card,"Уровень "+(pip+1),.08f+pip*.17f,.57f,.14f,.025f,pip<rank?new Color(.10f,.58f,.50f):new Color(.62f,.58f,.44f));
                    var buy=ReferenceScreens.SeaButton(card,capped?"МАКСИМУМ":"КУПИТЬ · "+price,.08f,.64f,.84f,.27f,()=>{
                        feedback=MetaProgression.Buy(id)?"УЛУЧШЕНИЕ КУПЛЕНО — ДЕЙСТВУЕТ СО СЛЕДУЮЩЕГО ЗАБЕГА!":"НЕ УДАЛОСЬ КУПИТЬ. ПРОВЕРЬ БАЛАНС И СОХРАНЕНИЕ.";Build();
                    },16,true);
                    buy.interactable=!capped&&SaveSystem.Data.Coins>=price;
                    index++;
                }
            }
            string note=!string.IsNullOrEmpty(feedback)?feedback:continuing?"ПОКУПКИ ДЕЙСТВУЮТ В НОВОМ ЗАБЕГЕ. СОХРАНЁННЫЙ ЗАБЕГ ПРОДОЛЖИТСЯ С ПРЕЖНИМИ ХАРАКТЕРИСТИКАМИ.":"УЛУЧШЕНИЯ ОСТАЮТСЯ НАВСЕГДА И ПРИМЕНЯЮТСЯ В НОВОМ ЗАБЕГЕ.";
            ReferenceScreens.SeaText(frame,note,13,.08f,.80f,.84f,.07f);
            var backButton=ReferenceScreens.SeaButton(frame,"НАЗАД",.34f,.895f,.32f,.07f,back,20,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }
    }
}

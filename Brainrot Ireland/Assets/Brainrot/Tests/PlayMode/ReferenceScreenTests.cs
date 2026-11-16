using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Brainrot.Tests
{
    public sealed class ReferenceScreenTests
    {
        [TestCase(1280f,720f,1f)]
        [TestCase(2560f,1080f,1.5f)]
        [TestCase(1024f,768f,.8f)]
        public void CompositionFitsViewportWithoutStretching(float width,float height,float scale){
            var parent=new GameObject("Canvas bounds",typeof(RectTransform));
            try{
                var rect=parent.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(width,height);
                var viewport=ReferenceScreens.Viewport(rect,"Reference");viewport.GetComponent<ReferenceViewport>().Fit();
                Assert.AreEqual(scale,viewport.localScale.x,.001f);Assert.AreEqual(viewport.localScale.x,viewport.localScale.y,.001f);
                Assert.LessOrEqual(viewport.rect.width*scale,width+.01f);Assert.LessOrEqual(viewport.rect.height*scale,height+.01f);
            }finally{Object.DestroyImmediate(parent);}
        }
        [Test]public void HoverScalesArtAndTextWhileHitboxRemainsStable(){
            var parent=new GameObject("Buttons",typeof(RectTransform));
            try{
                parent.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
                var button=ReferenceScreens.Hotspot(parent.transform,"ПРОДОЛЖИТЬ",.2f,.2f,.3f,.1f,()=>{});
                var bounds=(RectTransform)button.transform;var before=bounds.rect;
                var motion=button.GetComponent<CartoonButtonMotion>();motion.OnPointerEnter(null);
                for(int i=0;i<60;i++)motion.Tick(1/60f);
                Assert.Greater(button.targetGraphic.transform.localScale.x,1.05f);
                Assert.AreEqual(Vector3.one,button.transform.localScale);Assert.AreEqual(before,bounds.rect);
                Assert.IsTrue(button.GetComponentInChildren<Text>().transform.IsChildOf(button.targetGraphic.transform));
                motion.OnPointerExit(null);for(int i=0;i<60;i++)motion.Tick(1/60f);
                Assert.AreEqual(1,button.targetGraphic.transform.localScale.x,.001f);
                button.interactable=false;motion.OnPointerEnter(null);for(int i=0;i<60;i++)motion.Tick(1/60f);
                Assert.AreEqual(1,button.targetGraphic.transform.localScale.x,.001f);
            }finally{Object.DestroyImmediate(parent);}
        }
        [Test]public void PauseButtonsKeepSeparateActionsAndStayOnPaintedButtonRows(){
            var parent=new GameObject("Pause",typeof(RectTransform));int resume=0,settings=0,menu=0;
            try{
                parent.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
                var viewport=ReferenceScreens.Viewport(parent.transform,"Reference");
                ReferenceScreens.Pause(viewport,()=>resume++,()=>settings++,()=>menu++);
                var buttons=parent.GetComponentsInChildren<Button>();Assert.AreEqual(3,buttons.Length);
                foreach(var button in buttons){
                    var image=button.GetComponent<Image>();Assert.IsTrue(image.raycastTarget);
                    var rect=(RectTransform)button.transform;Assert.Greater(rect.anchorMin.x,.2f);Assert.Less(rect.anchorMax.x,.8f);
                    button.onClick.Invoke();
                }
                Assert.AreEqual(1,resume);Assert.AreEqual(1,settings);Assert.AreEqual(1,menu);
            }finally{Object.DestroyImmediate(parent);if(GameMusic.Instance!=null)Object.DestroyImmediate(GameMusic.Instance.gameObject);}
        }
    }
}

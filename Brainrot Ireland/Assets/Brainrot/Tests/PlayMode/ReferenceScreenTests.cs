using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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

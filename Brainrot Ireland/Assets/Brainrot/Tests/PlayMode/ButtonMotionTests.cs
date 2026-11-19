using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Brainrot.Tests
{
    public sealed class ButtonMotionTests
    {
        GameObject root;
        Button button;
        CartoonButtonMotion motion;
        RectTransform face;

        [SetUp] public void CreateButton()
        {
            root=new GameObject("Stable hitbox",typeof(RectTransform),typeof(Image),typeof(Button));
            var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(280,70);
            root.GetComponent<Image>().color=Color.clear;
            face=CartoonUI.Image("Face and lettering",rect,Vector2.zero,Vector2.one,Color.white).rectTransform;
            button=root.GetComponent<Button>();button.targetGraphic=face.GetComponent<Image>();button.transition=Selectable.Transition.None;
            motion=root.AddComponent<CartoonButtonMotion>();motion.VisualTarget=face;
            motion.Tick(1/60f);
        }
        [TearDown] public void CleanUp(){Object.DestroyImmediate(root);}
        void Tick(int frames=60){for(int i=0;i<frames;i++)motion.Tick(1/60f);}

        [Test] public void OnlyLeftPressCompressesTheArtAndNeverMovesTheHitbox()
        {
            var bounds=((RectTransform)root.transform).rect;
            var right=new PointerEventData(null){button=PointerEventData.InputButton.Right};
            motion.OnPointerDown(right);Tick(12);
            Assert.AreEqual(Vector3.one,face.localScale);
            var left=new PointerEventData(null){button=PointerEventData.InputButton.Left};
            motion.OnPointerDown(left);Tick(8);
            Assert.Less(face.localScale.y,.98f);Assert.Less(face.anchoredPosition.y,0);
            Assert.AreEqual(Vector3.one,root.transform.localScale);
            Assert.AreEqual(bounds,((RectTransform)root.transform).rect);
            motion.OnPointerUp(left);Tick();
            Assert.AreEqual(1,face.localScale.y,.001f);Assert.AreEqual(0,face.anchoredPosition.y,.001f);
        }

        [Test] public void SettledButtonDoesNotDirtyItsTransformEveryFrame()
        {
            Tick(120);face.hasChanged=false;Tick(120);
            Assert.IsFalse(face.hasChanged,"A settled button must not force canvas transforms to rebuild every frame.");
        }

        [Test] public void KeyboardSubmitAnimatesWhileTimeIsPaused()
        {
            float previous=Time.timeScale;
            try{
                Time.timeScale=0;motion.OnSelect(null);motion.OnSubmit(null);Tick(4);
                Assert.Less(face.localScale.y,.99f);
                Tick();Assert.Greater(face.localScale.y,1.05f);
                motion.OnDeselect(null);Tick();Assert.AreEqual(1,face.localScale.y,.001f);
            }finally{Time.timeScale=previous;}
        }

        [Test] public void LeavingAndDisablingAPressedButtonCannotLeaveItStuck()
        {
            motion.OnPointerEnter(null);motion.OnPointerDown(null);Tick(8);
            motion.OnPointerExit(null);motion.OnPointerUp(null);Tick();
            Assert.AreEqual(1,face.localScale.y,.001f);
            motion.OnPointerEnter(null);motion.OnPointerDown(null);Tick(8);
            button.interactable=false;Tick();
            Assert.AreEqual(1,face.localScale.x,.001f);Assert.AreEqual(1,face.localScale.y,.001f);
            button.interactable=true;Tick();Assert.AreEqual(1,face.localScale.y,.001f);
            motion.OnPointerDown(null);Tick(8);root.SetActive(false);
            Assert.AreEqual(Vector3.one,face.localScale);Assert.AreEqual(Vector2.zero,face.anchoredPosition);
        }

        [Test] public void DecorativeGlowFollowsFocusAndDisabledState()
        {
            var glow=CartoonUI.Image("Hover glow",face,Vector2.zero,Vector2.one,Color.white);
            motion.HoverGlow=glow;motion.Tick(1/60f);Assert.AreEqual(0,glow.color.a);
            motion.OnSelect(null);Tick();Assert.Greater(glow.color.a,.9f);
            motion.OnPointerExit(null);Tick();Assert.Greater(glow.color.a,.9f,"Pointer exit must preserve keyboard focus.");
            button.interactable=false;Tick();Assert.AreEqual(0,glow.color.a);
        }
    }
}

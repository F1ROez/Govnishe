using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Brainrot.Tests
{
    public sealed class QuestHudPolishTests
    {
        [Test]public void CleanWaveIsAwardedAtClearRatherThanOnlyAtNextWave()
        {
            var progress=new RunChallengeProgress();progress.Reset(2,3,false);
            Assert.AreEqual(RunChallenge.None,progress.ObserveCompletedWave(false,3));
            Assert.AreEqual(RunChallenge.CleanWave,progress.ObserveCompletedWave(true,3));
            Assert.AreEqual(RunChallenge.None,progress.ObserveCompletedWave(true,4));
            progress.Reset(2,3,true);
            Assert.AreEqual(RunChallenge.None,progress.ObserveCompletedWave(true,3),"Restoring part of a wave must not grant a free clean-wave achievement.");
        }
        [Test]public void QuestDataClampsProgressAndRejectsMissingTargets()
        {
            Assert.AreEqual(0,new ChallengeCardData {Current=1,Target=0}.Progress);
            Assert.AreEqual(1,new ChallengeCardData {Current=120,Target=100}.Progress);
            Assert.AreEqual(0,new ChallengeCardData {Current=-2,Target=100}.Progress);
            Assert.AreEqual(.5f,new ChallengeCardData {Current=25,Target=50}.Progress);
        }
        [Test]public void QuestMeterStartsAtActualValueAndSanitizesNonFiniteInput()
        {
            var parent=new GameObject("Quest meter",typeof(RectTransform));
            try{
                var meter=QuestHUD.Meter(parent.transform,"Progress",Vector2.zero,Vector2.one,Color.cyan);
                meter.Set(.25f,true);Assert.AreEqual(.25f,meter.Value,.001f);
                Assert.AreEqual(Mathf.Lerp(.005f,.995f,.25f),meter.Fill.rectTransform.anchorMax.x,.001f);
                meter.Set(float.NaN,true);Assert.AreEqual(0,meter.Value);Assert.IsFalse(meter.Fill.gameObject.activeSelf);
                meter.Set(1,true);Assert.IsTrue(meter.Fill.gameObject.activeSelf);
            }finally{Object.DestroyImmediate(parent);}
        }
        [Test]public void CardReusesLabelsAndReflectsCompletionWithoutDuplicatingObjects()
        {
            var parent=new GameObject("Quest card",typeof(RectTransform));
            try{
                var card=parent.AddComponent<HudQuestCard>();card.Build();
                var value=new ChallengeCardData {Id=1,Title="50 ПОБЕЖДЁННЫХ",Detail="ПОБЕДИ 50 ПРОТИВНИКОВ",Current=17,Target=50,Icon="HeavyIcon"};
                card.Present(value);int childCount=parent.GetComponentsInChildren<Transform>(true).Length;
                Assert.AreEqual("17 / 50",parent.transform.Find("Золотой кант/Морская эмаль/Счётчик").GetComponent<Text>().text);
                value.Current=50;value.Complete=true;card.Present(value);
                Assert.IsTrue(card.Complete);Assert.AreEqual("ГОТОВО",parent.transform.Find("Золотой кант/Морская эмаль/Счётчик").GetComponent<Text>().text);
                for(int i=0;i<10;i++)card.Present(value);
                Assert.AreEqual(childCount,parent.GetComponentsInChildren<Transform>(true).Length);
                card.Present(default);Assert.IsFalse(parent.activeSelf);
            }finally{Object.DestroyImmediate(parent);}
        }
        [Test]public void PointerExitDoesNotEraseKeyboardSelectionAndDisabledButtonReturnsToRest()
        {
            var parent=new GameObject("Keyboard button",typeof(RectTransform));
            try{
                var button=ReferenceScreens.Hotspot(parent.transform,"НАЗАД",0,0,.4f,.1f,()=>{});
                var motion=button.GetComponent<CartoonButtonMotion>();motion.OnSelect(null);motion.OnPointerEnter(null);motion.OnPointerExit(null);
                for(int i=0;i<60;i++)motion.Tick(1/60f);
                Assert.Greater(button.targetGraphic.transform.localScale.x,1.05f);
                button.interactable=false;for(int i=0;i<60;i++)motion.Tick(1/60f);
                Assert.AreEqual(1,button.targetGraphic.transform.localScale.x,.001f);
                Assert.AreEqual(Vector3.one,button.transform.localScale);
            }finally{Object.DestroyImmediate(parent);}
        }
    }
}

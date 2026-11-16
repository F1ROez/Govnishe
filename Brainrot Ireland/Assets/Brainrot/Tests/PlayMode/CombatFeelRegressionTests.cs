using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class CombatFeelRegressionTests
    {
        [Test] public void ReleaseReturnsFovQuicklyAndDoesNotCreateImpactOnMiss()
        {
            var settings=new CombatFeelTuning();var camera=new CombatCameraFeedback();
            camera.Tick(1,.45f,settings);Assert.Less(camera.FieldOfView(62,settings,1),58);
            camera.Tick(0,.28f,settings);Assert.That(camera.FieldOfView(62,settings,1),Is.EqualTo(62).Within(.03f));
            Assert.AreEqual(0,camera.Impact);Assert.AreEqual(Vector3.zero,camera.Offset(settings,1));
        }
        [Test] public void ChargeEnvelopeIsIndependentOfFrameRate()
        {
            var settings=new CombatFeelTuning();var fast=new CombatCameraFeedback();var slow=new CombatCameraFeedback();
            for(int i=0;i<60;i++)fast.Tick(1,1f/60,settings);
            for(int i=0;i<20;i++)slow.Tick(1,1f/20,settings);
            Assert.That(fast.Charge,Is.EqualTo(slow.Charge).Within(.00001f));
        }
        [Test] public void ConfirmedImpactHasBoundedDirectionalOffsetAndCanBeDisabled()
        {
            var settings=new CombatFeelTuning();var camera=new CombatCameraFeedback();
            camera.ConfirmHit(Vector3.forward,true,settings);Assert.Less(camera.Offset(settings,1).z,0);
            // Twenty targets do not multiply intensity: replacing the same swing envelope stays bounded.
            for(int i=0;i<20;i++)camera.ConfirmHit(Vector3.forward,true,settings);
            Assert.LessOrEqual(camera.Offset(settings,1).magnitude,settings.HitShake*1.1f);
            Assert.AreEqual(Vector3.zero,camera.Offset(settings,0));Assert.AreEqual(62,camera.FieldOfView(62,settings,0));
            camera.Tick(0,1,settings);Assert.AreEqual(0,camera.Impact);Assert.AreEqual(Vector3.zero,camera.Offset(settings,1));
        }
        [Test] public void CancellationClearsChargeAndImpactWithoutChangingGlobalTime()
        {
            float original=Time.timeScale;var settings=new CombatFeelTuning();var camera=new CombatCameraFeedback();
            camera.Tick(1,.5f,settings);camera.ConfirmHit(Vector3.right,true,settings);camera.Reset();
            Assert.AreEqual(0,camera.Charge);Assert.AreEqual(0,camera.Impact);Assert.AreEqual(62,camera.FieldOfView(62,settings,1));
            Assert.AreEqual(original,Time.timeScale);
        }
    }
    public sealed class CombatImpactContactRegressionTests
    {
        GameManager game;GameBalance profile;PlayerCharacterDefinition character;Scene scene,previous;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.IsNull(GameManager.Instance,"Запускайте проверки из режима редактирования.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Ощущение удара "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;profile.Normal.Health=500;profile.Normal.Damage=0;game.Balance=profile;
            character=ScriptableObject.CreateInstance<PlayerCharacterDefinition>();game.Player.Character=character;yield return null;game.StartRun();game.Player.Stats.CritChance=0;
            game.enabled=false;game.Player.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            MobileInput.Clear();if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);if(character!=null)Object.Destroy(character);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        [UnityTest] public IEnumerator MissCreatesNoContactRecoilAndCrowdContactStaysBounded()
        {
            var combat=game.Player.Combat;
            Assert.IsTrue(combat.ExecuteAttack(true));yield return new WaitForSeconds(.14f);
            Assert.AreEqual(0,game.Player.CombatCameraImpact,"Взмах мимо не должен давать подтверждение попадания камерой.");
            combat.ResetRun();
            for(int i=0;i<3;i++)game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*2+Vector3.right*(i-1)*.25f);
            Assert.IsTrue(combat.ExecuteAttack(true));yield return new WaitForSeconds(.14f);
            Assert.That(game.Player.CombatCameraImpact,Is.GreaterThan(0).And.LessThanOrEqualTo(1),"Несколько целей дают одну отдачу, без умножения амплитуды.");
            Assert.AreEqual(1,Time.timeScale,"Контактная задержка позы не должна останавливать игровое время.");
            game.Pause();Assert.AreEqual(0,game.Player.CombatCameraImpact,"Пауза немедленно сбрасывает отдачу камеры.");
            Assert.IsFalse(combat.IsAttacking);Assert.IsFalse(combat.IsCharging);
        }
    }
}

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
        [Test] public void HeroCameraMigrationUpgradesOnlyOldFactoryValuesOnce()
        {
            var profile=ScriptableObject.CreateInstance<GameBalance>();
            try{
                JsonUtility.FromJsonOverwrite("{\"heroFeelVersion\":0}",profile);
                profile.Camera.ChargeZoomDistance=.65f;profile.Feedback.ChargeFovReduction=5;
                profile.Feedback.HitShake=.07f; // Explicit owner's tuning must survive.
                Assert.IsTrue(profile.UpgradeHeroFeel());Assert.AreEqual(1.65f,profile.Camera.ChargeZoomDistance);
                Assert.AreEqual(17,profile.Feedback.ChargeFovReduction);Assert.AreEqual(.07f,profile.Feedback.HitShake);
                profile.Feedback.ChargeFovReduction=5;Assert.IsFalse(profile.UpgradeHeroFeel());Assert.AreEqual(5,profile.Feedback.ChargeFovReduction);
            }finally{Object.DestroyImmediate(profile);}
        }
        [Test] public void HeavyCloseupIsDramaticButBoundedAndReturnsWithoutImpactOnMiss()
        {
            var tuning=new CombatFeelTuning();var camera=new CombatCameraFeedback();camera.Tick(1,1,tuning);
            Assert.That(camera.FieldOfView(62,tuning,1),Is.InRange(35f,47f));
            camera.Tick(0,.2f,tuning);Assert.That(camera.FieldOfView(62,tuning,1),Is.EqualTo(62).Within(.03f));
            Assert.AreEqual(0,camera.Impact);
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
        [UnityTest] public IEnumerator EffectPoolStaysBoundedAndClearsHeavyEchoBeforeReuse()
        {
            HitEffects.ClearAll();profile.Feedback.EffectsBudget=4;
            int ceiling=Mathf.Max(4,HitEffects.AllocatedCount);
            for(int i=0;i<32;i++){
                HitEffects.HeavyImpact(Vector3.zero,Vector3.right);
                BatSwingArc.Play(Vector3.up,Vector3.right,3,true);
            }
            Assert.LessOrEqual(HitEffects.AllocatedCount,ceiling);
            yield return new WaitForSeconds(.1f);
            HitEffects.ClearAll();
            Assert.AreEqual(0,Object.FindObjectsByType<HitEffects>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0,Object.FindObjectsByType<BatSwingArc>(FindObjectsSortMode.None).Length);
            HitEffects.Burst(Vector3.zero,Color.white,4,.45f);
            var reused=Object.FindObjectsByType<HitEffects>(FindObjectsSortMode.None);
            Assert.AreEqual(1,reused.Length);
            foreach(var line in reused[0].GetComponentsInChildren<LineRenderer>()){
                if(line.name=="Эхо сильного удара")Assert.IsFalse(line.enabled,"Обычный удар не наследует эхо сильного.");
                Assert.AreEqual(3000,line.sharedMaterial.renderQueue);
            }
            HitEffects.ClearAll();
        }
        [UnityTest] public IEnumerator HoldingDuringCooldownCannotChargeOrFallBackToOrdinarySwing()
        {
            var combat=game.Player.Combat;combat.RestoreHeavyCooldown(.35f);combat.UpdateAttackInput(false);
            yield return new WaitForSeconds(.21f);combat.UpdateAttackInput(true);
            yield return new WaitForSeconds(.19f);combat.UpdateAttackInput(true);
            Assert.IsFalse(combat.IsCharging);Assert.AreEqual(0,combat.ChargeProgress);Assert.IsTrue(combat.HeavyCooldownNotice);
            combat.UpdateAttackInput(false);Assert.IsFalse(combat.IsAttacking);
            combat.RestoreHeavyCooldown(10);combat.UpdateAttackInput(false);
            yield return new WaitForSeconds(.21f);combat.UpdateAttackInput(true);combat.UpdateAttackInput(false);
            Assert.IsTrue(combat.IsAttacking,"Короткий клик остаётся обычным ударом во время перезарядки.");
        }
        [UnityTest] public IEnumerator ShortClickBuffersOnceAndCancellationDropsIt()
        {
            var combat=game.Player.Combat;combat.UpdateAttackInput(false);
            Assert.IsTrue(combat.ExecuteAttack(false));
            yield return new WaitForSeconds(.52f);
            // Press and release between frames: edge events retain this tap near cooldown completion.
            combat.UpdateAttackInput(false,true,true);
            yield return new WaitForSeconds(.14f);
            combat.UpdateAttackInput(false);Assert.IsTrue(combat.IsAttacking);
            yield return new WaitForSeconds(.52f);
            combat.UpdateAttackInput(false,true,true);combat.CancelPendingSwings();
            yield return new WaitForSeconds(.14f);
            combat.UpdateAttackInput(false);Assert.IsFalse(combat.IsAttacking);
            combat.UpdateAttackInput(false);Assert.IsFalse(combat.IsAttacking);
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

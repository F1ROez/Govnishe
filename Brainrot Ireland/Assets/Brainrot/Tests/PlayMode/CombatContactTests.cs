using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class CombatContactTests
    {
        GameManager game;
        GameBalance profile;
        PlayerCharacterDefinition character;
        Scene scene,previous;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Запускайте проверки из Edit mode.");previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Контакт биты "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;profile.Normal.Health=200;profile.Normal.Damage=0;game.Balance=profile;
            character=ScriptableObject.CreateInstance<PlayerCharacterDefinition>();game.Player.Character=character;yield return null;game.StartRun();game.Player.Stats.CritChance=0;
            // Drive combat explicitly, without automatic player input or enemy movement changing the contact geometry.
            game.enabled=false;game.Player.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);if(character!=null)Object.Destroy(character);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        [UnityTest]public IEnumerator BatHitsOrcaladonBodyBeforeHisCentreEntersDisplayedRange(){
            var player=game.Player;var position=player.transform.position+player.transform.forward*(player.Stats.Range+.5f);
            var enemy=game.Spawner.Spawn(EnemyKind.Orcaladon,position);Assert.IsNotNull(enemy);float health=enemy.Health;
            Assert.IsFalse(CombatSystem.InMeleeSector(player.transform.position,player.transform.forward,enemy.transform.position,player.Stats.Range,profile.Combat.ArcDegrees,profile.Combat.InnerOmniRadius));
            Assert.IsTrue(player.Combat.ExecuteAttack(false));yield return new WaitForSeconds(.15f);
            Assert.That(enemy.Health,Is.EqualTo(health-player.Stats.Damage).Within(.01f),"Попадание по большому видимому телу не должно требовать его центра внутри сектора.");
            Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
        }
        [UnityTest]public IEnumerator DirectAttackConsumesAnExistingChargeAndClearsTheWindup(){
            var combat=game.Player.Combat;combat.UpdateAttackInput(false);combat.UpdateAttackInput(true);yield return new WaitForSeconds(profile.Combat.HeavyHoldDelay+profile.Combat.HeavyChargeSeconds+.02f);combat.UpdateAttackInput(true);
            Assert.IsTrue(combat.IsCharging);Assert.AreEqual(1,combat.ChargeProgress);Assert.IsTrue(combat.ExecuteAttack(true));
            Assert.IsFalse(combat.IsCharging);Assert.AreEqual(0,combat.ChargeProgress);Assert.Greater(combat.HeavyCooldownRemaining,9);
            var bridge=game.Player.GetComponentInChildren<ImportedSahurVisual>();if(bridge!=null){Assert.IsFalse(bridge.ChargePoseActive);Assert.IsTrue(bridge.HeavyAttackActive);}
            yield return null;
        }
        [UnityTest]public IEnumerator DisablingCombatCancelsDelayedContactsInsteadOfApplyingThemAfterReenable(){
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*2);float health=enemy.Health;
            var combat=game.Player.Combat;Assert.IsTrue(combat.ExecuteAttack(false));combat.enabled=false;yield return new WaitForSeconds(.2f);Assert.IsFalse(combat.ExecuteAttack(false));combat.UpdateAttackInput(true);Assert.IsFalse(combat.IsCharging);combat.enabled=true;yield return null;
            Assert.AreEqual(health,enemy.Health);Assert.IsFalse(combat.IsAttacking);Assert.IsFalse(combat.IsCharging);
        }
    }
    public sealed class MeleeBodyIntersectionTests
    {
        static bool Hit(Vector3 target,float bodyRadius,float inner=.8f)=>CombatSystem.InMeleeSector(Vector3.zero,Vector3.forward,target,3,90,inner,bodyRadius);
        [Test]public void LargeBodyCanTouchOuterArcWhileCentreIsOutside(){
            Assert.IsFalse(Hit(Vector3.forward*3.6f,0));Assert.IsTrue(Hit(Vector3.forward*3.6f,.7f));Assert.IsFalse(Hit(Vector3.forward*3.8f,.7f));
        }
        [Test]public void BodyCrossingStraightSectorEdgeIsHitWithoutBroadeningTheWholeCone(){
            Vector3 target=Quaternion.Euler(0,60,0)*Vector3.forward*2.5f;
            Assert.IsFalse(Hit(target,0));Assert.IsFalse(Hit(target,.5f));Assert.IsTrue(Hit(target,.7f));
        }
        [Test]public void BodyBehindPlayerMustActuallyCrossTheNearCircle(){
            Assert.IsFalse(Hit(Vector3.back*1.6f,.5f));Assert.IsTrue(Hit(Vector3.back*1.6f,.9f));
        }
        [Test]public void RangeOutlineClampsNearCircleAndFiniteEdges(){
            Assert.IsFalse(Hit(Vector3.back*4,0,100));
            Vector3 beyondCorner=Quaternion.Euler(0,55,0)*Vector3.forward*3.8f;
            Assert.IsFalse(Hit(beyondCorner,.7f));Assert.IsTrue(Hit(beyondCorner,1.01f));
        }
    }
}

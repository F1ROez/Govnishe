using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BossSharkPatternTests
    {
        GameManager game;GameBalance balance;EnemyController enemy;BossEncounter encounter;BossSharkPattern pattern;
        Scene previous,scene;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);previous=SceneManager.GetActiveScene();
            scene=SceneManager.CreateScene("Shark boss pattern "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Game").AddComponent<GameManager>();balance=ScriptableObject.CreateInstance<GameBalance>();
            balance.Waves.InitialDelay=600;game.Balance=balance;yield return null;
            game.StartRun();game.enabled=false;game.Player.enabled=false;
            game.Player.Stats.MaxHealth=10000;game.Player.Heal(10000);
            enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.zero);enemy.ConfigureBoss(IslandBossKind.Shark,5);
            encounter=enemy.BossEncounter;pattern=new BossSharkPattern(encounter);
            encounter.Position=Vector3.zero;game.Player.transform.position=Vector3.forward*12;Physics.SyncTransforms();
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(balance!=null)Object.Destroy(balance);yield return null;
        }
        void Until(SharkBossAction action,int limit=1200){
            for(int i=0;i<limit&&pattern.Action!=action;i++)pattern.Tick(.05f);
            Assert.AreEqual(action,pattern.Action);
        }
        [Test]public void ChargeWarningLocksBeforeLaunchAndNewChargeNeverDamagesImmediately(){
            float health=game.Player.Health;pattern.Tick(.8f);
            Assert.AreEqual(SharkBossAction.ChargeWindup,pattern.Action);Assert.AreEqual(health,game.Player.Health);
            game.Player.transform.position=encounter.Position+Vector3.left*10;pattern.Tick(.2f);
            Vector3 committed=pattern.CommittedDirection;
            game.Player.transform.position=encounter.Position+Vector3.right*10;pattern.Tick(.7f);
            Assert.IsTrue(pattern.DirectionLocked);Assert.Less(Vector3.Angle(committed,pattern.CommittedDirection),.01f);
            pattern.Tick(.5f);Assert.AreEqual(SharkBossAction.Charging,pattern.Action);
            Assert.AreEqual(health,game.Player.Health,"Warning-to-launch transition must not consume the same giant frame as invisible movement.");
            Assert.Less(Vector3.Angle(committed,pattern.CommittedDirection),.01f);
        }
        [Test]public void CollidingRushCreatesGroundedStunWindowWithoutPhysicsTumble(){
            pattern.Tick(.8f);pattern.Tick(1.2f);Assert.AreEqual(SharkBossAction.Charging,pattern.Action);
            var rock=new GameObject("Test charge rock",typeof(BoxCollider));
            rock.transform.position=encounter.Position+pattern.CommittedDirection*3+Vector3.up*1.5f;
            rock.GetComponent<BoxCollider>().size=new Vector3(1,3,1);Physics.SyncTransforms();
            pattern.Tick(.3f);
            Assert.AreEqual(SharkBossAction.Stunned,pattern.Action);
            Assert.GreaterOrEqual(pattern.StateDuration,1);Assert.AreEqual(0,encounter.Position.y,.001f);
            Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
            pattern.Tick(pattern.StateDuration+.1f);Assert.AreEqual(SharkBossAction.Approach,pattern.Action);
        }
        [Test]public void SecondPhaseAddsTelegraphedSpinAndDoubleRush(){
            enemy.TakeDamage(enemy.MaxHealth*.6f,Vector3.zero);encounter.Tick(.02f);
            Assert.IsTrue(encounter.PhaseTwo);pattern.Reset();
            Until(SharkBossAction.ChargeWindup);pattern.Tick(pattern.StateDuration+.01f);
            // Keep the rush in open ground, avoiding the coast and deliberate obstruction stun.
            encounter.Position=Vector3.zero;game.Player.transform.position=Vector3.forward*12;
            pattern.Tick(balance.IslandBosses.Shark.ChargeSeconds+.01f);
            if(pattern.Action!=SharkBossAction.Stunned)
                Assert.AreEqual(SharkBossAction.ChargeWindup,pattern.Action,"The opening phase-II rush is a warned double sequence.");
            Until(SharkBossAction.SpinWindup);
            Assert.GreaterOrEqual(pattern.StateDuration,.8f);
            pattern.Tick(pattern.StateDuration+.01f);Assert.AreEqual(SharkBossAction.Spinning,pattern.Action);
            pattern.Tick(balance.IslandBosses.Shark.SpinSeconds+.01f);
            Assert.AreEqual(SharkBossAction.Recovery,pattern.Action);Assert.GreaterOrEqual(pattern.StateDuration,1.8f);
        }
        [Test]public void TripleBiteHasAlternatingCommittedArcsAndLongFinalWarning(){
            Until(SharkBossAction.BiteWindup);
            Assert.AreEqual(0,pattern.BiteNumber);float quick=pattern.StateDuration;Vector3 first=pattern.CommittedDirection;
            pattern.Tick(quick+.01f);Assert.AreEqual(SharkBossAction.Biting,pattern.Action);
            pattern.Tick(.29f);Assert.AreEqual(SharkBossAction.BiteWindup,pattern.Action);Assert.AreEqual(1,pattern.BiteNumber);
            Assert.AreEqual(44,Vector3.Angle(first,pattern.CommittedDirection),.5f,"The first two bites must commit to opposite sides.");
            pattern.Tick(pattern.StateDuration+.01f);pattern.Tick(.29f);
            Assert.AreEqual(SharkBossAction.BiteWindup,pattern.Action);Assert.AreEqual(2,pattern.BiteNumber);
            Assert.Greater(pattern.StateDuration,quick+.1f,"The wide third bite needs its own conspicuous preparation.");
            pattern.Tick(pattern.StateDuration+.01f);pattern.Tick(.29f);
            Assert.AreEqual(SharkBossAction.Recovery,pattern.Action);Assert.GreaterOrEqual(pattern.StateDuration,1);
            pattern.Reset();Assert.AreEqual(SharkBossAction.Approach,pattern.Action);Assert.AreEqual(0,pattern.BiteNumber);
        }
    }
}

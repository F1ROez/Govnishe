using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BossEncounterLifecycleTests
    {
        Scene previous,scene;GameManager game;GameBalance balance;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Boss lifetime "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Game").AddComponent<GameManager>();balance=ScriptableObject.CreateInstance<GameBalance>();game.Balance=balance;balance.Waves.InitialDelay=600;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;game.Player.Stats.MaxHealth=10000;game.Player.Heal(10000);
            game.Spawner.ResetRun();game.Spawner.Waves.RestoreRunState(new WaveRunSnapshot{Number=10});game.Spawner.Tick(.01f);
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(balance!=null)Object.Destroy(balance);yield return null;
        }
        BossEncounter Boss=>game.Spawner.CurrentBoss.BossEncounter;
        void NearBoss(){game.Player.transform.position=Boss.Position+Vector3.forward*(Boss.Radius+.6f);Physics.SyncTransforms();}
        [Test]public void DashAvoidsBossDamageAndCaptureAndEscapeProtectsAgainstImmediateRecapture(){
            NearBoss();Assert.IsTrue(game.Player.TryDash(Vector3.forward));float health=game.Player.Health;
            Boss.CircleHit(game.Player.transform.position,2);Assert.AreEqual(health,game.Player.Health);
            Assert.IsFalse(Boss.TryCapture());game.Player.EndDash();Assert.IsTrue(Boss.TryCapture());
            for(int i=0;i<Mathf.CeilToInt(balance.Octopus.EscapeClicks)+1;i++)game.Player.EscapeTap();
            Assert.IsFalse(game.Player.IsCaptured);Assert.IsTrue(Boss.CaptureEscaped);Assert.IsFalse(Boss.TryCapture());Assert.Less(game.Player.transform.position.y,.3f);
        }
        [Test]public void PauseCancelsHazardsAndCaptureSynchronouslyEvenWithSameFrameResume(){
            NearBoss();var boss=Boss;Assert.IsTrue(boss.TryCapture());boss.Tentacle(Vector3.zero,2,1);boss.Ink(Vector3.zero,2,5);Assert.AreEqual(2,boss.ActiveHazards);
            float before=game.Player.Health;game.Pause();Assert.IsFalse(boss.Capturing);Assert.IsFalse(game.Player.IsCaptured);Assert.AreEqual(0,boss.ActiveHazards);
            game.Resume();boss.Tick(.01f);Assert.AreEqual(before,game.Player.Health);Assert.AreEqual(0,boss.ActiveHazards);
        }
        [Test]public void PuddlesSlowOnlyWhileAliveAndCannotChangePermanentStats(){
            var boss=Boss;var arena=boss.GetComponent<BossArenaEffects>();float speed=game.Player.Stats.MoveSpeed;
            boss.Ink(game.Player.transform.position,2,1,.55f);arena.Tick(.4f);Assert.AreEqual(.55f,game.Player.BossMovementMultiplier,.001f);
            arena.Tick(2);Assert.AreEqual(1,game.Player.BossMovementMultiplier);Assert.AreEqual(speed,game.Player.Stats.MoveSpeed);
            for(int i=0;i<50;i++)boss.Tentacle(Vector3.zero,1,2);Assert.AreEqual(16,boss.ActiveHazards);
            game.StartRun();Assert.IsFalse(boss.IsActive);Assert.AreEqual(0,boss.ActiveHazards);Assert.AreEqual(1,game.Wave);
        }
        [Test]public void VisibleCircleAndConeUseExactlyTheirDeclaredBoundary(){
            Assert.IsTrue(BossEncounter.InCircle(new Vector3(3,5,0),Vector3.zero,3));Assert.IsFalse(BossEncounter.InCircle(new Vector3(3.01f,0,0),Vector3.zero,3));
            Assert.IsTrue(BossEncounter.InCone(Vector3.forward*3,Vector3.zero,Vector3.forward,3,90));
            Assert.IsFalse(BossEncounter.InCone(Vector3.right*2,Vector3.zero,Vector3.forward,3,90));
        }
    }
}

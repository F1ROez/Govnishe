using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BossOctopusPatternTests
    {
        Scene previous,scene;
        GameManager game;
        GameBalance balance;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Boss octopus "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Game").AddComponent<GameManager>();balance=ScriptableObject.CreateInstance<GameBalance>();
            balance.Waves.InitialDelay=600;game.Balance=balance;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
            game.Player.transform.position=Vector3.zero;game.Player.Stats.MaxHealth=10000;game.Player.Heal(10000);
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(balance!=null)Object.Destroy(balance);yield return null;
        }
        BossOctopusPattern Pattern(){
            var enemy=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,Vector3.back*8);Assert.IsNotNull(enemy);
            enemy.ConfigureBoss(IslandBossKind.Octopus,10);return new BossOctopusPattern(enemy.BossEncounter);
        }
        [Test]public void MeteorCommitsLandingAndDoesNotHitDuringVisiblePreparation(){
            var pattern=Pattern();float before=game.Player.Health;
            pattern.Tick(1.2f);Assert.AreEqual(BossOctopusPhase.Jump,pattern.Phase);
            var landing=pattern.CommittedLanding;
            game.Player.transform.position=Vector3.right*9;pattern.Tick(.35f);
            Assert.AreEqual(landing,pattern.CommittedLanding,"Moving after the marker appears must not retarget a meteor.");
            Assert.AreEqual(before,game.Player.Health,"The special marker is preparation, not contact damage.");
            pattern.Tick(2);Assert.AreEqual(BossOctopusPhase.Recovery,pattern.Phase);
            Assert.AreEqual(before,game.Player.Health,"Leaving the locked area avoids the meteor impact.");
        }
        [Test]public void OversizedTickStillSchedulesThreeTentacleWarningsSequentially(){
            var pattern=Pattern();
            pattern.Tick(2);pattern.Tick(2);pattern.Tick(10);pattern.Tick(10);
            Assert.AreEqual(BossOctopusPhase.Tentacles,pattern.Phase);Assert.AreEqual(1,pattern.TentaclesScheduled);
            pattern.Tick(100);Assert.AreEqual(2,pattern.TentaclesScheduled,"A hitch must not create all warned impacts in one frame.");
            pattern.Tick(100);Assert.AreEqual(3,pattern.TentaclesScheduled);
            pattern.Reset();Assert.AreEqual(BossOctopusPhase.Approach,pattern.Phase);Assert.AreEqual(0,pattern.TentaclesScheduled);
        }
        [Test]public void InkZonesRemainSeparatedAndInsideTheIslandAtTheShore(){
            var points=new Vector3[3];const float radius=1.65f;
            for(int i=0;i<16;i++){
                float angle=i*Mathf.PI/8;var player=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*21;
                BossOctopusPattern.FillInkTargets(player,Vector3.zero,radius,points);
                for(int a=0;a<3;a++){
                    Assert.IsTrue(IslandMap.IsWalkable(points[a],radius+1));
                    for(int b=0;b<a;b++)Assert.Greater(Vector3.Distance(points[a],points[b]),2*radius+.4f,"Slow zones must leave a way out.");
                }
            }
        }
    }
}

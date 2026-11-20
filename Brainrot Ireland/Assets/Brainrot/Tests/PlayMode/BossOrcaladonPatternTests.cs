using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BossOrcaladonPatternTests
    {
        Scene previous,scene;GameManager game;GameBalance profile;EnemyController enemy;BossEncounter encounter;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Бой Оркаладона "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Проверка босса").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();
            profile.Waves.InitialDelay=600;game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
            game.Player.transform.position=Vector3.zero;
            enemy=game.Spawner.Spawn(EnemyKind.Orcaladon,Vector3.back*6);Assert.IsNotNull(enemy);
            enemy.ConfigureBoss(IslandBossKind.Orcaladon,15);encounter=enemy.BossEncounter;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);yield return null;
        }
        [Test]public void FullFistComboIsOnlySelectedInSecondPhase(){
            for(int i=0;i<30;i++)Assert.AreNotEqual(3,BossOrcaladonPattern.AttackInSequence(i,false));
            int punches=0,charges=0,quakes=0;
            for(int i=0;i<6;i++){
                int attack=BossOrcaladonPattern.AttackInSequence(i,true);
                if(attack==3)punches++;if(attack==2)charges++;if(attack==0)quakes++;
            }
            Assert.AreEqual(1,punches);Assert.AreEqual(2,charges);Assert.AreEqual(2,quakes);
        }
        [Test]public void QuakeHasSeparateWarningStrikeAndThreeExpandingWaves(){
            var pattern=new BossOrcaladonPattern(encounter);
            pattern.Tick(50);Assert.AreEqual(BossOrcaStage.QuakeWindup,pattern.Stage);
            Assert.AreEqual(0,encounter.ActiveHazards);Assert.AreEqual(100,game.Player.Health);
            pattern.Tick(50);Assert.AreEqual(BossOrcaStage.QuakeStrike,pattern.Stage);
            Assert.AreEqual(0,encounter.ActiveHazards,"The downstroke is still warned before any damage starts.");
            pattern.Tick(.21f);Assert.AreEqual(BossOrcaStage.QuakeFinish,pattern.Stage);
            Assert.AreEqual(3,encounter.ActiveHazards);Assert.AreEqual(100,game.Player.Health,"A shockwave is an expanding annulus, not instant full-disk damage.");
            encounter.CancelAttack();Assert.AreEqual(0,encounter.ActiveHazards);
            pattern.Reset();Assert.AreEqual(BossOrcaStage.Approach,pattern.Stage);Assert.AreEqual(0,pattern.CompletedAttacks);
        }
        [Test]public void CorrectiveChargeStopsAndShowsANewLaneBeforeTurning(){
            enemy.TakeDamage(enemy.MaxHealth*.65f,Vector3.zero);encounter.Tick(.01f);Assert.IsTrue(encounter.PhaseTwo);
            var pattern=new BossOrcaladonPattern(encounter);
            for(int i=0;i<20&&pattern.Stage==BossOrcaStage.Approach;i++)pattern.Tick(.5f);
            Assert.AreEqual(BossOrcaStage.ChargeWindup,pattern.Stage);
            pattern.Tick(.6f);var fixedDirection=pattern.CommittedDirection;
            game.Player.transform.position=Vector3.right*8;pattern.Tick(.3f);
            Assert.That(Vector3.Distance(fixedDirection,pattern.CommittedDirection),Is.LessThan(.001f));
            pattern.Tick(1);Assert.AreEqual(BossOrcaStage.ChargeRun,pattern.Stage);
            pattern.Tick(.65f);Assert.AreEqual(BossOrcaStage.ChargeBrake,pattern.Stage);
            pattern.Tick(.31f);Assert.AreEqual(BossOrcaStage.ChargeRedirect,pattern.Stage);
            var stopped=encounter.Position;pattern.Tick(.3f);
            Assert.AreEqual(BossOrcaStage.ChargeRedirect,pattern.Stage);Assert.That(Vector3.Distance(stopped,encounter.Position),Is.LessThan(.001f));
            Assert.AreEqual(100,game.Player.Health,"The new direction is a safe warning, not a homing contact hit.");
        }
    }
}

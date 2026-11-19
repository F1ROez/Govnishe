using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class HeartDropTests
    {
        GameManager game;GameBalance balance;Scene previous,scene;
        System.IDisposable storage;string folder;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);
            folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HeartTests-"+System.Guid.NewGuid());
            storage=SaveSystem.UseStorageDirectory(folder);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Сердечки "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();balance=ScriptableObject.CreateInstance<GameBalance>();balance.Waves.InitialDelay=600;game.Balance=balance;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);
            Object.Destroy(balance);storage?.Dispose();if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        [UnityTest]public IEnumerator OnlyKilledOctopusDropsOnceAndRetreatDoesNotDrop(){
            var shark=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*5,1);
            shark.TakeDamage(100000,Vector3.zero);Assert.AreEqual(0,game.Hearts.ActiveCount);
            var octopus=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,game.Player.transform.position+Vector3.right*5,1);
            octopus.TakeDamage(100000,Vector3.zero);octopus.TakeDamage(100000,Vector3.zero);Assert.AreEqual(1,game.Hearts.ActiveCount);
            game.Hearts.Clear();var retreat=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,game.Player.transform.position+Vector3.right*6,1);
            var attack=retreat.GetComponent<OctopusAttack>();attack.Interrupt();attack.Tick(60);Assert.IsFalse(game.Spawner.Active.Contains(retreat));Assert.AreEqual(0,game.Hearts.ActiveCount);yield return null;
        }
        [Test]public void HealsTenCapsAtMaximumAndDoesNotConsumeAtFullHealth(){
            var position=game.Player.transform.position;game.Hearts.Restore(new[]{new HeartDropSnapshot{Position=position,Remaining=30}});
            game.Hearts.Tick(.1f,game.Player);Assert.AreEqual(1,game.Hearts.ActiveCount);
            var state=game.Player.CaptureRunState();state.Health=50;game.Player.RestoreRunState(state);
            game.Hearts.Tick(.1f,game.Player);Assert.AreEqual(60,game.Player.Health);Assert.AreEqual(0,game.Hearts.ActiveCount);
            game.Hearts.Tick(.1f,game.Player);Assert.AreEqual(60,game.Player.Health,"Награда подбирается один раз.");
            state.Health=95;game.Player.RestoreRunState(state);game.Hearts.Restore(new[]{new HeartDropSnapshot{Position=game.Player.transform.position,Remaining=20}});
            game.Hearts.Tick(.1f,game.Player);Assert.AreEqual(100,game.Player.Health);
        }
        [Test]public void SimultaneousHeartsDoNotConsumeTheSecondAfterTheFirstFillsHealth(){
            var state=game.Player.CaptureRunState();state.Health=game.Player.Stats.MaxHealth-5;game.Player.RestoreRunState(state);
            var position=game.Player.transform.position;
            game.Hearts.Restore(new[]{new HeartDropSnapshot{Position=position,Remaining=30},new HeartDropSnapshot{Position=position,Remaining=30}});
            game.Hearts.Tick(.1f,game.Player);
            Assert.AreEqual(game.Player.Stats.MaxHealth,game.Player.Health);
            Assert.AreEqual(1,game.Hearts.ActiveCount,"Only the heart that restored health should be consumed.");
            game.Hearts.Tick(.1f,game.Player);Assert.AreEqual(1,game.Hearts.ActiveCount,"The remaining heart must wait while health is full.");
        }
        [Test]public void PoolIsBoundedAndCheckpointRestoresRemainingLifetime(){
            for(int i=0;i<100;i++)game.Hearts.Drop(EnemyKind.BlueberrinniOctopusini,Vector3.zero);
            Assert.AreEqual(HeartDrops.Capacity,game.Hearts.ActiveCount);
            var saved=game.Hearts.Capture();game.Hearts.Clear();Assert.AreEqual(0,game.Hearts.ActiveCount);
            game.Hearts.Restore(saved);Assert.AreEqual(saved.Length,game.Hearts.ActiveCount);
            game.Hearts.Tick(120,game.Player);Assert.AreEqual(0,game.Hearts.ActiveCount);
            var snapshot=new RunSnapshot{Player=game.Player.CaptureRunState(),Wave=new WaveRunSnapshot(),Spawner=new SpawnerRunSnapshot(),Hearts=saved};
            Assert.IsTrue(RunSaveStore.IsValid(snapshot));saved[0].Remaining=float.NaN;Assert.IsFalse(RunSaveStore.IsValid(snapshot));
        }
    }
}

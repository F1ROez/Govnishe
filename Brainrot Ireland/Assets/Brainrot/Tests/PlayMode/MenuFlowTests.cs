using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class MenuFlowTests {
  Scene before;System.IDisposable storage;string folder;
  [UnitySetUp]public IEnumerator Setup(){
   if(!Application.CanStreamedLevelBeLoaded(SceneFlow.MenuPath)||!Application.CanStreamedLevelBeLoaded(SceneFlow.GamePath))Assert.Ignore("Сначала выполните Создать или обновить игру.");
   Assert.IsNull(GameManager.Instance,"Запускайте из Edit mode.");folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"MenuTests-"+System.Guid.NewGuid());storage=SaveSystem.UseStorageDirectory(folder);before=SceneManager.GetActiveScene();yield return null;
  }
  [UnityTearDown]public IEnumerator Cleanup(){
   if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;if(before.IsValid()&&before.isLoaded)SceneManager.SetActiveScene(before);
   foreach(string path in new[]{SceneFlow.MenuPath,SceneFlow.GamePath}){var s=SceneManager.GetSceneByPath(path);if(s.IsValid()&&s.isLoaded)yield return SceneManager.UnloadSceneAsync(s);}
   var flow=Object.FindFirstObjectByType<SceneFlow>();if(flow!=null)Object.Destroy(flow.gameObject);storage?.Dispose();if(folder!=null&&System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
  }
  [UnityTest]public IEnumerator MenuEndsAttemptAndPlayAlwaysStartsAtFirstWave(){
   yield return SceneManager.LoadSceneAsync(SceneFlow.MenuPath,LoadSceneMode.Additive);
   var flow=SceneFlow.Instance;flow.Play();while(flow.Busy)yield return null;
   var game=GameManager.Instance;Assert.IsTrue(game.HasRun);string id=game.CoinRunId;
   Assert.IsTrue(MetaProgression.Award(id,900,5));
   var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*8);enemy.Tumble(Vector3.right*5);
   flow.ShowMenu();while(flow.Busy)yield return null;
   Assert.IsTrue(game==null);Assert.IsTrue(enemy==null);Assert.AreEqual(0,Object.FindObjectsByType<HeroMotionEffects>(FindObjectsSortMode.None).Length);Assert.IsNull(GameManager.Instance);Assert.AreEqual(5,SaveSystem.Data.Coins);
   Assert.AreEqual(GameMusic.PlaybackMode.Menu,GameMusic.Instance.Mode);
   flow.Play(false);while(flow.Busy)yield return null; // Even the old API's false argument cannot resume.
   var next=GameManager.Instance;Assert.IsTrue(next.IsPlaying);Assert.AreNotEqual(id,next.CoinRunId);
   Assert.AreEqual(1,next.Wave);Assert.AreEqual(0,next.Kills);Assert.AreEqual(next.Player.Stats.MaxHealth,next.Player.Health);
   Assert.AreEqual(5,SaveSystem.Data.Coins);Assert.IsFalse(SceneManager.GetSceneByPath(SceneFlow.MenuPath).isLoaded);
  }
 }
}

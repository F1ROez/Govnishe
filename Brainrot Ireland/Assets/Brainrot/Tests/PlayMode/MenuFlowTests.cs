using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class MenuFlowTests {
  Scene before;
  [UnitySetUp]public IEnumerator Setup(){
   if(!Application.CanStreamedLevelBeLoaded(SceneFlow.MenuPath)||!Application.CanStreamedLevelBeLoaded(SceneFlow.GamePath))Assert.Ignore("Сначала выполните Создать или обновить игру.");
   Assert.IsNull(GameManager.Instance,"Запускайте из Edit mode.");before=SceneManager.GetActiveScene();yield return null;
  }
  [UnityTearDown]public IEnumerator Cleanup(){
   Time.timeScale=1;if(before.IsValid()&&before.isLoaded)SceneManager.SetActiveScene(before);
   foreach(string path in new[]{SceneFlow.MenuPath,SceneFlow.GamePath}){var s=SceneManager.GetSceneByPath(path);if(s.IsValid()&&s.isLoaded)yield return SceneManager.UnloadSceneAsync(s);}
   var flow=Object.FindFirstObjectByType<SceneFlow>();if(flow!=null)Object.Destroy(flow.gameObject);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
  }
  [UnityTest]public IEnumerator SeparateMenuContinuePreservesLiveAndPhysicalActors(){
   yield return SceneManager.LoadSceneAsync(SceneFlow.MenuPath,LoadSceneMode.Additive);
   var flow=SceneFlow.Instance;flow.Play(true);while(flow.Busy)yield return null;
   var game=GameManager.Instance;Assert.IsTrue(game.HasRun);Assert.IsTrue(game.IsPlaying);
   Assert.AreEqual(SceneFlow.GamePath,game.Player.gameObject.scene.path);
   var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*8);enemy.Tumble(Vector3.right*5);
   flow.ShowMenu();while(flow.Busy)yield return null;
   Assert.IsFalse(game.IsPlaying);Assert.IsTrue(enemy.gameObject.activeSelf);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);
   float elapsed=game.Elapsed;Vector3 position=enemy.transform.position;yield return new WaitForSecondsRealtime(.15f);
   Assert.AreEqual(elapsed,game.Elapsed);Assert.AreEqual(position,enemy.transform.position);
   flow.Play(false);while(flow.Busy)yield return null;
   Assert.AreSame(game,GameManager.Instance);Assert.IsTrue(game.IsPlaying);Assert.Contains(enemy,game.Spawner.Active);
   Assert.IsFalse(SceneManager.GetSceneByPath(SceneFlow.MenuPath).isLoaded);
   flow.ShowMenu();while(flow.Busy)yield return null;flow.Play(true);while(flow.Busy)yield return null;
   Assert.AreEqual(1,game.Wave);Assert.AreEqual(0,game.Kills);Assert.AreEqual(game.Player.Stamina.Maximum,game.Player.Stamina.Value);
  }
 }
}

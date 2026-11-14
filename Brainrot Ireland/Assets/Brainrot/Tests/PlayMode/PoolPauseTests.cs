using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Brainrot.Tests {
 public sealed class PoolPauseTests {
  Scene previous,scene;GameManager game;GameBalance profile;
  [UnitySetUp]public IEnumerator Setup(){
   Assert.IsNull(GameManager.Instance,"Запускайте тесты из Edit mode.");previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Проверка пула "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
   game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;game.Balance=profile;yield return null;game.StartRun();
  }
  [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);if(profile!=null)Object.Destroy(profile);yield return null;}
  [UnityTest]public IEnumerator ReusedPhysicalCorpseSpawnsAtOceanPoseNotOldIslandPose(){
   var first=game.Spawner.Spawn(EnemyKind.Normal,EnemySpawner.OceanSpawnPoint(0));first.TakeDamage(9999,Vector3.right*10);
   first.PhysicsBody.Rigidbody.position=Vector3.right*2;first.PhysicsBody.Rigidbody.linearVelocity=Vector3.left*7;
   game.Spawner.ReleaseCorpse(first);Vector3 spawn=EnemySpawner.OceanSpawnPoint(1.234f);
   var reused=game.Spawner.Spawn(EnemyKind.Normal,spawn);Assert.AreSame(first,reused);
   Assert.That(Vector3.Distance(spawn,reused.transform.position),Is.LessThan(.001f));Assert.That(Vector3.Distance(spawn,reused.PhysicsBody.Rigidbody.position),Is.LessThan(.001f));
   Assert.AreEqual(Vector3.zero,reused.PhysicsBody.Rigidbody.linearVelocity);Assert.IsTrue(reused.PhysicsBody.Rigidbody.isKinematic);Assert.AreEqual(RigidbodyInterpolation.None,reused.PhysicsBody.Rigidbody.interpolation);
   yield return new WaitForFixedUpdate();Assert.Greater(new Vector2(reused.transform.position.x,reused.transform.position.z).magnitude,25);
  }
  [UnityTest]public IEnumerator PausedContinueButtonIsRaycastableAndResumes(){
   game.Pause();yield return null;yield return null;
   Assert.AreEqual(0,Time.timeScale);var system=EventSystem.current;Assert.IsNotNull(system);Assert.AreEqual(scene,system.gameObject.scene);
   Button target=null;foreach(var button in game.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text=="ПРОДОЛЖИТЬ")target=button;
   Assert.IsNotNull(target);Assert.IsTrue(target.interactable);
   var data=new PointerEventData(system){position=RectTransformUtility.WorldToScreenPoint(null,target.transform.position),button=PointerEventData.InputButton.Left};
   var hits=new System.Collections.Generic.List<RaycastResult>();system.RaycastAll(data,hits);Assert.Greater(hits.Count,0);Assert.AreEqual(target.gameObject,hits[0].gameObject);
   ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.pointerClickHandler);Assert.IsTrue(game.IsPlaying);Assert.AreEqual(1,Time.timeScale);
  }
  [Test]public void ProductionSpawnRingRemainsOutsideTheIsland(){for(int i=0;i<72;i++){float a=i*Mathf.PI*2/72;var point=EnemySpawner.OceanSpawnPoint(a);Assert.IsFalse(IslandMap.IsWalkable(point,0));Assert.Greater(new Vector2(point.x,point.z).magnitude,IslandMap.CoastRadius(a)+5.9f);}}
 }
}

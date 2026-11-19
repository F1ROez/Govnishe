using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class IntermissionStallTests
    {
        [Test]public void PlacementAvoidsPlayerAndOccupiedPreferredPointAndStaysOnSand(){
            var preferred=new Vector3(5,0,3);Vector3 chosen;
            Assert.IsTrue(IntermissionShop.TryFindPlacement(preferred,preferred,p=>(p-preferred).sqrMagnitude<16,out chosen));
            Assert.GreaterOrEqual((chosen-preferred).sqrMagnitude,25);Assert.IsTrue(IslandMap.IsWalkable(chosen,5));
            Assert.IsFalse(IntermissionShop.TryFindPlacement(preferred,Vector3.zero,p=>true,out chosen),"An occupied island must not spawn a solid counter inside an obstacle.");
        }
        [Test]public void MarketIsBatchedAndSolidOnlyAfterArrival(){
            var go=new GameObject("Market presentation test");
            try{
                var stall=go.AddComponent<IslandMarketStall>();stall.Build();
                Assert.LessOrEqual(go.GetComponentsInChildren<Renderer>(true).Length,8);
                int triangles=0;
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true)){
                    triangles+=filter.sharedMesh.triangles.Length/3;
                    foreach(var normal in filter.sharedMesh.normals)Assert.Greater(normal.sqrMagnitude,.00001f,"Canvas sides must have separate, non-cancelling normals.");
                }
                Assert.Less(triangles,6000);Assert.AreEqual(0,go.GetComponentsInChildren<Light>(true).Length);
                stall.SetVisible(true);stall.TickPresentation(.2f,.6f);
                foreach(var collider in go.GetComponents<Collider>())Assert.IsFalse(collider.enabled);
                stall.TickPresentation(.5f,.6f);Assert.AreEqual(1,stall.Amount);
                foreach(var collider in go.GetComponents<Collider>())Assert.IsTrue(collider.enabled);
                stall.SetVisible(false);foreach(var collider in go.GetComponents<Collider>())Assert.IsFalse(collider.enabled,"The fading stall must never obstruct a new wave.");
                stall.TickPresentation(1,.6f);Assert.AreEqual(0,stall.Amount);
            }finally{Object.DestroyImmediate(go);}
        }
        [UnityTest]public IEnumerator FreshHoldRequiredAfterCombatShopAndPause(){
            Assert.IsNull(GameManager.Instance);
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("Intermission stall "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            var profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;
            var game=new GameObject("Intermission game").AddComponent<GameManager>();game.Balance=profile;
            try{
                yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;var stall=game.Stall;stall.enabled=false;
                stall.Tick(.5f,false,true);game.Spawner.Waves.Advance(10000,0,false);
                Assert.IsTrue(game.IsIntermission);stall.Tick(2,false,true);Assert.AreEqual(1,game.Wave,"Holding F before the wave ends cannot skip the shop phase.");
                stall.Tick(.01f,false,false);Assert.IsTrue(stall.Available);
                game.Player.transform.position=stall.WorldPosition-Vector3.up*2.7f+Vector3.forward*2.5f;
                Assert.IsTrue(stall.CanInteract);stall.Tick(.01f,true,false);
                Assert.AreEqual(GameState.RunShop,game.State);Assert.IsTrue(stall.CanInteract,"An open shop still needs a valid nearby counter for purchase guards.");
                stall.Tick(2,false,true);Assert.AreEqual(0,stall.StartHoldProgress);game.CloseRunShop();stall.Tick(2,false,true);Assert.AreEqual(1,game.Wave);
                stall.Tick(.01f,false,false);stall.Tick(.45f,false,true);Assert.Greater(stall.StartHoldProgress,0);game.Pause();stall.Tick(1,false,true);Assert.AreEqual(0,stall.StartHoldProgress);
                game.Resume();stall.Tick(2,false,true);Assert.AreEqual(1,game.Wave);stall.Tick(.01f,false,false);stall.Tick(1.1f,false,true);Assert.AreEqual(2,game.Wave);
            }finally{
                game.EndRun();if(GameMusic.Instance!=null)Object.DestroyImmediate(GameMusic.Instance.gameObject);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                Object.Destroy(profile);Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}

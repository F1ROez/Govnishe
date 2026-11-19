using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class DashPhaseTests
    {
        Scene previous,scene;GameManager game;GameBalance profile;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Dash phase "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;
            game=new GameObject("Dash test").AddComponent<GameManager>();game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;game.Player.Character=null;game.Player.ResetRun(false);
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            Time.timeScale=1;MobileInput.Clear();
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            Object.Destroy(profile);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        [UnityTest]public IEnumerator DashPassesEnemyButStillStopsAtWorldObstacle(){
            var player=game.Player;var start=player.transform.position;
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,start+Vector3.forward*1.5f,1);
            Assert.IsNotNull(enemy);var capsule=enemy.GetComponent<CapsuleCollider>();var motor=player.GetComponent<CharacterController>();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=start+Vector3.forward*4+Vector3.up*2;wall.transform.localScale=new Vector3(8,4,.3f);
            Physics.SyncTransforms();Assert.IsTrue(player.TryDash(Vector3.forward));
            Assert.IsTrue(Physics.GetIgnoreCollision(motor,capsule));Assert.IsFalse(Physics.GetIgnoreCollision(motor,wall.GetComponent<Collider>()));
            motor.Move(Vector3.forward*5);float traveled=player.transform.position.z-start.z;
            Assert.Greater(traveled,2.3f,"Enemy capsule must not block the dash.");Assert.Less(traveled,4,"World obstacles must remain solid.");
            player.EndDash();Assert.IsFalse(Physics.GetIgnoreCollision(motor,capsule));yield return null;
        }
        [UnityTest]public IEnumerator DamageAndOctopusCaptureAreRejectedOnlyWhileDashing(){
            var player=game.Player;float before=player.Health;
            Assert.IsTrue(player.TryDash(Vector3.right));
            player.TakeDamage(30,player.transform.position-Vector3.right,20);
            Assert.AreEqual(before,player.Health);Assert.AreEqual(0,player.DamageTakenCount);
            var octopus=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,player.transform.position+Vector3.right*3,1);
            Assert.IsNotNull(octopus);Assert.IsFalse(player.BeginCapture(octopus.GetComponent<OctopusAttack>()));
            player.EndDash();player.TakeDamage(10,player.transform.position-Vector3.right);
            Assert.AreEqual(before-10,player.Health);Assert.AreEqual(1,player.DamageTakenCount);yield return null;
        }
        [UnityTest]public IEnumerator PauseAndNewRunRestoreOwnedCollisionPairsButPreserveExistingIgnores(){
            var player=game.Player;var motor=player.GetComponent<CharacterController>();
            var a=game.Spawner.Spawn(EnemyKind.Normal,player.transform.position+Vector3.forward*2,1);
            var b=game.Spawner.Spawn(EnemyKind.Normal,player.transform.position+Vector3.right*2,1);
            Assert.IsNotNull(a);Assert.IsNotNull(b);var ca=a.GetComponent<CapsuleCollider>();var cb=b.GetComponent<CapsuleCollider>();
            Physics.IgnoreCollision(motor,cb,true);Assert.IsTrue(player.TryDash(Vector3.forward));
            game.Pause();Assert.IsFalse(player.IsDashing);Assert.IsFalse(Physics.GetIgnoreCollision(motor,ca));Assert.IsTrue(Physics.GetIgnoreCollision(motor,cb));
            game.Resume();player.ResetRun(false);Assert.IsTrue(player.TryDash(Vector3.right));player.ResetRun(false);
            Assert.IsFalse(player.IsDashing);Assert.IsFalse(Physics.GetIgnoreCollision(motor,ca));Physics.IgnoreCollision(motor,cb,false);yield return null;
        }
    }
}

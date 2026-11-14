using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class RunLifecycleTests {
  GameManager game;GameBalance profile;PlayerCharacterDefinition character;Scene scene,previous;
  [UnitySetUp]public IEnumerator Setup(){
   Assert.IsNull(GameManager.Instance,"Запускайте проверки из режима редактирования.");previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Проверка боя "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
   game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;profile.Normal.Health=200;profile.Normal.Damage=0;game.Balance=profile;
   character=ScriptableObject.CreateInstance<PlayerCharacterDefinition>();game.Player.Character=character;yield return null;game.StartRun();game.Player.Stats.CritChance=0;
  }
  [UnityTearDown]public IEnumerator Cleanup(){
   Time.timeScale=1;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
   if(profile!=null)Object.Destroy(profile);if(character!=null)Object.Destroy(character);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
  }
  EnemyController Spawn(float distance=2)=>game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*distance);
  void Teleport(Vector3 position){var c=game.Player.GetComponent<CharacterController>();c.enabled=false;game.Player.transform.position=position+Vector3.up*.15f;c.enabled=true;}
  [UnityTest]public IEnumerator TapHasDelayedContactAndNoPhysicalPush(){
   var enemy=Spawn();float hp=enemy.Health;var start=enemy.transform.position;Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));Assert.AreEqual(hp,enemy.Health);
   yield return new WaitForSeconds(.15f);Assert.That(enemy.Health,Is.EqualTo(hp-game.Player.Stats.Damage).Within(.01f));Assert.IsFalse(enemy.PhysicsBody.IsPhysical);Assert.That(enemy.transform.position.z,Is.LessThanOrEqualTo(start.z+.01f));
  }
  [UnityTest]public IEnumerator HeavyAttackAddsDamageAndPhysicalSpin(){
   var enemy=Spawn();float hp=enemy.Health;Assert.IsTrue(game.Player.Combat.ExecuteAttack(true));yield return new WaitForSeconds(.15f);
   Assert.That(enemy.Health,Is.EqualTo(hp-game.Player.Stats.Damage*profile.Combat.HeavyDamageMultiplier).Within(.01f));Assert.IsTrue(enemy.PhysicsBody.IsPhysical);Assert.IsFalse(enemy.PhysicsBody.Rigidbody.isKinematic);
  }
  [UnityTest]public IEnumerator HoldInputChargesAndFiresOnlyOnRelease(){
   var enemy=Spawn();var combat=game.Player.Combat;combat.UpdateAttackInput(false);combat.UpdateAttackInput(true);Assert.IsTrue(combat.IsCharging);float hp=enemy.Health;
   // Freeze automatic player input while driving this input state explicitly.
   game.Player.enabled=false;yield return new WaitForSeconds(profile.Combat.HeavyChargeSeconds+.05f);Assert.AreEqual(hp,enemy.Health);Assert.AreEqual(1,combat.ChargeProgress);
   combat.UpdateAttackInput(false);Assert.IsFalse(combat.IsCharging);yield return new WaitForSeconds(.15f);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);
  }
  [UnityTest]public IEnumerator DeathKeepsPhysicalCorpseThenReturnsItToPool(){
   profile.Bodies.CorpseSeconds=1;var enemy=Spawn();enemy.TakeDamage(9999,Vector3.zero);
   Assert.IsFalse(enemy.Alive);Assert.IsTrue(enemy.gameObject.activeSelf);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);Assert.AreEqual(1,game.Spawner.Corpses.Count);Assert.AreEqual(0,game.Spawner.Active.Count);Assert.AreEqual(1,game.Kills);
   yield return new WaitForSeconds(1.2f);Assert.AreEqual(0,game.Spawner.Corpses.Count);Assert.IsFalse(enemy.gameObject.activeSelf);
  }
  [UnityTest]public IEnumerator LivingTumbleRecoversUpright(){
   profile.Bodies.TumbleSeconds=.3f;var enemy=Spawn(5);enemy.Tumble(Vector3.right*9);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);yield return new WaitForSeconds(.5f);Assert.IsFalse(enemy.PhysicsBody.IsPhysical);Assert.Greater(Vector3.Dot(enemy.transform.up,Vector3.up),.99f);
  }
  [UnityTest]public IEnumerator MovingSharkCompletesAttackAfterPlayerLeavesRange(){
   profile.Normal.Damage=8;var enemy=Spawn(1.7f);enemy.Tick(.61f);Assert.AreEqual(EnemyAction.MeleeWindup,enemy.Action);
   var before=enemy.transform.position;Teleport(Vector3.right*12);float hp=game.Player.Health;enemy.Tick(.66f);
   Assert.Greater((enemy.transform.position-before).magnitude,.1f);Assert.AreEqual(EnemyAction.Recovery,enemy.Action);Assert.AreEqual(hp,game.Player.Health);yield return null;
  }
  [UnityTest]public IEnumerator BodiesCollideAndTransferPush(){
   var first=Spawn(4);var second=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*7);
   first.Tumble(Vector3.forward*11);yield return new WaitForSeconds(.45f);Assert.IsTrue(second.PhysicsBody.IsPhysical,"Кувыркающееся тело должно толкнуть вторую акулу.");
  }
  [UnityTest]public IEnumerator PauseCancelsChargedAndPendingHits(){
   var enemy=Spawn();float hp=enemy.Health;game.Player.Combat.ExecuteAttack(true);game.Pause();Assert.AreEqual(0,Time.timeScale);game.Resume();yield return new WaitForSeconds(.2f);Assert.AreEqual(hp,enemy.Health);
  }
  [UnityTest]public IEnumerator RestartResetsWavesAndCorpsesAndKeepsXpDisabled(){
   var enemy=Spawn();enemy.TakeDamage(9999,Vector3.zero);Assert.AreEqual(0,game.Upgrades.Experience);Assert.AreEqual(1,game.Upgrades.Level);
   game.Spawner.Waves.Advance(140);Assert.Greater(game.Wave,1);game.StartRun();Assert.AreEqual(1,game.Wave);Assert.AreEqual(0,game.Spawner.Corpses.Count);Assert.AreEqual(0,game.Spawner.Active.Count);yield return null;
  }
  [Test]public void WaveHasQuietEdgesAndMiddlePeak(){Assert.That(WaveDirector.Envelope(0),Is.EqualTo(0).Within(.00001));Assert.That(WaveDirector.Envelope(1),Is.EqualTo(0).Within(.00001));Assert.That(WaveDirector.Envelope(.5f),Is.EqualTo(1).Within(.00001));Assert.Greater(EnemySpawner.Interval(profile.Waves,0),EnemySpawner.Interval(profile.Waves,1));}
  [Test]public void RestAndNextWaveAreDeterministic(){var wave=new WaveDirector();wave.Reset();wave.Advance(60);Assert.IsTrue(wave.Resting);Assert.AreEqual(1,wave.Number);wave.Advance(8);Assert.IsFalse(wave.Resting);Assert.AreEqual(2,wave.Number);}
  [Test]public void NormalOnlySpawnAndSmallIsland(){Assert.IsNotNull(Spawn());Assert.IsNull(game.Spawner.Spawn(EnemyKind.King,Vector3.zero));Assert.IsNull(game.Spawner.Spawn(EnemyKind.LiriliLarila,Vector3.zero));Assert.IsFalse(IslandMap.IsWalkable(Vector3.forward*23));}
 }
}

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
   Assert.That(enemy.Health,Is.EqualTo(hp-game.Player.Stats.Damage*profile.Combat.StrongDamageMultiplier).Within(.01f));Assert.IsTrue(enemy.PhysicsBody.IsPhysical);Assert.IsFalse(enemy.PhysicsBody.Rigidbody.isKinematic);
  }
  [UnityTest]public IEnumerator HoldInputChargesAndFiresOnlyOnRelease(){
   var enemy=Spawn();var combat=game.Player.Combat;combat.UpdateAttackInput(false);combat.UpdateAttackInput(true);Assert.IsTrue(combat.IsCharging);float hp=enemy.Health;
   // Freeze automatic player input while driving this input state explicitly.
   game.Player.enabled=false;yield return new WaitForSeconds(profile.Combat.HeavyChargeSeconds+.05f);Assert.AreEqual(hp,enemy.Health);Assert.AreEqual(1,combat.ChargeProgress);
   combat.UpdateAttackInput(false);Assert.IsFalse(combat.IsCharging);yield return new WaitForSeconds(.15f);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);
  }
  [UnityTest]public IEnumerator DeathKeepsPhysicalCorpseThenReturnsItToPool(){
   profile.Bodies.DeathBodyLifetime=1;var enemy=Spawn();enemy.TakeDamage(9999,Vector3.zero);
   Assert.IsFalse(enemy.Alive);Assert.IsTrue(enemy.gameObject.activeSelf);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);Assert.AreEqual(1,game.Spawner.Corpses.Count);Assert.AreEqual(0,game.Spawner.Active.Count);Assert.AreEqual(1,game.Kills);
   yield return new WaitForSeconds(1.2f);Assert.AreEqual(0,game.Spawner.Corpses.Count);Assert.IsFalse(enemy.gameObject.activeSelf);
  }
  [UnityTest]public IEnumerator LivingTumbleRecoversUpright(){
   profile.Bodies.TumbleSeconds=.3f;var enemy=Spawn(5);enemy.Tumble(Vector3.right*9);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);yield return new WaitForSeconds(.5f);Assert.IsFalse(enemy.PhysicsBody.IsPhysical);Assert.Greater(Vector3.Dot(enemy.transform.up,Vector3.up),.99f);
  }
  [UnityTest]public IEnumerator MovingSharkCompletesAttackAfterPlayerLeavesRange(){
   profile.Normal.Damage=8;var enemy=Spawn(1.7f);enemy.Tick(.61f);Assert.AreEqual(EnemyAction.MeleeWindup,enemy.Action);
   var before=enemy.transform.position;Teleport(Vector3.right*12);float hp=game.Player.Health;enemy.Tick(.66f);enemy.Tick(.33f);
   Assert.Greater((enemy.transform.position-before).magnitude,.1f);Assert.AreEqual(EnemyAction.Recovery,enemy.Action);Assert.AreEqual(hp,game.Player.Health);yield return null;
  }
  [UnityTest]public IEnumerator BodiesCollideWithoutTransferringTumble(){
   var first=Spawn(4);var second=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+Vector3.forward*7);
   first.Tumble(Vector3.forward*11);yield return new WaitForSeconds(.45f);Assert.IsFalse(second.PhysicsBody.IsPhysical,"Столкновение не должно включать кувырок другой акулы.");
  }
  [UnityTest]public IEnumerator PauseCancelsChargedAndPendingHits(){
   var enemy=Spawn();float hp=enemy.Health;game.Player.Combat.ExecuteAttack(true);game.Pause();Assert.AreEqual(0,Time.timeScale);game.Resume();yield return new WaitForSeconds(.2f);Assert.AreEqual(hp,enemy.Health);
  }
  [UnityTest]public IEnumerator RestartResetsWavesAndCorpsesAndKeepsXpDisabled(){
   var enemy=Spawn();enemy.TakeDamage(9999,Vector3.zero);Assert.AreEqual(0,game.Upgrades.Experience);Assert.AreEqual(1,game.Upgrades.Level);
   game.Spawner.Waves.Advance(60);game.Spawner.Waves.Advance(8);Assert.Greater(game.Wave,1);game.StartRun();Assert.AreEqual(1,game.Wave);Assert.AreEqual(0,game.Spawner.Corpses.Count);Assert.AreEqual(0,game.Spawner.Active.Count);yield return null;
  }
  [UnityTest]public IEnumerator HeavyCooldownIsIndependentAndResetsOnNewRun(){
   Assert.That(profile.Combat.StrongDamageMultiplier,Is.EqualTo(2.5f));Assert.That(profile.Combat.StrongCooldownSeconds,Is.EqualTo(10));
   Assert.IsTrue(game.Player.Combat.ExecuteAttack(true));Assert.That(game.Player.Combat.HeavyCooldownRemaining,Is.EqualTo(10).Within(.05f));
   yield return new WaitForSeconds(.75f);Assert.IsFalse(game.Player.Combat.ExecuteAttack(true));Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));
   game.StartRun();Assert.AreEqual(0,game.Player.Combat.HeavyCooldownRemaining);Assert.AreEqual(0,game.Player.DashCooldownRemaining);
  }
  [UnityTest]public IEnumerator CooldownDoesNotTickWhilePaused(){
   game.Player.Combat.ExecuteAttack(true);float before=game.Player.Combat.HeavyCooldownRemaining;game.Pause();yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(before,game.Player.Combat.HeavyCooldownRemaining);game.Resume();
  }
  [UnityTest]public IEnumerator SharkBiteLungesAndReturns(){
   var enemy=Spawn(4);var visual=enemy.GetComponentInChildren<CharacterVisual>();var body=visual.transform.GetChild(0);var rest=body.localPosition;
   visual.PlayBite(.32f);yield return new WaitForSeconds(.12f);Assert.Greater(body.localPosition.z,rest.z+.2f);
   yield return new WaitForSeconds(.28f);Assert.That(body.localPosition.z,Is.EqualTo(rest.z).Within(.05f));
  }
  [UnityTest]public IEnumerator OctopusLocksTargetAndStrongHitInterrupts(){
   var oct=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,Vector3.forward*10);var attack=oct.GetComponent<OctopusAttack>();
   attack.Tick(.01f);Assert.AreEqual(OctopusPhase.Aiming,attack.Phase);var target=attack.Target;
   Teleport(Vector3.right*4);attack.Tick(.2f);Assert.AreEqual(target,attack.Target);
   oct.TakeDamage(1,Vector3.forward*10);Assert.IsTrue(oct.PhysicsBody.IsPhysical);Assert.AreEqual(OctopusPhase.Retreat,attack.Phase);yield return null;
  }
  [UnityTest]public IEnumerator CaptureBlocksAttackAndTapsReleasePlayer(){
   var oct=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,Vector3.forward*8);var attack=oct.GetComponent<OctopusAttack>();
   Assert.IsTrue(game.Player.BeginCapture(attack));Assert.IsFalse(game.Player.Combat.ExecuteAttack(false));Assert.IsTrue(game.Player.TryDash(Vector3.forward));
   for(int i=0;i<profile.Octopus.EscapeClicks;i++)game.Player.EscapeTap();Assert.IsFalse(game.Player.IsCaptured);Assert.AreEqual(OctopusPhase.Dropping,attack.Phase);yield return null;
  }
  [UnityTest]public IEnumerator CapturedPlayerCanMoveAndResumeKeepsState(){
   var oct=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,Vector3.forward*12);var attack=oct.GetComponent<OctopusAttack>();
   Assert.IsTrue(game.Player.BeginCapture(attack));var start=game.Player.transform.position;
   Assert.IsTrue(game.Player.TryDash(Vector3.right));yield return new WaitForSeconds(.08f);Assert.Greater(game.Player.transform.position.x,start.x+.1f);Assert.IsFalse(game.Player.Combat.ExecuteAttack(false));
   float health=game.Player.Health,stamina=game.Player.Stamina.Value;int count=game.Spawner.Active.Count;
   game.SuspendRun();yield return null;Assert.IsTrue(game.HasRun);Assert.AreEqual(count,game.Spawner.Active.Count);game.ResumeRun();Assert.IsTrue(game.IsPlaying);Assert.AreEqual(health,game.Player.Health);Assert.AreEqual(stamina,game.Player.Stamina.Value);
  }
  [Test]public void WaveWaitsForEveryLiveEnemyBeforeStartingBreak(){
   var w=new WaveDirector();w.Reset();w.Advance(60,2);Assert.IsTrue(w.WaitingForClear);w.Advance(100,1);Assert.AreEqual(1,w.Number);
   w.Advance(.01f,0);Assert.IsFalse(w.WaitingForClear);w.Advance(8,0);Assert.AreEqual(2,w.Number);
  }
  [Test]public void StaminaDrainsOnlyForMovingSprintAndRecovers(){
   var t=new StaminaTuning();var stamina=new StaminaPool();stamina.Reset(t);
   Assert.IsTrue(stamina.Tick(true,true,false,1,t));Assert.AreEqual(75,stamina.Value);
   stamina.Tick(true,false,false,2,t);Assert.Greater(stamina.Value,75);
   stamina.Tick(true,true,false,10,t);Assert.AreEqual(0,stamina.Value);Assert.IsTrue(stamina.Exhausted);
   stamina.Tick(false,false,false,10,t);Assert.AreEqual(100,stamina.Value);Assert.IsTrue(stamina.Tick(true,true,false,.01f,t));
  }
  [Test]public void NewBodyLifetimeIsFourSeconds(){Assert.AreEqual(4,profile.Bodies.DeathBodyLifetime);}
  [Test]public void WaveHasQuietEdgesAndMiddlePeak(){Assert.That(WaveDirector.Envelope(0),Is.EqualTo(0).Within(.00001));Assert.That(WaveDirector.Envelope(1),Is.EqualTo(0).Within(.00001));Assert.That(WaveDirector.Envelope(.5f),Is.EqualTo(1).Within(.00001));Assert.Greater(EnemySpawner.Interval(profile.Waves,0),EnemySpawner.Interval(profile.Waves,1));}
  [Test]public void RestAndNextWaveAreDeterministic(){var wave=new WaveDirector();wave.Reset();wave.Advance(60);Assert.IsTrue(wave.Resting);Assert.AreEqual(1,wave.Number);wave.Advance(8);Assert.IsFalse(wave.Resting);Assert.AreEqual(2,wave.Number);}
  [Test]public void NormalOnlySpawnAndSmallIsland(){Assert.IsNotNull(Spawn());Assert.IsNull(game.Spawner.Spawn(EnemyKind.King,Vector3.zero));Assert.IsNull(game.Spawner.Spawn(EnemyKind.LiriliLarila,Vector3.zero));Assert.IsFalse(IslandMap.IsWalkable(Vector3.forward*23));}
 }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class RunLifecycleTests {
  GameManager game; GameBalance profile; Scene testScene,previousScene;
  bool oldLock,oldPreference,oldRange;
  [UnitySetUp] public IEnumerator Setup(){
   Assert.IsNull(GameManager.Instance,"Run tests from Test Runner in Edit mode, without an existing live game.");
   oldLock=SaveSystem.Data.CameraLocked;oldPreference=SaveSystem.Data.HasCameraPreference;oldRange=SaveSystem.Data.ShowAttackRange;
   previousScene=SceneManager.GetActiveScene();testScene=SceneManager.CreateScene("Brainrot test "+System.Guid.NewGuid());SceneManager.SetActiveScene(testScene);
   game=new GameObject("Test arena").AddComponent<GameManager>();
   profile=ScriptableObject.CreateInstance<GameBalance>();game.Balance=profile;
   SaveSystem.Data.CameraLocked=true;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Data.ShowAttackRange=true;
   yield return null;game.StartRun();
  }
  [UnityTearDown] public IEnumerator Cleanup(){
   Time.timeScale=1;SaveSystem.Data.CameraLocked=oldLock;SaveSystem.Data.HasCameraPreference=oldPreference;SaveSystem.Data.ShowAttackRange=oldRange;
   if(previousScene.IsValid()&&previousScene.isLoaded)SceneManager.SetActiveScene(previousScene);
   if(testScene.IsValid()&&testScene.isLoaded)yield return SceneManager.UnloadSceneAsync(testScene);
   if(profile!=null)Object.Destroy(profile);yield return null;
  }
  [UnityTest] public IEnumerator KillRewardsExperienceAndReturnsEnemyToPool(){
   var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2);enemy.TakeDamage(9999,Vector3.zero);
   Assert.AreEqual(1,game.Kills);Assert.AreEqual(profile.Normal.Experience,game.Upgrades.Experience);
   Assert.IsFalse(enemy.gameObject.activeSelf);Assert.AreEqual(0,game.Spawner.Active.Count);yield return null;
  }
  [UnityTest] public IEnumerator QueuedLevelsPauseUntilEveryChoiceIsApplied(){
   int xp=profile.RequiredXp(1)+profile.RequiredXp(2)+profile.RequiredXp(3);
   game.Upgrades.AddExperience(xp);Assert.AreEqual(GameState.Upgrade,game.State);Assert.AreEqual(0,Time.timeScale);
   int selections=0;while(game.State==GameState.Upgrade&&selections<10){Assert.Greater(game.Upgrades.Choices.Count,0);selections++;game.Upgrades.Choose(0);}
   Assert.AreEqual(GameState.Playing,game.State);Assert.AreEqual(4,game.Upgrades.Level);Assert.AreEqual(3,selections);yield return null;
  }
  [UnityTest] public IEnumerator FirstLevelRequiresSeveralKillsAndCostsAccelerate(){
   for(int i=0;i<5;i++)game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2).TakeDamage(9999,Vector3.zero);
   Assert.AreEqual(1,game.Upgrades.Level);Assert.AreEqual(GameState.Playing,game.State);
   game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2).TakeDamage(9999,Vector3.zero);
   Assert.AreEqual(2,game.Upgrades.Level);Assert.AreEqual(GameState.Upgrade,game.State);
   int firstDelta=profile.RequiredXp(2)-profile.RequiredXp(1);int laterDelta=profile.RequiredXp(4)-profile.RequiredXp(3);
   Assert.Greater(laterDelta,firstDelta);yield return null;
  }
  [UnityTest] public IEnumerator PauseFreezesRunAndRestartResetsProgress(){
   game.Spawner.Spawn(EnemyKind.Tank,Vector3.forward*20);game.Pause();float elapsed=game.Elapsed;
   yield return null;yield return null;Assert.AreEqual(elapsed,game.Elapsed);game.StartRun();
   Assert.AreEqual(0,game.Spawner.Active.Count);Assert.AreEqual(1,game.Upgrades.Level);Assert.AreEqual(profile.Player.MaxHealth,game.Player.Health);Assert.AreEqual(GameState.Playing,game.State);
  }
  [UnityTest] public IEnumerator BackwardDashKeepsCameraAimAndIdleDoesNotSpendCooldown(){
   var player=game.Player;Assert.IsTrue(player.CameraLocked);Assert.IsFalse(player.TryDash(Vector3.zero));
   Assert.IsTrue(player.TryDash(Vector3.back));Assert.AreEqual(Vector3.back,player.DashDirection);
   yield return null;Assert.Greater(Vector3.Dot(player.transform.forward,player.AimForward),.99f);Assert.IsFalse(player.TryDash(Vector3.right));
  }
  [UnityTest] public IEnumerator MeleeHasWindupAndMovingOutOfRangeAvoidsDamage(){
   float before=game.Player.Health;var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*1.4f);
   enemy.Tick(.61f);Assert.AreEqual(EnemyAction.MeleeWindup,enemy.Action);Assert.AreEqual(before,game.Player.Health);
   TeleportPlayer(Vector3.right*5);enemy.Tick(profile.Normal.MeleeWindup+.01f);
   Assert.AreEqual(before,game.Player.Health);Assert.AreEqual(EnemyAction.Recovery,enemy.Action);yield return null;
  }
  [UnityTest] public IEnumerator MeleeDealsDamageOnlyAfterWindupAndHitInterruptsNextAttack(){
   float before=game.Player.Health;var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*1.4f);
   enemy.Tick(.61f);Assert.AreEqual(before,game.Player.Health);enemy.Tick(profile.Normal.MeleeWindup+.01f);
   Assert.Less(game.Player.Health,before);float after=game.Player.Health;enemy.Tick(.02f);Assert.AreEqual(after,game.Player.Health);
   game.Spawner.ResetRun();enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*1.4f);enemy.Tick(.61f);enemy.TakeDamage(1,Vector3.zero);
   Assert.AreEqual(EnemyAction.Staggered,enemy.Action);Assert.IsFalse(enemy.IsPreparingAttack);yield return null;
  }
  [UnityTest] public IEnumerator ChargerCommitsToTelegraphAndDoesNotHomeAfterWindup(){
   var enemy=game.Spawner.Spawn(EnemyKind.Fast,Vector3.forward*8);enemy.Tick(.81f);
   Assert.AreEqual(EnemyAction.ChargeWindup,enemy.Action);float before=game.Player.Health;
   TeleportPlayer(Vector3.right*4);enemy.Tick(profile.Fast.ChargeWindup+.01f);Assert.AreEqual(EnemyAction.Charging,enemy.Action);
   enemy.Tick(profile.Fast.ChargeDuration);Assert.That(enemy.transform.position.x,Is.EqualTo(0).Within(.01f));Assert.AreEqual(before,game.Player.Health);yield return null;
  }
  [UnityTest] public IEnumerator SpawnDelayAndSoftCapPreventOpeningFlood(){
   profile.Spawning.InitialDelay=2;profile.Spawning.Interval=AnimationCurve.Constant(0,600,.35f);profile.Spawning.AliveLimit=AnimationCurve.Constant(0,600,2);game.Spawner.ResetRun();
   game.Spawner.Tick(1.9f);Assert.AreEqual(0,game.Spawner.Active.Count);game.Spawner.Tick(.11f);Assert.AreEqual(1,game.Spawner.Active.Count);
   for(int i=0;i<10;i++)game.Spawner.Tick(.36f);Assert.AreEqual(2,game.Spawner.Active.Count);yield return null;
  }
  [UnityTest] public IEnumerator HitTintIsPerEnemyAndClearsWhenPooled(){
   var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2);
   var other=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*4);
   var renderer=enemy.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>();
   Color original=renderer.sharedMaterial.color;enemy.TakeDamage(1,Vector3.back*4);
   var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);Assert.IsFalse(block.isEmpty);Assert.AreEqual(original,renderer.sharedMaterial.color);
   var otherRenderer=other.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>();
   otherRenderer.GetPropertyBlock(block);Assert.IsTrue(block.isEmpty);
   game.Spawner.ResetRun();var reused=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2);
   reused.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>().GetPropertyBlock(block);
   Assert.IsTrue(block.isEmpty);Assert.AreEqual(EnemyAction.Chasing,reused.Action);yield return null;
  }
  void TeleportPlayer(Vector3 pos){var motor=game.Player.GetComponent<CharacterController>();motor.enabled=false;game.Player.transform.position=pos+Vector3.up*.15f;motor.enabled=true;}
  [Test] public void MeleeSectorMatchesFrontReachAndRejectsBehind(){
   Assert.IsTrue(CombatSystem.InMeleeSector(Vector3.zero,Vector3.forward,Vector3.forward*3,3.2f,130,.8f));
   Assert.IsFalse(CombatSystem.InMeleeSector(Vector3.zero,Vector3.forward,Vector3.back*2,3.2f,130,.8f));
   Assert.IsFalse(CombatSystem.InMeleeSector(Vector3.zero,Vector3.forward,Vector3.forward*3.3f,3.2f,130,.8f));
   Assert.IsTrue(CombatSystem.InMeleeSector(Vector3.zero,Vector3.forward,Vector3.back*.5f,3.2f,130,.8f));
  }
  [Test] public void ChargeSweptHitCatchesCrossingButRejectsSideStep(){
   Assert.IsTrue(EnemyController.SweptHitXZ(Vector3.back*5,Vector3.forward*5,Vector3.zero,1));
   Assert.IsFalse(EnemyController.SweptHitXZ(Vector3.back*5,Vector3.forward*5,Vector3.right*2,1));
  }
 }
}

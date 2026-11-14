using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Brainrot.Tests
{
    public sealed class RunLifecycleTests
    {
        GameManager game;
        GameBalance profile;
        PlayerCharacterDefinition character;
        Scene testScene, previousScene;
        bool oldLock, oldPreference, oldRange;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsNull(GameManager.Instance, "Запускайте Test Runner из режима редактирования, без уже запущенной игры.");
            oldLock = SaveSystem.Data.CameraLocked;
            oldPreference = SaveSystem.Data.HasCameraPreference;
            oldRange = SaveSystem.Data.ShowAttackRange;
            previousScene = SceneManager.GetActiveScene();
            testScene = SceneManager.CreateScene("Проверка острова " + System.Guid.NewGuid());
            SceneManager.SetActiveScene(testScene);
            game = new GameObject("Тестовая игра").AddComponent<GameManager>();
            profile = ScriptableObject.CreateInstance<GameBalance>();
            // No random arrivals during unrelated assertions; population tests opt in explicitly.
            profile.Exploration.ZoneEntryPopulation = 0;
            profile.Exploration.SpawnIntervalMin = profile.Exploration.SpawnIntervalMax = 600;
            game.Balance = profile;
            character = ScriptableObject.CreateInstance<PlayerCharacterDefinition>();
            game.Player.Character = character;
            SaveSystem.Data.CameraLocked = true;
            SaveSystem.Data.HasCameraPreference = true;
            SaveSystem.Data.ShowAttackRange = true;
            yield return null;
            game.StartRun();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            SaveSystem.Data.CameraLocked = oldLock;
            SaveSystem.Data.HasCameraPreference = oldPreference;
            SaveSystem.Data.ShowAttackRange = oldRange;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (testScene.IsValid() && testScene.isLoaded) yield return SceneManager.UnloadSceneAsync(testScene);
            if (profile != null) Object.Destroy(profile);
            if (character != null) Object.Destroy(character);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator KillRewardsExperienceAndReturnsEnemyToPool()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2);
            enemy.TakeDamage(9999, Vector3.zero);
            Assert.AreEqual(1, game.Kills);
            Assert.AreEqual(profile.Normal.Experience, game.Upgrades.Experience);
            Assert.IsFalse(enemy.gameObject.activeSelf);
            Assert.AreEqual(0, game.Spawner.Active.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator QueuedLevelsPauseUntilEveryChoiceIsApplied()
        {
            int xp = profile.RequiredXp(1) + profile.RequiredXp(2) + profile.RequiredXp(3);
            game.Upgrades.AddExperience(xp);
            Assert.AreEqual(GameState.Upgrade, game.State);
            Assert.AreEqual(0, Time.timeScale);
            int selections = 0;
            while (game.State == GameState.Upgrade && selections < 10)
            {
                Assert.Greater(game.Upgrades.Choices.Count, 0);
                selections++;
                game.Upgrades.Choose(0);
            }
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(4, game.Upgrades.Level);
            Assert.AreEqual(3, selections);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstLevelRequiresSeveralKillsAndCostsAccelerate()
        {
            for (int i = 0; i < 5; i++) SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2).TakeDamage(9999, Vector3.zero);
            Assert.AreEqual(1, game.Upgrades.Level);
            Assert.AreEqual(GameState.Playing, game.State);
            SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2).TakeDamage(9999, Vector3.zero);
            Assert.AreEqual(2, game.Upgrades.Level);
            Assert.AreEqual(GameState.Upgrade, game.State);
            Assert.Greater(profile.RequiredXp(4) - profile.RequiredXp(3), profile.RequiredXp(2) - profile.RequiredXp(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseFreezesRunAndRestartReturnsToStartingBeach()
        {
            SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 20);
            game.Pause();
            float elapsed = game.Elapsed;
            yield return null;
            yield return null;
            Assert.AreEqual(elapsed, game.Elapsed);
            TeleportPlayer(IslandMap.Center(IslandZone.Ruins));
            game.StartRun();
            Assert.AreEqual(0, game.Spawner.Active.Count);
            Assert.AreEqual(1, game.Upgrades.Level);
            Assert.AreEqual(profile.Player.MaxHealth, game.Player.Health);
            Assert.That(Vector3.Distance(IslandMap.StartPosition, game.Player.transform.position), Is.LessThan(.01f));
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [UnityTest]
        public IEnumerator OpenMapFreezesRunAndSpawningUntilClosed()
        {
            float elapsed = game.Elapsed;
            int population = game.Spawner.Active.Count;
            game.OpenMap();
            Assert.AreEqual(GameState.Map, game.State);
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(game.IsPlaying);
            game.Spawner.Tick(600);
            yield return null;
            yield return null;
            Assert.AreEqual(elapsed, game.Elapsed);
            Assert.AreEqual(population, game.Spawner.Active.Count);
            game.CloseMap();
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator BackwardDashKeepsCameraAimAndIdleDoesNotSpendCooldown()
        {
            var player = game.Player;
            Assert.IsTrue(player.CameraLocked);
            Assert.IsFalse(player.TryDash(Vector3.zero));
            Assert.IsTrue(player.TryDash(Vector3.back));
            Assert.AreEqual(Vector3.back, player.DashDirection);
            yield return null;
            Assert.Greater(Vector3.Dot(player.transform.forward, player.AimForward), .99f);
            Assert.IsFalse(player.TryDash(Vector3.right));
        }

        [UnityTest]
        public IEnumerator FarEnemyIdlesOrWandersWithoutPursuingPlayer()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 25);
            Vector3 home = enemy.HomePosition;
            for (int i = 0; i < 20; i++)
            {
                enemy.Tick(.5f);
                Assert.IsFalse(enemy.IsAlerted);
                Assert.IsTrue(enemy.Action == EnemyAction.Idle || enemy.Action == EnemyAction.Wandering);
                Assert.AreEqual(IslandZone.Beach, IslandMap.GetZone(enemy.transform.position));
            }
            Assert.That(Vector3.Distance(home, enemy.transform.position), Is.LessThan(profile.Exploration.WanderRadius + .1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearbyEnemyDetectsPlayerOnlyInsideItsHomeZone()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 8);
            Assert.IsFalse(enemy.IsAlerted);
            enemy.Tick(.01f);
            Assert.IsTrue(enemy.IsAlerted);
            Assert.AreEqual(EnemyAction.Chasing, enemy.Action);
            TeleportPlayer(IslandMap.Center(IslandZone.Meadows));
            enemy.Tick(.01f);
            Assert.IsFalse(enemy.IsAlerted);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnteringAnotherZoneCancelsPreparedAttackImmediately()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 1.4f);
            enemy.Tick(.61f);
            Assert.AreEqual(EnemyAction.MeleeWindup, enemy.Action);
            float before = game.Player.Health;
            TeleportPlayer(IslandMap.Center(IslandZone.Meadows));
            enemy.Tick(.01f);
            Assert.IsFalse(enemy.IsAlerted);
            Assert.IsFalse(enemy.IsPreparingAttack);
            Assert.IsTrue(enemy.Action == EnemyAction.Idle || enemy.Action == EnemyAction.Wandering);
            enemy.Tick(profile.Normal.MeleeWindup + 1);
            Assert.AreEqual(before, game.Player.Health);
            Assert.AreEqual(IslandZone.Beach, IslandMap.GetZone(enemy.transform.position));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeleeHasWindupAndMovingOutOfRangeAvoidsDamage()
        {
            Vector3 start = game.Player.transform.position;
            float before = game.Player.Health;
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 1.4f);
            enemy.Tick(.61f);
            Assert.AreEqual(EnemyAction.MeleeWindup, enemy.Action);
            Assert.AreEqual(before, game.Player.Health);
            TeleportPlayer(start + Vector3.right * 5);
            enemy.Tick(profile.Normal.MeleeWindup + .01f);
            Assert.AreEqual(before, game.Player.Health);
            Assert.AreEqual(EnemyAction.Recovery, enemy.Action);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeleeDealsOneDelayedHitAndPlayerHitInterruptsPreparation()
        {
            float before = game.Player.Health;
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 1.4f);
            enemy.Tick(.61f);
            Assert.AreEqual(before, game.Player.Health);
            enemy.Tick(profile.Normal.MeleeWindup + .01f);
            Assert.Less(game.Player.Health, before);
            float after = game.Player.Health;
            enemy.Tick(.02f);
            Assert.AreEqual(after, game.Player.Health);
            game.Spawner.ResetRun();
            enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 1.4f);
            enemy.Tick(.61f);
            enemy.TakeDamage(1, Vector3.zero);
            Assert.AreEqual(EnemyAction.Staggered, enemy.Action);
            Assert.IsFalse(enemy.IsPreparingAttack);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeadowChargerCommitsToShownDirectionRatherThanHoming()
        {
            Vector3 meadow = IslandMap.Center(IslandZone.Meadows);
            TeleportPlayer(meadow);
            var enemy = SpawnNearPlayer(EnemyKind.LiriliLarila, Vector3.forward * 8);
            enemy.Tick(.81f);
            Assert.AreEqual(EnemyAction.ChargeWindup, enemy.Action);
            float before = game.Player.Health;
            TeleportPlayer(meadow + Vector3.right * 5);
            enemy.Tick(profile.Lirili.ChargeWindup + .01f);
            Assert.AreEqual(EnemyAction.Charging, enemy.Action);
            enemy.Tick(profile.Lirili.ChargeDuration);
            Assert.That(enemy.transform.position.x, Is.EqualTo(meadow.x).Within(.02f));
            Assert.AreEqual(before, game.Player.Health);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnRejectsWrongSpeciesOtherZonesAndEveryBoss()
        {
            Assert.IsNull(game.Spawner.Spawn(EnemyKind.LiriliLarila, game.Player.transform.position + Vector3.forward * 2));
            Assert.IsNull(game.Spawner.Spawn(EnemyKind.Normal, IslandMap.Center(IslandZone.Forest)));
            Assert.IsNull(game.Spawner.Spawn(EnemyKind.MiniBoss, game.Player.transform.position));
            Assert.IsNull(game.Spawner.Spawn(EnemyKind.King, game.Player.transform.position));
            Assert.IsNull(game.Spawner.Spawn(EnemyKind.Fast, game.Player.transform.position));
            for (int i = 0; i < 5; i++)
            {
                var zone = (IslandZone)i;
                TeleportPlayer(IslandMap.Center(zone));
                var enemy = SpawnNearPlayer(IslandMap.EnemyForZone(zone), Vector3.forward * 2);
                Assert.AreEqual(zone, enemy.HomeZone);
                Assert.AreEqual(IslandMap.EnemyForZone(zone), enemy.Kind);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RandomPopulationAppearsOnlyInCurrentLocation()
        {
            profile.Exploration.ZoneEntryPopulation = 3;
            game.Spawner.ResetRun();
            game.Spawner.Tick(.01f);
            Assert.AreEqual(3, game.Spawner.CountInZone(IslandZone.Beach));
            foreach (var enemy in game.Spawner.Active) Assert.AreEqual(EnemyKind.Normal, enemy.Kind);
            TeleportPlayer(IslandMap.Center(IslandZone.Forest));
            game.Spawner.Tick(.01f);
            Assert.AreEqual(3, game.Spawner.CountInZone(IslandZone.Forest));
            Assert.AreEqual(3, game.Spawner.CountInZone(IslandZone.Beach));
            Assert.AreEqual(0, game.Spawner.CountInZone(IslandZone.Meadows));
            Assert.AreEqual(0, game.Spawner.CountInZone(IslandZone.Ruins));
            Assert.AreEqual(0, game.Spawner.CountInZone(IslandZone.Swamp));
            foreach (var enemy in game.Spawner.Active)
                if (enemy.HomeZone == IslandZone.Forest) Assert.AreEqual(EnemyKind.BrrBrrPatapim, enemy.Kind);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LongBossTickNeverCreatesBosses()
        {
            var bosses = game.GetComponent<BossSystem>();
            if (bosses == null) bosses = game.gameObject.AddComponent<BossSystem>();
            bosses.ResetRun();
            bosses.Tick(36000);
            Assert.IsNull(bosses.CurrentBoss);
            Assert.AreEqual(0, game.Spawner.Active.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BatContactIsDelayedUntilVisibleSwingContact()
        {
            profile.Normal.Health = 500;
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2);
            float before = enemy.Health;
            game.Player.GetComponent<CombatSystem>().TryAttack();
            Assert.AreEqual(before, enemy.Health, "Урон не должен приходить до взмаха.");
            yield return new WaitForSeconds(.15f);
            Assert.Less(enemy.Health, before);
            Assert.IsTrue(enemy.Alive);
        }

        [UnityTest]
        public IEnumerator BatImpulseProducesNoticeableButBoundedDisplacement()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 24);
            Vector3 before = enemy.transform.position;
            enemy.TakeDamage(1, Vector3.forward * 9);
            enemy.Tick(.3f);
            float displacement = Vector3.Dot(enemy.transform.position - before, Vector3.forward);
            Assert.Greater(displacement, .7f);
            Assert.Less(displacement, 3);
            Assert.IsFalse(enemy.IsAlerted);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KnockbackAndWanderingCannotLeaveHomeZoneOrLand()
        {
            Vector3 boundary = IslandMap.ClampToZone(IslandMap.Center(IslandZone.Meadows), IslandZone.Beach);
            var enemy = game.Spawner.Spawn(EnemyKind.Normal, boundary);
            Assert.IsNotNull(enemy);
            enemy.TakeDamage(1, Vector3.forward * 10000);
            for (int i = 0; i < 40; i++)
            {
                enemy.Tick(.2f);
                Assert.AreEqual(IslandZone.Beach, IslandMap.GetZone(enemy.transform.position));
                Assert.IsTrue(IslandMap.IsWalkable(enemy.transform.position));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitTintIsPerEnemyAndClearsWhenPooled()
        {
            var enemy = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2);
            var other = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 4);
            var renderer = enemy.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>();
            Color original = renderer.sharedMaterial.color;
            enemy.TakeDamage(1, Vector3.back * 4);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.IsFalse(block.isEmpty);
            Assert.AreEqual(original, renderer.sharedMaterial.color);
            other.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.IsTrue(block.isEmpty);
            game.Spawner.ResetRun();
            var reused = SpawnNearPlayer(EnemyKind.Normal, Vector3.forward * 2);
            reused.GetComponentInChildren<CharacterVisual>().GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.IsTrue(block.isEmpty);
            Assert.AreEqual(EnemyAction.Idle, reused.Action);
            Assert.IsFalse(reused.IsAlerted);
            yield return null;
        }

        EnemyController SpawnNearPlayer(EnemyKind kind, Vector3 offset)
        {
            var enemy = game.Spawner.Spawn(kind, game.Player.transform.position + offset);
            Assert.IsNotNull(enemy, "Не удалось создать " + kind + " в текущей зоне.");
            return enemy;
        }
        void TeleportPlayer(Vector3 position)
        {
            var motor = game.Player.GetComponent<CharacterController>();
            motor.enabled = false;
            position.y = IslandMap.StartPosition.y;
            game.Player.transform.position = position;
            motor.enabled = true;
        }

        [UnityTest]
        public IEnumerator OpeningMapCancelsPendingInvisibleContact()
        {
            var enemy=SpawnNearPlayer(EnemyKind.Normal,Vector3.forward*2);
            float before=enemy.Health;
            game.Player.GetComponent<CombatSystem>().TryAttack();
            game.OpenMap();game.CloseMap();
            yield return new WaitForSeconds(.18f);
            Assert.AreEqual(before,enemy.Health,"Отменённый взмах не должен наносить урон после карты.");
        }

        [UnityTest]
        public IEnumerator CustomCharacterStatsAndDashTuningAreUsed()
        {
            character.UseCustomStats=true;character.StartingStats.Damage=57;
            character.StartingStats.DashDuration=.5f;character.StartingStats.DashSpeed=11;
            game.Player.ResetRun();Assert.AreEqual(57,game.Player.Stats.Damage);
            Assert.AreEqual(11,game.Player.ActiveTuning.DashSpeed);
            Assert.IsTrue(game.Player.TryDash(Vector3.forward));
            yield return new WaitForSeconds(.22f);Assert.IsTrue(game.Player.IsDashing);
        }

        [Test]
        public void ZoneCentersHaveDistinctSpeciesAndAreWalkable()
        {
            var species = new System.Collections.Generic.HashSet<EnemyKind>();
            for (int i = 0; i < 5; i++)
            {
                var zone = (IslandZone)i;
                Vector3 center = IslandMap.Center(zone);
                Assert.IsTrue(IslandMap.IsWalkable(center));
                Assert.AreEqual(zone, IslandMap.GetZone(center));
                Assert.IsTrue(species.Add(IslandMap.EnemyForZone(zone)));
            }
            Assert.AreEqual(IslandZone.Beach, IslandMap.GetZone(IslandMap.StartPosition));
        }

        [Test]
        public void LandAndZoneClampsKeepPositionsInsidePlayableBoundaries()
        {
            Vector3 outside = new Vector3(400, .15f, -500);
            Vector3 land = IslandMap.ClampToLand(outside);
            Assert.IsTrue(IslandMap.IsWalkable(land, 1.99f));
            for (int i = 0; i < 5; i++)
            {
                var zone = (IslandZone)i;
                Vector3 bounded = IslandMap.ClampToZone(outside, zone);
                Assert.AreEqual(zone, IslandMap.GetZone(bounded));
                Assert.IsTrue(IslandMap.IsWalkable(bounded, 1.99f));
            }
        }

        [Test]
        public void MeleeSectorMatchesFrontReachAndRejectsBehind()
        {
            Assert.IsTrue(CombatSystem.InMeleeSector(Vector3.zero, Vector3.forward, Vector3.forward * 3, 3.2f, 130, .8f));
            Assert.IsFalse(CombatSystem.InMeleeSector(Vector3.zero, Vector3.forward, Vector3.back * 2, 3.2f, 130, .8f));
            Assert.IsFalse(CombatSystem.InMeleeSector(Vector3.zero, Vector3.forward, Vector3.forward * 3.3f, 3.2f, 130, .8f));
            Assert.IsTrue(CombatSystem.InMeleeSector(Vector3.zero, Vector3.forward, Vector3.back * .5f, 3.2f, 130, .8f));
        }

        [Test]
        public void ChargeSweptHitCatchesCrossingButRejectsSideStep()
        {
            Assert.IsTrue(EnemyController.SweptHitXZ(Vector3.back * 5, Vector3.forward * 5, Vector3.zero, 1));
            Assert.IsFalse(EnemyController.SweptHitXZ(Vector3.back * 5, Vector3.forward * 5, Vector3.right * 2, 1));
        }
    }
}

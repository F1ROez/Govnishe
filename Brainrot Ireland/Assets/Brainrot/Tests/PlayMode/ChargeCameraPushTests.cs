using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class ChargeCameraPushTests
    {
        GameManager game;
        GameBalance profile;
        PlayerCharacterDefinition character;
        Camera camera;
        Scene scene,previous;

        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.IsNull(GameManager.Instance,"Запускайте тесты из режима редактирования.");
            previous=SceneManager.GetActiveScene();
            scene=SceneManager.CreateScene("Камера и толчок "+System.Guid.NewGuid());
            SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();
            profile=ScriptableObject.CreateInstance<GameBalance>();
            profile.Waves.InitialDelay=600;
            profile.Camera.Distance=5;
            profile.Camera.ChargeZoomDistance=1;
            profile.Camera.ChargeZoomInSpeed=3;
            profile.Camera.ChargeZoomOutSpeed=14;
            profile.Combat.HeavyChargeSeconds=.4f;
            game.Balance=profile;
            character=ScriptableObject.CreateInstance<PlayerCharacterDefinition>();
            game.Player.Character=character;
            MobileInput.Clear();
            yield return null;
            game.StartRun();
            yield return null; // Release the input suppression installed by ResetRun.
            foreach(var candidate in Camera.allCameras)if(candidate.name=="Arena Camera")camera=candidate;
            Assert.IsNotNull(camera);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            MobileInput.Clear();
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);
            if(character!=null)Object.Destroy(character);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            yield return null;
        }

        float BoomDistance()
        {
            float scale=Mathf.Max(.1f,character.VisualScale)*Mathf.Max(.1f,profile.PlayerModelScale);
            Vector3 target=game.Player.transform.position+Vector3.up*(1.9f*scale+Mathf.Max(0,profile.Camera.HeightAboveHead));
            // The shoulder offset is orthogonal to camera.forward, so it does not affect this measurement.
            return -Vector3.Dot(camera.transform.position-target,camera.transform.forward);
        }

        [UnityTest] public IEnumerator ChargingPullsCameraGraduallyAndReleaseReturnsItQuickly()
        {
            float baseline=BoomDistance();
            MobileInput.Attack=true;
            yield return new WaitForSeconds(profile.Combat.HeavyHoldDelay+.18f);
            Assert.IsTrue(game.Player.Combat.IsCharging);
            float earlyPull=baseline-BoomDistance();
            Assert.That(earlyPull,Is.InRange(.005f,.45f),"Камера должна приближаться постепенно, пока идёт замах.");
            yield return new WaitForSeconds(.8f);
            float fullPull=baseline-BoomDistance();
            Assert.Greater(fullPull,.55f);
            Assert.Less(fullPull,profile.Camera.ChargeZoomDistance+.01f);
            MobileInput.Attack=false;
            yield return new WaitForSeconds(.04f);
            Assert.IsFalse(game.Player.Combat.IsCharging);
            Assert.Greater(game.Player.Combat.HeavyCooldownRemaining,0,"Отпускание после заряда должно выполнить сильный удар.");
            float returningPull=baseline-BoomDistance();
            Assert.Less(returningPull,fullPull);
            yield return new WaitForSeconds(.28f);
            Assert.Less(Mathf.Abs(baseline-BoomDistance()),fullPull*.1f+.02f,"После удара камера быстро возвращается на плечо.");
        }

        [UnityTest] public IEnumerator PauseAndMenuCancelChargeAndResetCameraDistance()
        {
            MobileInput.Attack=true;
            yield return new WaitForSeconds(.6f);
            Assert.Greater(profile.Camera.Distance-BoomDistance(),.2f);
            game.Pause();
            yield return null;
            Assert.IsFalse(game.Player.Combat.IsCharging);
            Assert.That(BoomDistance(),Is.EqualTo(profile.Camera.Distance).Within(.02f));
            game.Resume();
            yield return null;
            MobileInput.Attack=true;
            yield return new WaitForSeconds(.6f);
            Assert.IsTrue(game.Player.Combat.IsCharging);
            game.SuspendRun();
            yield return null;
            Assert.IsFalse(game.Player.Combat.IsCharging);
            Assert.That(BoomDistance(),Is.EqualTo(profile.Camera.Distance).Within(.02f));
        }

        [UnityTest] public IEnumerator AcceptedHitPushesPlayerButInvulnerableRepeatDoesNotReplaceTheShove()
        {
            Vector3 start=game.Player.transform.position;
            game.Player.TakeDamage(30,start-Vector3.right*2,12);
            game.Player.TakeDamage(30,start+Vector3.right*2,40);
            Assert.That(game.Player.Health,Is.EqualTo(70).Within(.01f));
            yield return new WaitForSeconds(.3f);
            Assert.Greater(game.Player.transform.position.x-start.x,.8f,"Второе попадание во время неуязвимости не должно развернуть толчок.");
            Assert.That(game.Player.Health,Is.EqualTo(70).Within(.01f));
        }

        [UnityTest] public IEnumerator NewRunClearsPreviousKnockbackVelocity()
        {
            Vector3 initial=game.Player.transform.position;
            game.Player.TakeDamage(30,initial-Vector3.right*2,17);
            game.StartRun();
            Vector3 restarted=game.Player.transform.position;
            yield return new WaitForSeconds(.2f);
            Vector3 offset=game.Player.transform.position-restarted;offset.y=0;
            Assert.Less(offset.magnitude,.05f,"Новый забег не должен продолжать отбрасывание предыдущего.");
            Assert.That(game.Player.Health,Is.EqualTo(game.Player.Stats.MaxHealth).Within(.01f));
        }
    }
}

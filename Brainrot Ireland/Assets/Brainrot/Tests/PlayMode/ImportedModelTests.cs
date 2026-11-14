using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class ImportedModelTests {
  GameObject actor;
  [UnityTearDown]public IEnumerator Cleanup(){if(actor!=null)Object.Destroy(actor);yield return null;}
  [UnityTest]public IEnumerator ImportedRigHasStableLocomotionScaleAndExistingBat(){
   var prefab=Resources.Load<GameObject>("ImportedSahur");if(prefab==null)Assert.Ignore("Сначала выполните Создать или обновить игру, чтобы импортировать FBX.");
   actor=Object.Instantiate(prefab);var bridge=actor.GetComponent<ImportedSahurVisual>();Assert.IsNotNull(bridge);Assert.IsNotNull(bridge.Animator.runtimeAnimatorController);Assert.IsNotNull(bridge.Weapon);Assert.IsNotNull(bridge.WeaponTip);Assert.IsNotNull(actor.GetComponentInChildren<SkinnedMeshRenderer>());
   yield return null; // Let the actual runtime Animator and model calibration evaluate their first frame.
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();Assert.IsTrue(skin.enabled);Assert.IsTrue(skin.updateWhenOffscreen);
   var material=skin.sharedMaterial;
   foreach(string slot in new[]{"_MainTex","_BaseMap","_BumpMap","_MetallicGlossMap"}){
    Assert.IsTrue(material.HasProperty(slot),"Отсутствует слот материала: "+slot);
    var texture=material.GetTexture(slot);Assert.IsNotNull(texture,"Карта не загрузилась: "+slot);
    Assert.IsInstanceOf<Texture2D>(texture,"Для материала Сахура требуется 2D-карта: "+slot);
   }
   foreach(float speed in new[]{0f,3f,4.2f}){bridge.Animate(speed,false);bridge.Animator.Update(.35f);bridge.Animator.Update(.35f);var bounds=GeometryBounds(actor);float height=bounds.size.y;
    Assert.Greater(bounds.min.y,-.35f,"Модель не должна оказаться под островом.");Assert.Less(bounds.min.y,.35f,"Стопы должны оставаться у поверхности острова.");
    Assert.Less(new Vector2(bounds.center.x,bounds.center.z).magnitude,1.5f,"Сетка должна оставаться около игрока.");Assert.Greater(height,1.2f,"Смена клипа не должна вернуть bind-scale .229.");Assert.Less(height,2.6f,"Idle не должен увеличивать персонажа.");}
   bridge.PlayAttack(.32f);Assert.IsTrue(bridge.AttackActive);bridge.ResetAttack();Assert.IsFalse(bridge.AttackActive);Assert.IsFalse(bridge.Trail.emitting);yield return null;
  }
  [UnityTest]public IEnumerator ImportedRigCalibrationUsesActorLocalSpace(){
   var prefab=Resources.Load<GameObject>("ImportedSahur");if(prefab==null)Assert.Ignore("Сначала выполните Создать или обновить игру.");
   actor=Object.Instantiate(prefab,new Vector3(23,2,-19),Quaternion.Euler(0,70,0));yield return null;
   var bridge=actor.GetComponent<ImportedSahurVisual>();
   bridge.Normalization.localScale=Vector3.one*.00009492202f; // Scale recorded in owner's invisible rig report.
   bridge.Normalization.localPosition=new Vector3(0,.01f,.003f);
   Assert.IsTrue(bridge.CalibrateModel());var firstScale=bridge.Normalization.localScale;
   Assert.IsTrue(bridge.CalibrateModel());Assert.Less((bridge.Normalization.localScale-firstScale).magnitude,.00001f,"Повторная подгонка не должна уменьшать модель.");
   var bounds=GeometryBounds(actor);Assert.That(bounds.size.y,Is.InRange(1.4f,2.6f));Assert.That(bounds.min.y,Is.InRange(-.35f,.35f));
   Assert.Less(new Vector2(bounds.center.x,bounds.center.z).magnitude,1.5f);
  }
  static Bounds GeometryBounds(GameObject root){
   bool any=false;Bounds bounds=new Bounds();
   foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var world=skin.bounds;
    for(int corner=0;corner<8;corner++){
     var vertex=world.center+Vector3.Scale(world.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
     var point=root.transform.InverseTransformPoint(vertex);if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
    }
   }
   Assert.IsTrue(any,"Импортированная модель должна иметь границы отображения.");return bounds;
  }
 }
}

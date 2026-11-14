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
   var material=actor.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
   foreach(string slot in new[]{"_MainTex","_BaseMap","_BumpMap","_MetallicGlossMap"}){
    Assert.IsTrue(material.HasProperty(slot),"Отсутствует слот материала: "+slot);
    var texture=material.GetTexture(slot);Assert.IsNotNull(texture,"Карта не загрузилась: "+slot);
    Assert.IsInstanceOf<Texture2D>(texture,"Для материала Сахура требуется 2D-карта: "+slot);
   }
   foreach(float speed in new[]{0f,3f,4.2f}){bridge.Animate(speed,false);bridge.Animator.Update(.35f);bridge.Animator.Update(.35f);float height=Height(actor);Assert.Greater(height,1.2f,"Смена клипа не должна вернуть bind-scale .229.");Assert.Less(height,2.6f,"Idle не должен увеличивать персонажа.");}
   bridge.PlayAttack(.32f);Assert.IsTrue(bridge.AttackActive);bridge.ResetAttack();Assert.IsFalse(bridge.AttackActive);Assert.IsFalse(bridge.Trail.emitting);yield return null;
  }
  static float Height(GameObject root){float min=float.PositiveInfinity,max=float.NegativeInfinity;foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var vertex in mesh.vertices){float y=skin.transform.TransformPoint(vertex).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}Object.Destroy(mesh);}return max-min;}
 }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class ObjectPoolSafetyTests {
  [UnityTest]public IEnumerator DestroyedAvailableObjectIsReplaced(){
   var pool=new ObjectPool<Transform>(()=>new GameObject("Pool safety").transform);
   var first=pool.Get();pool.Release(first);Object.Destroy(first.gameObject);yield return null;
   var next=pool.Get();Assert.IsNotNull(next);Assert.IsTrue(next.gameObject.activeSelf);Object.Destroy(next.gameObject);yield return null;
  }
  [UnityTest]public IEnumerator ReleasingTwiceCannotLoanTheSameObjectTwice(){
   var pool=new ObjectPool<Transform>(()=>new GameObject("Pool safety").transform);
   var original=pool.Get();pool.Release(original);pool.Release(original);
   var a=pool.Get();var b=pool.Get();Assert.AreNotSame(a,b);Assert.IsTrue(a.gameObject.activeSelf);
   Object.Destroy(a.gameObject);Object.Destroy(b.gameObject);yield return null;
  }
 }
}

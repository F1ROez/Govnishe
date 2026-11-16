using NUnit.Framework;
using UnityEngine;

namespace Brainrot.Tests
{
    public sealed class CameraCollisionTests
    {
        GameObject owner,wall;
        CameraCollision collision;
        [SetUp]public void Setup(){owner=new GameObject("Camera owner");collision=new CameraCollision(owner.transform);}
        [TearDown]public void Cleanup(){collision.Dispose();if(wall!=null)Object.DestroyImmediate(wall);Object.DestroyImmediate(owner);}
        GameObject Box(Vector3 position,Vector3 size){var go=new GameObject("Obstacle",typeof(BoxCollider));go.transform.position=position;go.GetComponent<BoxCollider>().size=size;return go;}
        Vector3 Resolve(float dt=.016f)=>collision.Resolve(new Vector3(0,2,0),new Vector3(0,2,-5),.25f,.08f,7,~0,dt);
        [Test]public void EnvironmentObstructionPullsInImmediatelyAndClearingReturnsSmoothly(){
            Assert.AreEqual(-5,Resolve().z,.001f);
            wall=Box(new Vector3(0,2,-2),new Vector3(4,4,.5f));Physics.SyncTransforms();
            Vector3 close=Resolve();Assert.Greater(close.z,-1.5f);Assert.Less(close.z,0);
            wall.SetActive(false);Physics.SyncTransforms();
            Vector3 returning=Resolve();Assert.Less(returning.z,close.z);Assert.Greater(returning.z,-5);
            for(int i=0;i<100;i++)returning=Resolve(.033f);
            Assert.AreEqual(-5,returning.z,.001f);
        }
        [Test]public void TriggerAndOwnedCollidersDoNotCollapseShoulderCamera(){
            wall=Box(new Vector3(0,2,-2),new Vector3(4,4,.5f));wall.GetComponent<BoxCollider>().isTrigger=true;
            var playerCollider=owner.AddComponent<BoxCollider>();playerCollider.center=new Vector3(0,2,-1);playerCollider.size=new Vector3(3,3,3);
            Physics.SyncTransforms();Assert.AreEqual(-5,Resolve().z,.001f);
        }
        [Test]public void StartingInsideEnvironmentResolvesSphereOutOfObstacle(){
            wall=Box(new Vector3(0,2,0),new Vector3(.5f,4,1));Physics.SyncTransforms();
            var camera=Resolve();Assert.IsFalse(float.IsNaN(camera.x));
            var overlaps=Physics.OverlapSphere(camera,.24f,~0,QueryTriggerInteraction.Ignore);
            CollectionAssert.DoesNotContain(overlaps,wall.GetComponent<BoxCollider>(),"Camera near plane must not remain inside the wall.");
        }
        [Test]public void MovingObstacleCannotTrapPreviouslyClearCamera(){
            Resolve();wall=Box(new Vector3(0,2,-5),new Vector3(2,3,1));Physics.SyncTransforms();
            var camera=Resolve();Assert.Greater(camera.z,-4.2f);
            var overlaps=Physics.OverlapSphere(camera,.24f,~0,QueryTriggerInteraction.Ignore);
            CollectionAssert.DoesNotContain(overlaps,wall.GetComponent<BoxCollider>());
        }
    }
}

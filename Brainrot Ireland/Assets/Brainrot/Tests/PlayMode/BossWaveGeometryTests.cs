using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class BossWaveGeometryTests
    {
        [Test]public void OppositeOutsideEndpointsStillCrossTheDangerousAnnulus(){
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(Vector3.left*10,Vector3.right*10,Vector3.zero,5,5,.26f));
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(new Vector3(-10,30,0),new Vector3(10,-20,0),Vector3.up*99,3,7,.26f));
        }
        [Test]public void RingHoleIsSafeAndAPathBeyondTheOuterEdgeMisses(){
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(Vector3.left*2,Vector3.right*2,Vector3.zero,5,6,.26f));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(new Vector3(-10,0,5.5f),new Vector3(10,0,5.5f),Vector3.zero,5,5,.26f));
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(new Vector3(-10,0,5.25f),new Vector3(10,0,5.25f),Vector3.zero,5,5,.26f));
        }
        [Test]public void ExpandingRingCatchesStationaryPlayerButNeverFillsItsHole(){
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(Vector3.right*4,Vector3.right*4,Vector3.zero,1,8,.26f));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(Vector3.right,Vector3.right,Vector3.zero,3,8,.26f));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(Vector3.right*3,Vector3.right*5,Vector3.zero,2,4,.26f));
        }
        [Test]public void ZeroMovementBoundaryTangencyAndInvalidInputStayDefined(){
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(Vector3.right*5,Vector3.right*5,Vector3.zero,5,5,0));
            Assert.IsTrue(BossWaveGeometry.SweptRingHit(new Vector3(-10,0,5),new Vector3(10,0,5),Vector3.zero,5,5,0));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(Vector3.one,Vector3.one,Vector3.zero,0,0,0));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(new Vector3(float.NaN,0,0),Vector3.zero,Vector3.zero,1,2,.2f));
            Assert.IsFalse(BossWaveGeometry.SweptRingHit(Vector3.zero,Vector3.one,Vector3.zero,1,float.PositiveInfinity,.2f));
        }
    }
}

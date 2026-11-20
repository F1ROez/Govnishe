using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class CrowdSeparationTests
    {
        readonly List<GameObject> objects=new List<GameObject>();
        EnemyController Enemy(Vector3 position){
            var go=new GameObject("Crowd test");objects.Add(go);go.transform.position=position;
            var enemy=go.AddComponent<EnemyController>();typeof(EnemyController).GetProperty("Alive").SetValue(enemy,true);return enemy;
        }
        [TearDown]public void Cleanup(){foreach(var go in objects)if(go!=null)Object.DestroyImmediate(go);objects.Clear();}
        [Test]public void CoincidentPairReceivesOppositeFiniteNormals(){
            var a=CrowdSeparation.DiskCorrection(Vector2.zero,Vector2.zero,1.5f,7,9);
            var b=CrowdSeparation.DiskCorrection(Vector2.zero,Vector2.zero,1.5f,9,7);
            Assert.AreEqual(1.5f,a.magnitude,.001f);Assert.Less((a+b).sqrMagnitude,.000001f);
            Assert.AreEqual(Vector2.zero,CrowdSeparation.DiskCorrection(Vector2.zero,new Vector2(4,0),1.5f,7,9));
        }
        [Test]public void DenseCrowdWorkAndSpeedStayBounded(){
            var enemies=new List<EnemyController>();for(int i=0;i<100;i++)enemies.Add(Enemy(Vector3.zero));
            var tuning=new CrowdTuning{Iterations=2,MaxNeighbours=16,MaxCorrectionSpeed=4.5f};var solver=new CrowdSeparation();
            solver.Resolve(enemies,new Vector3(80,0,80),.48f,1/60f,tuning);
            Assert.LessOrEqual(solver.LastNeighbourChecks,100*16*2);
            bool moved=false;
            foreach(var enemy in enemies){var position=enemy.transform.position;Assert.IsFalse(float.IsNaN(position.x)||float.IsNaN(position.z));Assert.LessOrEqual(position.magnitude,4.5f/60+.0001f);moved|=position.sqrMagnitude>.000001f;}
            Assert.IsTrue(moved,"Even coincident centres must begin separating.");
        }
        [Test]public void HashAliasesCannotUseUpTheActualNeighbourBudget(){
            var first=Enemy(Vector3.zero);var near=Enemy(Vector3.right*.2f);
            var enemies=new List<EnemyController>{first,near};var tuning=new CrowdTuning{Iterations=1,MaxNeighbours=4};
            float cell=first.CrowdRadius*2+tuning.Padding;
            // Coordinates differing by 256 hash into the same bucket, but are nowhere near this pair.
            for(int i=1;i<=12;i++)enemies.Add(Enemy(new Vector3(0,0,cell*256*i+.1f)));
            var solver=new CrowdSeparation();solver.Resolve(enemies,new Vector3(80,0,80),.48f,1/60f,tuning);
            Assert.Less(first.transform.position.x,-.01f,"A real overlap must still be resolved behind unrelated hash entries.");
            Assert.Greater(near.transform.position.x,.21f);
            Assert.LessOrEqual(solver.LastNeighbourChecks,enemies.Count*tuning.MaxNeighbours);
        }
        [Test]public void CommittedChargeAndHeadAttachedOctopusAreNotMoved(){
            var charge=Enemy(Vector3.zero);typeof(EnemyController).GetProperty("Action").SetValue(charge,EnemyAction.Charging);
            var attached=Enemy(Vector3.up*1.6f);var attack=attached.gameObject.AddComponent<OctopusAttack>();
            typeof(OctopusAttack).GetProperty("Phase").SetValue(attack,OctopusPhase.Attached);
            typeof(EnemyController).GetField("octopus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(attached,attack);
            var solver=new CrowdSeparation();solver.Resolve(new[]{charge,attached},Vector3.zero,.48f,.1f,new CrowdTuning());
            Assert.AreEqual(Vector3.zero,charge.transform.position);Assert.AreEqual(Vector3.up*1.6f,attached.transform.position);
        }
        [Test]public void SeparationClearsSpaceAroundPlayerWithoutMovingPlayer(){
            var enemy=Enemy(Vector3.zero);var player=new Vector3(.1f,0,0);var solver=new CrowdSeparation();
            for(int i=0;i<60;i++)solver.Resolve(new[]{enemy},player,.48f,1/60f,new CrowdTuning());
            Assert.Greater((enemy.transform.position-player).magnitude,1.2f);
            Assert.AreEqual(new Vector3(.1f,0,0),player);
        }
    }
}

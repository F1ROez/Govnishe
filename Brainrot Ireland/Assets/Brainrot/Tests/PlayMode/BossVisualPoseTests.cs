using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class BossVisualPoseTests
    {
        static Transform Node(Transform parent,string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        [Test]public void ArticulatedAttackChangesArmsWithoutMovingActorAndDoesNotAccumulate(){
            var actor=new GameObject("Inactive boss pose preview");actor.SetActive(false);
            try{
                var enemy=actor.AddComponent<EnemyController>();var avatar=Node(actor.transform,"Avatar",Vector3.zero);
                var bridge=avatar.gameObject.AddComponent<ImportedEnemyVisual>();bridge.IsOrcaladon=true;
                bridge.MotionRoot=Node(avatar,"Motion",Vector3.zero);bridge.ModelRoot=Node(bridge.MotionRoot,"Model",Vector3.zero);
                var orca=avatar.gameObject.AddComponent<OrcaladonVisual>();bridge.Orcaladon=orca;orca.NeutralArmsDown=true;orca.ModelRoot=bridge.ModelRoot;orca.MotionRoot=bridge.MotionRoot;
                orca.Body=Node(bridge.ModelRoot,"Body",Vector3.up);orca.UpperLeft=Node(orca.Body,"UpperLeft",Vector3.left);orca.UpperRight=Node(orca.Body,"UpperRight",Vector3.right);
                orca.ForeLeft=Node(orca.UpperLeft,"ForeLeft",Vector3.down);orca.ForeRight=Node(orca.UpperRight,"ForeRight",Vector3.down);
                orca.ThighLeft=Node(orca.Body,"ThighLeft",Vector3.left*.4f);orca.ThighRight=Node(orca.Body,"ThighRight",Vector3.right*.4f);
                var driver=actor.AddComponent<BossVisualDriver>();driver.Initialize(enemy);
                var actorPosition=actor.transform.position;var actorRotation=actor.transform.rotation;var planted=orca.ThighLeft.position;
                driver.Sample(BossPose.RightPunch,.95f);
                Assert.Greater(Quaternion.Angle(orca.UpperLeft.localRotation,orca.UpperRight.localRotation),30,"A punch must articulate the selected arm.");
                var pose=orca.UpperRight.localRotation;driver.Sample(BossPose.RightPunch,.95f);
                Assert.Less(Quaternion.Angle(pose,orca.UpperRight.localRotation),.001f,"Sampling the same pose must never accumulate rotation.");
                Assert.Less(Vector3.Distance(planted,orca.ThighLeft.position),.001f,"Torso lean must preserve the planted legs.");
                Assert.AreEqual(actorPosition,actor.transform.position);Assert.AreEqual(actorRotation,actor.transform.rotation);
                driver.ResetPose();Assert.Less(Quaternion.Angle(Quaternion.identity,orca.UpperRight.localRotation),.001f);
                Assert.AreEqual(Vector3.one,actor.transform.localScale);
            }finally{Object.DestroyImmediate(actor);}
        }
        [Test]public void StaticToyOctopusReceivesRealSkinBonesAndRestoresItsOriginalRenderer(){
            var actor=new GameObject("Static octopus rig preview");actor.SetActive(false);Mesh source=null;
            try{
                var enemy=actor.AddComponent<EnemyController>();var avatar=Node(actor.transform,"Avatar",Vector3.zero);
                var bridge=avatar.gameObject.AddComponent<ImportedEnemyVisual>();bridge.IsOctopus=true;
                bridge.MotionRoot=Node(avatar,"Motion",Vector3.zero);bridge.ModelRoot=Node(bridge.MotionRoot,"ToyModel",Vector3.zero);
                var model=new GameObject("ToyMesh",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(bridge.ModelRoot,false);
                source=new Mesh();source.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1),new Vector3(0,2,0)};source.triangles=new[]{0,1,4,1,2,4,2,3,4,3,0,4};source.RecalculateNormals();source.RecalculateBounds();model.GetComponent<MeshFilter>().sharedMesh=source;
                var renderer=model.GetComponent<MeshRenderer>();var driver=actor.AddComponent<BossVisualDriver>();driver.Initialize(enemy);
                Assert.GreaterOrEqual(driver.DrivenBoneCount,17);Assert.IsFalse(renderer.enabled);
                var skin=avatar.GetComponentInChildren<SkinnedMeshRenderer>(true);Assert.IsNotNull(skin);Assert.AreEqual(17,skin.bones.Length);Assert.AreEqual(source.vertexCount,skin.sharedMesh.boneWeights.Length);
                var baseline=skin.bones[2].localRotation;driver.Sample(BossPose.Grab,1);
                Assert.Greater(Quaternion.Angle(baseline,skin.bones[2].localRotation),10,"The toy must animate its skin, not just rotate the whole model.");
                driver.ResetPose();Assert.IsTrue(renderer.enabled);Assert.IsFalse(skin.enabled);Assert.Less(Quaternion.Angle(baseline,skin.bones[2].localRotation),.001f);
            }finally{Object.DestroyImmediate(actor);if(source!=null)Object.DestroyImmediate(source);}
        }
    }
}

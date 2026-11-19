using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    /// <summary>Pose continuity and grounding invariants without depending on a generated asset or a gameplay scene.</summary>
    public sealed class OrcaladonPosePolishTests
    {
        GameObject actor;OrcaladonVisual pose;Transform motion;Vector3 actorPosition;Quaternion actorRotation;
        Transform Joint(string name,Transform parent,Vector3 position){var value=new GameObject(name).transform;value.SetParent(parent,false);value.localPosition=position;return value;}
        [SetUp]public void Build(){
            actor=new GameObject("Orca pose preview");actor.SetActive(false);actor.transform.SetPositionAndRotation(new Vector3(3,0,-5),Quaternion.Euler(0,47,0));
            motion=Joint("Motion",actor.transform,new Vector3(0,.08f,0));var model=Joint("Model",motion,Vector3.zero);
            pose=actor.AddComponent<OrcaladonVisual>();pose.ModelRoot=model;pose.MotionRoot=motion;pose.NeutralArmsDown=true;
            pose.Body=Joint("Body",model,new Vector3(0,1,0));
            pose.UpperLeft=Joint("Upper L",pose.Body,new Vector3(.72f,.28f,.45f));pose.UpperRight=Joint("Upper R",pose.Body,new Vector3(-.72f,.28f,.45f));
            pose.ForeLeft=Joint("Fore L",pose.UpperLeft,new Vector3(.19f,-.64f,.25f));pose.ForeRight=Joint("Fore R",pose.UpperRight,new Vector3(-.19f,-.64f,.25f));
            pose.ThighLeft=Joint("Thigh L",pose.Body,new Vector3(.65f,-.15f,-.65f));pose.ThighRight=Joint("Thigh R",pose.Body,new Vector3(-.65f,-.15f,-.65f));
            pose.ShinLeft=Joint("Shin L",pose.ThighLeft,new Vector3(.05f,-.49f,0));pose.ShinRight=Joint("Shin R",pose.ThighRight,new Vector3(-.05f,-.49f,0));
            actorPosition=actor.transform.position;actorRotation=actor.transform.rotation;actor.SetActive(true);pose.ResetPose();
        }
        [TearDown]public void Cleanup(){Object.DestroyImmediate(actor);}
        [Test]public void ImpactAndRecoveryCannotSinkMotionRootOrMovePhysicsActor(){
            Vector3 baseline=motion.localPosition;var rear=pose.ThighLeft.position;
            for(int i=0;i<60;i++)pose.Pose(1/60f,2.2f,OrcaladonPhase.Approach,0);
            for(int i=0;i<=60;i++)pose.Pose(1/60f,0,OrcaladonPhase.SlamWindup,i/60f);
            for(int i=0;i<=12;i++){
                pose.Pose(1/60f,0,OrcaladonPhase.SlamImpact,i/12f);
                Assert.AreEqual(baseline,motion.localPosition,"Slam compression must not drag the whole rig below ground.");
                Assert.AreEqual(actorPosition,actor.transform.position);Assert.AreEqual(actorRotation,actor.transform.rotation);
            }
            for(int i=0;i<120;i++)pose.Pose(1/60f,0,OrcaladonPhase.Recovery,1);
            Assert.AreEqual(baseline,motion.localPosition);
            pose.ResetPose();Assert.Less(Vector3.Distance(rear,pose.ThighLeft.position),.001f,"Reset after recovery must not retain accumulated compression.");
        }
        [Test]public void EndingMovementBlendsLegsInsteadOfSnappingToBindPose(){
            for(int i=0;i<15;i++)pose.Pose(1/60f,2.2f,OrcaladonPhase.Approach,0);
            Quaternion before=pose.ThighLeft.localRotation;
            Assert.Greater(Quaternion.Angle(Quaternion.identity,before),1);
            pose.Pose(1/60f,0,OrcaladonPhase.ChargeAim,0);
            Assert.Less(Quaternion.Angle(before,pose.ThighLeft.localRotation),Quaternion.Angle(before,Quaternion.identity)*.5f);
            for(int i=0;i<120;i++)pose.Pose(1/60f,0,OrcaladonPhase.ChargeAim,.5f);
            Assert.Less(Quaternion.Angle(Quaternion.identity,pose.ThighLeft.localRotation),.001f);
        }
        [Test]public void PausedSamplingKeepsPoseAndResetRestoresBonesWithoutScaleDrift(){
            for(int i=0;i<35;i++)pose.Pose(1/60f,2.2f,OrcaladonPhase.Approach,0);
            var rotation=pose.Body.localRotation;var position=pose.Body.localPosition;
            pose.Pose(0,0,OrcaladonPhase.SlamImpact,1);
            Assert.AreEqual(rotation,pose.Body.localRotation);Assert.AreEqual(position,pose.Body.localPosition);
            pose.ResetPose();var baseline=pose.Body.localPosition;var scale=pose.ForeLeft.localScale;
            for(int i=0;i<120;i++){pose.Pose(1/60f,0,OrcaladonPhase.Approach,0);Assert.AreEqual(scale,pose.ForeLeft.localScale);}
            Assert.Greater(Vector3.Distance(baseline,pose.Body.localPosition),.0001f,"Waiting should retain subtle torso breathing.");
            pose.ResetPose();Assert.AreEqual(baseline,pose.Body.localPosition);Assert.AreEqual(Quaternion.identity,pose.UpperLeft.localRotation);
        }
    }
}

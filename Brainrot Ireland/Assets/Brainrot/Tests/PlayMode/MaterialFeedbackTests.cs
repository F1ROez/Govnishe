using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class MaterialFeedbackTests
    {
        GameObject actor;
        Mesh mesh;
        Material first,second;
        [TearDown] public void Cleanup(){
            if(actor!=null)Object.DestroyImmediate(actor);
            if(mesh!=null)Object.DestroyImmediate(mesh);
            if(first!=null)Object.DestroyImmediate(first);
            if(second!=null)Object.DestroyImmediate(second);
        }
        [Test] public void ToonFlashUsesBaseColorPerSlotAndLeavesSharedMaterialsUnchanged(){
            var shader=Shader.Find("Brainrot/ToonCharacter");Assert.IsNotNull(shader);
            first=new Material(shader);second=new Material(shader);
            var blue=new Color(.12f,.45f,.83f,1);var cream=new Color(.98f,.91f,.72f,1);
            first.SetColor("_BaseColor",blue);second.SetColor("_BaseColor",cream);
            // MainColor must make Material.color work for other Unity consumers too.
            Assert.IsFalse(first.HasProperty("_Color"));Assert.AreEqual(blue,first.color);
            actor=new GameObject("Material regression");var visual=actor.AddComponent<CharacterVisual>();
            var go=new GameObject("Two material mesh",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(actor.transform,false);
            mesh=new Mesh{vertices=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.one},subMeshCount=2};
            mesh.SetTriangles(new[]{0,1,2},0);mesh.SetTriangles(new[]{2,1,3},1);mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{first,second};
            visual.FlashHit(.2f,.3f);
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,0);
            Assert.AreEqual(Color.Lerp(blue,new Color(1,.16f,.14f,1),.3f),block.GetColor("_BaseColor"));
            renderer.GetPropertyBlock(block,1);
            Assert.AreEqual(Color.Lerp(cream,new Color(1,.16f,.14f,1),.3f),block.GetColor("_BaseColor"));
            Assert.AreSame(first,renderer.sharedMaterials[0]);Assert.AreSame(second,renderer.sharedMaterials[1]);
            Assert.AreEqual(blue,first.GetColor("_BaseColor"));Assert.AreEqual(cream,second.GetColor("_BaseColor"));
            visual.ResetFeedback();renderer.GetPropertyBlock(block,0);Assert.IsTrue(block.isEmpty);
            renderer.GetPropertyBlock(block,1);Assert.IsTrue(block.isEmpty);LogAssert.NoUnexpectedReceived();
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Brainrot.Tests
{
    public sealed class MenuGeometryTests
    {
        [Test]public void IconButtonsKeepWordsCenteredAndKeepTheirOutlineSeparateFromShadow(){
            var parent=new GameObject("Buttons",typeof(RectTransform));
            try{
                parent.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
                foreach(var symbol in new[]{IslandSymbol.Play,IslandSymbol.Shop,IslandSymbol.Trophy,IslandSymbol.Settings,IslandSymbol.Exit}){
                    var button=ReferenceScreens.SeaButton(parent.transform,"НАСТРОЙКИ",.1f,.1f,.4f,.1f,()=>{},31,symbol==IslandSymbol.Play,symbol);
                    var label=button.GetComponentInChildren<Text>();var rect=label.rectTransform;
                    Assert.AreEqual(.5f,(rect.anchorMin.x+rect.anchorMax.x)*.5f,.001f,"The icon must not push the words off the button's center.");
                    var outline=label.GetComponent<Outline>();Shadow shadow=null;
                    foreach(var effect in label.GetComponents<Shadow>())if(effect.GetType()==typeof(Shadow))shadow=effect;
                    Assert.IsNotNull(shadow);Assert.AreNotSame(outline,shadow);
                    Assert.Greater(outline.effectDistance.x,0,"Configuring the shadow must not erase the horizontal outline.");
                    Assert.AreEqual(0,shadow.effectDistance.x);
                }
            }finally{Object.DestroyImmediate(parent);}
        }
        [Test]public void EveryPictogramHasAnExplicitRendererWhiteTextureAndVisibleClockwiseGeometry(){
            var parent=new GameObject("Icons",typeof(RectTransform));var mesh=new Mesh();
            try{
                parent.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
                foreach(IslandSymbol symbol in System.Enum.GetValues(typeof(IslandSymbol))){
                    var icon=IslandMenuSymbol.Place(parent.transform,symbol,.1f,.1f,.05f,.08f,Color.white);
                    Assert.IsNotNull(icon.GetComponent<CanvasRenderer>());Assert.AreSame(Texture2D.whiteTexture,icon.mainTexture);
                    using(var helper=new VertexHelper()){
                        typeof(IslandMenuSymbol).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(icon,new object[]{helper});
                        helper.FillMesh(mesh);
                    }
                    var vertices=mesh.vertices;var indices=mesh.triangles;Assert.Greater(indices.Length,0,symbol.ToString());
                    Assert.Greater(mesh.bounds.size.x,0);Assert.Greater(mesh.bounds.size.y,0);
                    for(int i=0;i<indices.Length;i+=3){
                        Vector3 u=vertices[indices[i+1]]-vertices[indices[i]],v=vertices[indices[i+2]]-vertices[indices[i]];
                        Assert.LessOrEqual(u.x*v.y-u.y*v.x,.001f,symbol+" must use UGUI's front-facing winding.");
                    }
                    Object.DestroyImmediate(icon.gameObject);mesh.Clear();
                }
            }finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(parent);}
        }
    }
}

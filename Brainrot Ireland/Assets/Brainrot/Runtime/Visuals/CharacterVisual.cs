using UnityEngine;
using System.Collections.Generic;
namespace Brainrot
{
    /// <summary>Replace this visual child with an imported rig without changing gameplay components.</summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        public Transform Weapon;
        Transform leftLeg,rightLeg,body,leftArm,rightArm;
        Vector3 bodyPosition; bool isShark;
        float phase,attackBlend;
        static Material white,black,wood,shoe,mouth;
        static readonly Dictionary<Color,Material> skins=new Dictionary<Color,Material>();
        static void Materials()
        {
            if(white!=null)return;
            white=WorldBuilder.Material(new Color(.98f,.98f,.91f));black=WorldBuilder.Material(new Color(.025f,.04f,.065f));
            wood=WorldBuilder.Material(new Color(.54f,.27f,.10f));shoe=WorldBuilder.Material(new Color(.12f,.30f,.94f));mouth=WorldBuilder.Material(new Color(.36f,.08f,.09f));
        }
        public static CharacterVisual Create(Transform parent,bool shark,Color color,float size=1)
        {
            Materials();var go=new GameObject(shark?"Tralala visual":"Tung Tung visual");go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*size;
            var c=go.AddComponent<CharacterVisual>();c.isShark=shark;
            if(!skins.TryGetValue(color,out var skin) || skin==null)
            { skin=WorldBuilder.Material(color,shark?"Shark skin":"Sahur wood"); skins[color]=skin; }
            c.body=new GameObject("Animated body").transform;c.body.SetParent(go.transform,false);
            if(shark)c.BuildShark(skin);else c.BuildSahur(skin);
            c.bodyPosition=c.body.localPosition;return c;
        }
        GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 s,Material mat)
        {return WorldBuilder.Primitive(name,type,parent,p,s,mat);}
        Transform Limb(string name,Vector3 pivot,Material mat)
        {
            var t=new GameObject(name).transform;t.SetParent(body,false);t.localPosition=pivot;
            Part(name+" shape",PrimitiveType.Capsule,t,new Vector3(0,-.23f,0),new Vector3(.19f,.32f,.19f),mat);return t;
        }
        void BuildSahur(Material skin)
        {
            Part("Log body",PrimitiveType.Cylinder,body,new Vector3(0,1.15f,0),new Vector3(.75f,.68f,.65f),skin);
            Part("Wood top",PrimitiveType.Cylinder,body,new Vector3(0,1.84f,0),new Vector3(.68f,.025f,.59f),wood);
            for(int j=0;j<7;j++)
            {
                float a=j*Mathf.PI*2/7;
                Part("Bark groove",PrimitiveType.Cube,body,new Vector3(Mathf.Cos(a)*.365f,1.15f,Mathf.Sin(a)*.31f),new Vector3(.022f,1.15f,.022f),wood);
            }
            Face(body,new Vector3(0,1.50f,.32f),.20f);
            leftLeg=Limb("Left leg",new Vector3(-.22f,.55f,0),skin);rightLeg=Limb("Right leg",new Vector3(.22f,.55f,0),skin);
            Part("Left foot",PrimitiveType.Sphere,leftLeg,new Vector3(0,-.43f,.08f),new Vector3(.24f,.16f,.39f),wood);
            Part("Right foot",PrimitiveType.Sphere,rightLeg,new Vector3(0,-.43f,.08f),new Vector3(.24f,.16f,.39f),wood);
            leftArm=Limb("Left arm",new Vector3(-.48f,1.35f,0),skin);rightArm=Limb("Bat arm",new Vector3(.48f,1.35f,0),skin);
            Weapon=new GameObject("Wooden bat pivot").transform;Weapon.SetParent(rightArm,false);Weapon.localPosition=new Vector3(0,-.45f,.15f);
            var bat=Part("Wooden bat",PrimitiveType.Capsule,Weapon,new Vector3(0,.38f,.25f),new Vector3(.17f,.55f,.17f),wood);
            bat.transform.localRotation=Quaternion.Euler(35,0,0);
        }
        void BuildShark(Material skin)
        {
            var torso=Part("Shark body",PrimitiveType.Capsule,body,new Vector3(0,1.08f,0),new Vector3(.94f,.85f,.92f),skin);
            torso.transform.localRotation=Quaternion.Euler(70,0,0);
            Part("White belly",PrimitiveType.Sphere,body,new Vector3(0,.97f,.40f),new Vector3(.70f,.60f,.31f),white);
            Part("Snout",PrimitiveType.Sphere,body,new Vector3(0,1.28f,.69f),new Vector3(.72f,.40f,.65f),skin);
            Part("Grinning jaw",PrimitiveType.Sphere,body,new Vector3(0,1.08f,.68f),new Vector3(.63f,.18f,.43f),mouth);
            for(int j=0;j<6;j++)
            {
                var tooth=Part("Tooth",PrimitiveType.Cube,body,new Vector3((j-2.5f)*.087f,1.13f,.878f),new Vector3(.06f,.095f,.04f),white);
                tooth.transform.localRotation=Quaternion.Euler(0,0,j%2==0?12:-12);
            }
            for(int j=-1;j<=1;j+=2)
            {
                Part("Eye",PrimitiveType.Sphere,body,new Vector3(j*.35f,1.49f,.46f),new Vector3(.22f,.23f,.13f),white);
                Part("Pupil",PrimitiveType.Sphere,body,new Vector3(j*.35f,1.48f,.525f),new Vector3(.09f,.12f,.035f),black);
                Fin(body,"Side fin",new Vector3(j*.48f,1.14f,-.05f),new Vector3(j*.65f,-.13f,-.28f),skin);
            }
            Fin(body,"Dorsal fin",new Vector3(0,1.40f,-.36f),new Vector3(0,.65f,-.27f),skin);
            Fin(body,"Tail left",new Vector3(0,1.03f,-.86f),new Vector3(-.44f,.25f,-.35f),skin);
            Fin(body,"Tail right",new Vector3(0,1.03f,-.86f),new Vector3(.44f,.25f,-.35f),skin);
            leftLeg=Limb("Left running leg",new Vector3(-.27f,.56f,0),skin);rightLeg=Limb("Right running leg",new Vector3(.27f,.56f,0),skin);
            foreach(var leg in new[]{leftLeg,rightLeg})
            {
                Part("Blue sneaker",PrimitiveType.Sphere,leg,new Vector3(0,-.43f,.12f),new Vector3(.35f,.25f,.60f),shoe);
                Part("Sneaker sole",PrimitiveType.Cube,leg,new Vector3(0,-.53f,.13f),new Vector3(.34f,.075f,.52f),white);
            }
        }
        void Face(Transform p,Vector3 center,float separation)
        {
            for(int j=-1;j<=1;j+=2)
            {
                Part("Wide eye",PrimitiveType.Sphere,p,center+Vector3.right*(separation*j),new Vector3(.28f,.32f,.13f),white);
                Part("Pupil",PrimitiveType.Sphere,p,center+new Vector3(separation*j,-.01f,.065f),new Vector3(.11f,.15f,.035f),black);
            }
            Part("Smile",PrimitiveType.Sphere,p,center+new Vector3(0,-.28f,.018f),new Vector3(.32f,.13f,.08f),mouth);
        }
        void Fin(Transform parent,string name,Vector3 start,Vector3 tip,Material mat)
        {
            var vertices=new[]{start+Vector3.forward*.22f,start-Vector3.forward*.22f,start+tip};
            var mesh=new Mesh{name=name,vertices=vertices,triangles=new[]{0,1,2,2,1,0}};mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        public void Animate(float speed,bool attacking)
        {
            float intensity=Mathf.Clamp01(speed/3);
            phase+=Time.deltaTime*(3+Mathf.Abs(speed)*2.5f);
            float swing=Mathf.Sin(phase)*32*intensity;
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            body.localPosition=bodyPosition+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.075f*intensity);
            body.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*3*intensity);
            attackBlend=Mathf.MoveTowards(attackBlend,attacking?1:0,Time.deltaTime*14);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-swing*.6f,0,-10);
            if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(Mathf.Lerp(swing*.6f,-80,attackBlend),Mathf.Sin(attackBlend*Mathf.PI)*100,-10);
            if(isShark && attacking)body.localPosition+=Vector3.forward*.10f;
        }
    }
}

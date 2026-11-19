using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    /// <summary>Hand-built tropical counter. Geometry is combined by shared colour; only its arrival transform animates.</summary>
    public sealed class IslandMarketStall:MonoBehaviour
    {
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        readonly Dictionary<Material,List<CombineInstance>> pieces=new Dictionary<Material,List<CombineInstance>>();
        Transform visual;Mesh box,cylinder,sphere;Collider[] solids;
        public float Amount{get;private set;}
        public bool TargetVisible{get;private set;}
        Material wood,woodLight,dark,cream,teal,gold,coral,glass;
        public void Build(){
            if(visual!=null)return;
            visual=new GameObject("Дерево, парус и товары").transform;visual.SetParent(transform,false);
            box=Own(Box());cylinder=Own(Cylinder(12));sphere=Own(Sphere(10,6));
            wood=Paint(new Color(.56f,.27f,.085f));woodLight=Paint(new Color(.79f,.45f,.16f));dark=Paint(new Color(.23f,.12f,.055f));
            cream=Paint(new Color(1,.92f,.70f));teal=Paint(new Color(.045f,.64f,.64f));gold=Paint(new Color(1,.68f,.16f));coral=Paint(new Color(.95f,.26f,.14f));glass=Paint(new Color(.10f,.39f,.46f));
            Add(box,wood,new Vector3(0,.12f,.08f),new Vector3(3.4f,.20f,1.65f));
            Add(box,dark,new Vector3(0,.61f,.28f),new Vector3(3.03f,.91f,.70f));
            for(int i=0;i<9;i++)Add(box,i%2==0?woodLight:wood,new Vector3(-1.35f+i*.3375f,.59f,.658f),new Vector3(.31f,.83f,.065f));
            Add(box,woodLight,new Vector3(0,1.10f,.27f),new Vector3(3.35f,.17f,1.02f));
            Add(box,gold,new Vector3(0,1.02f,.80f),new Vector3(3.28f,.045f,.055f));
            for(int side=-1;side<=1;side+=2){
                Add(cylinder,wood,new Vector3(side*1.54f,1.43f,-.48f),new Vector3(.15f,2.70f,.15f));
                Add(sphere,gold,new Vector3(side*1.54f,2.80f,-.48f),Vector3.one*.23f);
                Add(box,woodLight,new Vector3(side*1.18f,1.68f,-.43f),new Vector3(.12f,1.10f,.16f));
            }
            Add(box,woodLight,new Vector3(0,1.48f,-.42f),new Vector3(2.55f,.12f,.36f));
            Add(box,woodLight,new Vector3(0,1.95f,-.42f),new Vector3(2.55f,.12f,.35f));
            for(int i=0;i<7;i++){
                float x=-1.0f+i*.335f;var colour=i%3==0?coral:i%3==1?teal:glass;
                Add(cylinder,colour,new Vector3(x,1.70f,-.40f),new Vector3(.15f,.30f,.15f));
                Add(cylinder,gold,new Vector3(x,1.88f,-.40f),new Vector3(.085f,.10f,.085f));
                Add(sphere,cream,new Vector3(x,1.73f,-.317f),new Vector3(.065f,.11f,.02f));
            }
            for(int i=0;i<5;i++){
                float x=-.95f+i*.39f;
                Add(sphere,cream,new Vector3(x,1.24f,.28f),new Vector3(.24f,.16f,.20f));
                Add(cylinder,gold,new Vector3(x,1.36f,.27f),new Vector3(.11f,.025f,.11f),Quaternion.Euler(90,0,0));
            }
            // Display bat, braced diagonally against the counter.
            Add(cylinder,woodLight,new Vector3(1.08f,1.57f,.10f),new Vector3(.12f,.73f,.12f),Quaternion.Euler(0,0,-22));
            Add(cylinder,dark,new Vector3(.91f,1.16f,.10f),new Vector3(.075f,.23f,.075f),Quaternion.Euler(0,0,-22));
            // Seven individual canvas stripes share two materials and curve over the roof.
            for(int i=0;i<9;i++){
                float left=-1.86f+i*(3.72f/9);var roof=Own(Awning(left,left+3.72f/9));Add(roof,i%2==0?teal:cream,Vector3.zero,Vector3.one);
            }
            for(int side=-1;side<=1;side+=2)Add(sphere,gold,new Vector3(side*1.82f,2.58f,.97f),Vector3.one*.14f);
            // A shell emblem instead of baked lettering: the actual E prompt is live HUD text.
            Add(box,dark,new Vector3(0,2.26f,.96f),new Vector3(1.34f,.35f,.085f));
            Add(box,woodLight,new Vector3(0,2.28f,1.01f),new Vector3(1.20f,.26f,.025f));
            for(int i=-2;i<=2;i++)Add(cylinder,cream,new Vector3(i*.065f,2.29f,1.04f),new Vector3(.04f,.19f,.035f),Quaternion.Euler(0,0,i*-14));
            // Warm lantern: a painted glow, not an additional realtime light.
            Add(cylinder,dark,new Vector3(-1.47f,2.11f,.43f),new Vector3(.045f,.31f,.045f));
            Add(sphere,gold,new Vector3(-1.47f,1.88f,.43f),new Vector3(.20f,.29f,.20f));
            Add(cylinder,dark,new Vector3(-1.47f,1.71f,.43f),new Vector3(.25f,.045f,.25f));
            Add(cylinder,dark,new Vector3(-1.47f,2.04f,.43f),new Vector3(.24f,.045f,.24f));
            Combine();
            var counter=gameObject.AddComponent<BoxCollider>();counter.center=new Vector3(0,.59f,.28f);counter.size=new Vector3(3.05f,1.10f,.76f);
            var leftPost=gameObject.AddComponent<BoxCollider>();leftPost.center=new Vector3(-1.54f,1.4f,-.48f);leftPost.size=new Vector3(.18f,2.70f,.18f);
            var rightPost=gameObject.AddComponent<BoxCollider>();rightPost.center=new Vector3(1.54f,1.4f,-.48f);rightPost.size=leftPost.size;
            solids=new Collider[]{counter,leftPost,rightPost};SetSolids(false);visual.localScale=Vector3.one*.001f;visual.localPosition=Vector3.down*.32f;visual.gameObject.SetActive(false);
        }
        public void SetVisible(bool visible){TargetVisible=visible;if(!visible)SetSolids(false);if(visible&&visual!=null)visual.gameObject.SetActive(true);}
        public void TickPresentation(float delta,float seconds){
            if(visual==null||delta<=0)return;
            float target=TargetVisible?1:0;if(Mathf.Approximately(Amount,target))return;
            Amount=Mathf.MoveTowards(Amount,target,delta/Mathf.Max(.1f,seconds));
            float ease=1-Mathf.Pow(1-Amount,3),scale=Mathf.Max(.001f,ease+Mathf.Sin(Amount*Mathf.PI)*.045f);
            visual.localScale=Vector3.one*scale;visual.localPosition=Vector3.down*((1-ease)*.32f);
            visual.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(Amount*Mathf.PI)*2.5f);
            SetSolids(TargetVisible&&Amount>=.999f);
            if(Amount<=0&&!TargetVisible)visual.gameObject.SetActive(false);
        }
        void SetSolids(bool enabled){if(solids!=null)foreach(var collider in solids)if(collider!=null&&collider.enabled!=enabled)collider.enabled=enabled;}
        Material Paint(Color colour){var material=WorldBuilder.Material(colour,"Лавка — "+materials.Count);materials.Add(material);return material;}
        Mesh Own(Mesh mesh){meshes.Add(mesh);return mesh;}
        void Add(Mesh mesh,Material material,Vector3 point,Vector3 scale){Add(mesh,material,point,scale,Quaternion.identity);}
        void Add(Mesh mesh,Material material,Vector3 point,Vector3 scale,Quaternion rotation){
            if(!pieces.TryGetValue(material,out var list)){list=new List<CombineInstance>();pieces.Add(material,list);}
            list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(point,rotation,scale)});
        }
        void Combine(){
            foreach(var pair in pieces){
                var mesh=Own(new Mesh{name="Лавка — объединённая геометрия"});mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                var go=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(visual,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;renderer.shadowCastingMode=ShadowCastingMode.On;
            }
            pieces.Clear();
        }
        static Mesh Box(){
            Vector3[] corners={new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
            int[] faces={0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,3,7,6,2,0,1,5,4};
            var v=new Vector3[24];var t=new int[36];for(int i=0;i<6;i++){for(int j=0;j<4;j++)v[i*4+j]=corners[faces[i*4+j]];int a=i*4,b=i*6;t[b]=a;t[b+1]=a+1;t[b+2]=a+2;t[b+3]=a;t[b+4]=a+2;t[b+5]=a+3;}
            var mesh=new Mesh{name="Лавка — доска"};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();return mesh;
        }
        static Mesh Cylinder(int sides){
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<sides;i++){
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;int k=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(a)*.5f,.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(b)*.5f,.5f,Mathf.Sin(b)*.5f));v.Add(new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f));
                t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
                k=v.Count;v.Add(new Vector3(0,.5f,0));v.Add(new Vector3(Mathf.Cos(b)*.5f,.5f,Mathf.Sin(b)*.5f));v.Add(new Vector3(Mathf.Cos(a)*.5f,.5f,Mathf.Sin(a)*.5f));t.AddRange(new[]{k,k+1,k+2});
                k=v.Count;v.Add(new Vector3(0,-.5f,0));v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f));t.AddRange(new[]{k,k+1,k+2});
            }
            var mesh=new Mesh{name="Лавка — округлённая опора"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh Sphere(int sides,int rings){
            var v=new List<Vector3>();var t=new List<int>();
            for(int y=0;y<=rings;y++){float p=y*Mathf.PI/rings;for(int x=0;x<=sides;x++){float a=x*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a))*.5f);}}
            for(int y=0;y<rings;y++)for(int x=0;x<sides;x++){int a=y*(sides+1)+x,b=a+sides+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Лавка — округлый декор"};mesh.SetVertices(v);mesh.SetTriangles(t,0);var normals=new Vector3[v.Count];for(int i=0;i<v.Count;i++)normals[i]=v[i].normalized;mesh.normals=normals;return mesh;
        }
        static Mesh Awning(float left,float right){
            var v=new List<Vector3>();var t=new List<int>();const int steps=8;
            for(int i=0;i<=steps;i++){
                float z=Mathf.Lerp(-.85f,1.04f,i/(float)steps),y=2.62f+.28f*Mathf.Sin(i/(float)steps*Mathf.PI);
                v.Add(new Vector3(left,y,z));v.Add(new Vector3(right,y,z));
            }
            for(int i=0;i<steps;i++){int a=i*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            int k=v.Count;v.Add(new Vector3(left,2.62f,1.04f));v.Add(new Vector3(right,2.62f,1.04f));v.Add(new Vector3(right,2.43f,1.06f));v.Add(new Vector3((left+right)*.5f,2.36f,1.07f));v.Add(new Vector3(left,2.43f,1.06f));
            t.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+2,k,k+4,k+3});
            var mesh=new Mesh{name="Лавка — полоса паруса"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            // Separate back-face vertices keep opposing normals from cancelling each other.
            int count=v.Count,indexCount=t.Count;var normals=new List<Vector3>(mesh.normals);
            for(int i=0;i<count;i++){v.Add(v[i]);normals.Add(-normals[i]);}
            for(int i=0;i<indexCount;i+=3){t.Add(t[i]+count);t.Add(t[i+2]+count);t.Add(t[i+1]+count);}
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetNormals(normals);mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);meshes.Clear();materials.Clear();}
    }
}

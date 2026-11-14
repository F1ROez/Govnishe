using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Brainrot
{
    /// <summary>Deterministic five-biome island. All routes share one uninterrupted walkable floor.</summary>
    public static class WorldBuilder
    {
        public static Material Material(Color color, string name = "Материал острова")
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color, enableInstancing = true };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .16f);
            return material;
        }
        public static void Build()
        {
            var old = GameObject.Find("Процедурный остров") ?? GameObject.Find("Procedural Island");
            if (old != null) Object.Destroy(old);
            new IslandBuilder().Build();
        }
        // Kept for procedural character factories. Scenery uses batched meshes below instead.
        public static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); if (collider != null) Object.Destroy(collider);
            return go;
        }

        sealed class IslandBuilder
        {
            readonly Transform root;
            readonly IslandResources resources;
            readonly System.Random random = new System.Random(7312026);
            readonly Dictionary<string, Batch> batches = new Dictionary<string, Batch>();
            readonly List<Vector3[]> roads = new List<Vector3[]>();
            readonly Material[] ground = new Material[6];
            Material bark, darkBark, leaf, lime, paleLeaf, stone, cliff, sand, cream, white, red, road, water, reed, lilypad, flower, darkWater;
            IslandZone batchZone;
            public IslandBuilder()
            {
                root = new GameObject("Процедурный остров").transform;
                resources = root.gameObject.AddComponent<IslandResources>();
            }
            Material Make(Color color, string name)
            {
                var material = WorldBuilder.Material(color, name); resources.Own(material); return material;
            }
            public void Build()
            {
                for (int i = 0; i < 5; i++) ground[i] = Make(IslandMap.Color((IslandZone)i), IslandMap.Name((IslandZone)i));
                sand = ground[5] = Make(new Color(.99f,.82f,.49f),"Тёплый песок");
                bark=Make(new Color(.43f,.25f,.11f),"Пальмовая древесина");darkBark=Make(new Color(.24f,.20f,.12f),"Кора старых деревьев");
                leaf=Make(new Color(.13f,.43f,.19f),"Изумрудная листва");lime=Make(new Color(.34f,.66f,.13f),"Солнечная листва");paleLeaf=Make(new Color(.59f,.75f,.20f),"Края кроны");
                stone=Make(new Color(.47f,.53f,.49f),"Скалы");cliff=Make(new Color(.56f,.45f,.32f),"Береговые уступы");
                cream=Make(new Color(.89f,.77f,.55f),"Камень руин");white=Make(new Color(1,.95f,.78f),"Светлая ткань");red=Make(new Color(.93f,.27f,.16f),"Акцент");
                road=Make(new Color(.79f,.59f,.29f),"Тропинки");water=Make(new Color(.10f,.63f,.72f),"Ручьи");darkWater=Make(new Color(.22f,.35f,.40f),"Болотные заводи");
                reed=Make(new Color(.51f,.57f,.13f),"Камыш");lilypad=Make(new Color(.40f,.63f,.16f),"Кувшинки");flower=Make(new Color(1,.81f,.30f),"Полевые цветы");
                BuildGround(); BuildCoast(); BuildWater(); MakeRoads(); Rivers(); DrawRoads();
                Beach(); Meadows(); Forest(); Ruins(); Swamp();
                Bridge(new Vector3(-28,0,5),-48);Bridge(new Vector3(33,0,-20),72);
                foreach(var batch in batches.Values) batch.Finish(root,resources);
                Lighting();
            }
            void BuildGround()
            {
                const int segments=160, rings=42;
                var vertices=new Vector3[1+segments*rings];vertices[0]=new Vector3(0,-.025f,0);
                for(int r=1;r<=rings;r++)for(int i=0;i<segments;i++)
                {
                    float angle=i*Mathf.PI*2/segments;
                    float radius=IslandMap.CoastRadius(angle)*r/rings;
                    vertices[1+(r-1)*segments+i]=new Vector3(Mathf.Cos(angle)*radius,-.025f,Mathf.Sin(angle)*radius);
                }
                var triangles=new List<int>[6];for(int i=0;i<6;i++)triangles[i]=new List<int>();
                for(int i=0;i<segments;i++)AddGroundTriangle(vertices,triangles,0,1+(i+1)%segments,1+i);
                for(int r=1;r<rings;r++)for(int i=0;i<segments;i++)
                {
                    int a=1+(r-1)*segments+i,b=1+(r-1)*segments+(i+1)%segments,c=1+r*segments+i,d=1+r*segments+(i+1)%segments;
                    AddGroundTriangle(vertices,triangles,a,b,d);AddGroundTriangle(vertices,triangles,a,d,c);
                }
                var mesh=new Mesh{name="Непрерывная поверхность острова",vertices=vertices,subMeshCount=6};
                for(int i=0;i<6;i++)mesh.SetTriangles(triangles[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();resources.Own(mesh);
                var go=new GameObject("Земля — все пять зон",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=ground;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            }
            static void AddGroundTriangle(Vector3[] vertices,List<int>[] triangles,int a,int b,int c)
            {
                Vector3 midpoint=(vertices[a]+vertices[b]+vertices[c])/3;
                float coast=IslandMap.CoastRadius(Mathf.Atan2(midpoint.z,midpoint.x));
                int material=midpoint.x*midpoint.x+midpoint.z*midpoint.z>(coast-3)*(coast-3)?5:(int)IslandMap.GetZone(midpoint);
                triangles[material].Add(a);triangles[material].Add(b);triangles[material].Add(c);
            }
            void BuildCoast()
            {
                const int count=192;
                var vertices=new Vector3[count*2];var triangles=new int[count*6];
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float radius=IslandMap.CoastRadius(a);
                    vertices[i*2]=dir*radius+Vector3.down*.06f;vertices[i*2+1]=dir*(radius+1)+Vector3.down*5;
                    int next=(i+1)%count*2,k=i*6;triangles[k]=i*2;triangles[k+1]=next;triangles[k+2]=i*2+1;triangles[k+3]=i*2+1;triangles[k+4]=next;triangles[k+5]=next+1;
                    if(i%2==0)
                    {
                        batchZone=IslandMap.GetZone(dir*(radius-4));
                        Add(PrimitiveType.Sphere,dir*(radius-.3f)+Vector3.down*.9f,new Vector3(Range(1.5f,3),Range(2.5f,4.5f),Range(1.5f,3)),stone,Quaternion.Euler(Range(-12,12),Range(0,360),Range(-10,10)));
                    }
                }
                var mesh=new Mesh{name="Береговые склоны",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();resources.Own(mesh);
                MeshObject("Береговые склоны",mesh,cliff);
                // Thin pale foam ribbon follows the actual irregular coast.
                var ring=new Vector3[count+1];for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;ring[i]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(IslandMap.CoastRadius(a)+2.2f);}
                Strip("Пена у берега",ring,.75f,-3.28f,Make(new Color(.73f,.96f,.90f),"Морская пена"));
            }
            void BuildWater()
            {
                var ocean=GameBalance.Current.Ocean;var shader=Shader.Find("Brainrot/TropicalWater");
                var material=shader!=null?new Material(shader):WorldBuilder.Material(ocean.DeepColor,"Океан");resources.Own(material);material.name="Океан вокруг острова";
                if(shader!=null){material.SetColor("_BaseColor",ocean.DeepColor);material.SetColor("_ShallowColor",ocean.ShallowColor);material.SetFloat("_WaveSpeed",ocean.WaveSpeed);material.SetFloat("_WaveScale",ocean.WaveScale);material.SetFloat("_WaveStrength",ocean.WaveStrength);}
                var oceanGo=Primitive("Океан",PrimitiveType.Plane,root,new Vector3(0,-3.4f,0),new Vector3(400,1,400),material);
                oceanGo.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            void MakeRoads()
            {
                roads.Add(Curve(new Vector3(0,0,-83),new Vector3(-10,0,-46),new Vector3(0,0,-12)));
                roads.Add(Curve(new Vector3(0,0,-12),new Vector3(-40,0,-2),new Vector3(-48,0,35)));
                roads.Add(Curve(new Vector3(0,0,-12),new Vector3(29,0,-5),new Vector3(46,0,40)));
                roads.Add(Curve(new Vector3(0,0,-12),new Vector3(25,0,-17),new Vector3(55,0,-26)));
                roads.Add(Curve(new Vector3(-48,0,35),new Vector3(-8,0,63),new Vector3(46,0,40)));
            }
            static Vector3[] Curve(Vector3 a,Vector3 b,Vector3 c)
            {
                var result=new Vector3[41];for(int i=0;i<result.Length;i++){float t=i/40f;result[i]=(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c;}return result;
            }
            void DrawRoads(){for(int i=0;i<roads.Count;i++)Strip("Тропинка "+(i+1),roads[i],4.4f,.012f,road);}
            void Rivers()
            {
                Strip("Западный ручей",Curve(new Vector3(-33,0,45),new Vector3(-24,0,-15),new Vector3(-29,0,-91)),3.2f,.006f,water);
                Strip("Восточный ручей",Curve(new Vector3(35,0,20),new Vector3(38,0,-33),new Vector3(25,0,-91)),3.6f,.007f,water);
                batchZone=IslandZone.Forest;Add(PrimitiveType.Sphere,new Vector3(-33,.015f,45),new Vector3(8,.025f,7),water);
            }
            void Beach()
            {
                batchZone=IslandZone.Beach;
                for(int i=0;i<31;i++)if(Sample(IslandZone.Beach,3,out var point))Palm(point,Range(4,7));
                Umbrella(new Vector3(-12,0,-76));Umbrella(new Vector3(13,0,-81));
                for(int i=0;i<65;i++)if(Sample(IslandZone.Beach,1,out var point))
                {
                    if(i%6==0)Star(point,red);else Add(PrimitiveType.Sphere,point+Vector3.up*.045f,new Vector3(.28f,.12f,.4f),white);
                }
                Sign(new Vector3(-7,0,-63),"СТАРТОВЫЙ ПЛЯЖ",IslandMap.Color(IslandZone.Beach));
            }
            void Meadows()
            {
                batchZone=IslandZone.Meadows;
                for(int i=0;i<28;i++)if(Sample(IslandZone.Meadows,2,out var point))Broadleaf(point,Range(3.2f,4.5f),true);
                for(int i=0;i<185;i++)if(Sample(IslandZone.Meadows,.6f,out var point))
                {
                    float h=Range(.18f,.45f);Add(PrimitiveType.Cylinder,point+Vector3.up*h/2,new Vector3(.04f,h/2,.04f),leaf);
                    Add(PrimitiveType.Sphere,point+Vector3.up*h,new Vector3(.25f,.12f,.25f),i%3==0?white:flower);
                }
                Windmill(new Vector3(13,0,-8));
                for(int i=0;i<5;i++){Vector3 point=new Vector3(-15+i*1.5f,.5f,-25);Add(PrimitiveType.Cylinder,point,new Vector3(1.2f,.65f,1.2f),flower,Quaternion.Euler(0,0,90));}
                Fence(new Vector3(9,0,-18),new Vector3(22,0,-18));Fence(new Vector3(22,0,-18),new Vector3(22,0,-4));
                Sign(new Vector3(-7,0,-20),"ЛУГА",IslandMap.Color(IslandZone.Meadows));
            }
            void Forest()
            {
                batchZone=IslandZone.Forest;
                for(int i=0;i<100;i++)if(Sample(IslandZone.Forest,3.4f,out var point))Broadleaf(point,Range(5,9),false);
                for(int i=0;i<55;i++)if(Sample(IslandZone.Forest,1,out var point))
                {
                    float h=Range(.35f,.7f);Add(PrimitiveType.Cylinder,point+Vector3.up*h*.45f,new Vector3(.14f,h*.45f,.14f),white);
                    Add(PrimitiveType.Sphere,point+Vector3.up*h,new Vector3(.65f,.3f,.65f),red);
                    Add(PrimitiveType.Sphere,point+new Vector3(.12f,h+.13f,.12f),new Vector3(.12f,.06f,.12f),white);
                }
                for(int i=0;i<20;i++)if(Sample(IslandZone.Forest,2,out var point))Rock(point,Range(1,2));
                Sign(new Vector3(-45,0,27),"ЛЕС ПАТАПИМА",IslandMap.Color(IslandZone.Forest));
            }
            void Ruins()
            {
                batchZone=IslandZone.Ruins;
                Vector3 center=IslandMap.Center(IslandZone.Ruins);
                for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
                {
                    Vector3 p=center+new Vector3(x*2.2f,0,z*2.2f);Add(PrimitiveType.Cube,p+Vector3.up*.025f,new Vector3(2,.06f,2),cream);
                }
                Arch(center+new Vector3(0,0,12),0,1.3f);Arch(center+new Vector3(-13,0,0),90,.85f);Arch(center+new Vector3(13,0,-4),-20,.9f);
                for(int i=0;i<10;i++)
                {
                    float a=i*Mathf.PI*2/10;Vector3 p=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*19;
                    Column(p,Range(2.4f,5.5f),i%3==0);
                }
                for(int i=0;i<28;i++)if(Sample(IslandZone.Ruins,1.3f,out var point))
                {
                    if(i%3==0)Cypress(point,Range(3,6));else Add(PrimitiveType.Cube,point+Vector3.up*.4f,new Vector3(1.8f,.8f,1.1f),cream,Quaternion.Euler(0,Range(0,180),Range(-10,10)));
                }
                Sign(center+new Vector3(-8,0,-12),"МАКАРОННЫЕ РУИНЫ",IslandMap.Color(IslandZone.Ruins));
            }
            void Swamp()
            {
                batchZone=IslandZone.Swamp;
                for(int i=0;i<16;i++)if(Sample(IslandZone.Swamp,4,out var point))
                {
                    float radius=Range(2.6f,5.2f);Add(PrimitiveType.Sphere,point+Vector3.up*.01f,new Vector3(radius*2,.025f,radius*1.5f),darkWater);
                    for(int j=0;j<6;j++)
                    {
                        float a=Range(0,Mathf.PI*2);Vector3 edge=point+new Vector3(Mathf.Cos(a)*radius*.9f,0,Mathf.Sin(a)*radius*.65f);
                        Reeds(edge);
                        if(j%2==0){Vector3 pad=point+new Vector3(Range(-radius*.6f,radius*.6f),.045f,Range(-radius*.4f,radius*.4f));Add(PrimitiveType.Cylinder,pad,new Vector3(.65f,.018f,.65f),lilypad);}
                    }
                }
                for(int i=0;i<32;i++)if(Sample(IslandZone.Swamp,2.5f,out var point))DeadTree(point,Range(3,5.5f));
                for(int i=0;i<26;i++)if(Sample(IslandZone.Swamp,1,out var point))Reeds(point);
                Sign(new Vector3(49,0,-18),"БОЛОТО БОМБАРДИРО",IslandMap.Color(IslandZone.Swamp));
            }
            bool Sample(IslandZone zone,float clearance,out Vector3 point)
            {
                for(int attempt=0;attempt<60;attempt++)
                {
                    point=new Vector3(Range(-98,98),0,Range(-98,98));
                    if(!IslandMap.IsWalkable(point,6)||IslandMap.GetZone(point)!=zone)continue;
                    if((point-IslandMap.Center(zone)).sqrMagnitude<50||(point-IslandMap.StartPosition).sqrMagnitude<36)continue;
                    if(RoadDistance(point)<clearance+3)continue;
                    return true;
                }
                point=Vector3.zero;return false;
            }
            float RoadDistance(Vector3 point)
            {
                float distance=float.PositiveInfinity;
                foreach(var path in roads)for(int i=1;i<path.Length;i++)
                {
                    Vector3 delta=path[i]-path[i-1];float t=Mathf.Clamp01(Vector3.Dot(point-path[i-1],delta)/Mathf.Max(.001f,delta.sqrMagnitude));
                    distance=Mathf.Min(distance,(point-(path[i-1]+delta*t)).magnitude);
                }
                return distance;
            }
            void Palm(Vector3 p,float h)
            {
                for(int j=0;j<5;j++)Add(PrimitiveType.Cylinder,p+new Vector3(j*j*.035f,h*(j+.5f)/5,0),new Vector3(.48f-j*.04f,h/10+.02f,.48f-j*.04f),bark,Quaternion.Euler(0,0,-j*3));
                Vector3 crown=p+new Vector3(.65f,h,0);float offset=Range(0,360);
                for(int j=0;j<7;j++){Quaternion rotation=Quaternion.Euler(12,offset+j*360/7f,0);Add(PrimitiveType.Sphere,crown+rotation*Vector3.forward*1.4f,new Vector3(.75f,.18f,4),j%2==0?lime:leaf,rotation);}
                for(int j=0;j<3;j++)Add(PrimitiveType.Sphere,crown+new Vector3((j-1)*.23f,-.2f,.18f),Vector3.one*.35f,bark);
            }
            void Broadleaf(Vector3 p,float h,bool small)
            {
                Add(PrimitiveType.Cylinder,p+Vector3.up*h*.38f,new Vector3(h*.085f,h*.38f,h*.085f),darkBark);
                float radius=h*.7f;Add(PrimitiveType.Sphere,p+Vector3.up*h*.83f,new Vector3(radius*1.25f,h*.56f,radius),small?lime:leaf);
                Add(PrimitiveType.Sphere,p+new Vector3(-h*.3f,h*.77f,h*.1f),new Vector3(radius,h*.4f,radius),lime);
                Add(PrimitiveType.Sphere,p+new Vector3(h*.25f,h*.98f,-h*.12f),new Vector3(radius*.85f,h*.4f,radius*.9f),small?lime:paleLeaf);
                Add(PrimitiveType.Cylinder,p+Vector3.up*.07f,new Vector3(h*.4f,.07f,h*.38f),leaf);
            }
            void Rock(Vector3 p,float size){Add(PrimitiveType.Sphere,p+Vector3.up*size*.35f,new Vector3(size*1.4f,size,size*1.2f),stone,Quaternion.Euler(Range(-15,15),Range(0,360),Range(-15,15)));}
            void Cypress(Vector3 p,float h){Add(PrimitiveType.Cylinder,p+Vector3.up*h*.3f,new Vector3(.22f,h*.3f,.22f),bark);Add(PrimitiveType.Capsule,p+Vector3.up*h*.65f,new Vector3(h*.25f,h*.44f,h*.25f),leaf);}
            void Star(Vector3 p,Material material){for(int j=0;j<5;j++)Add(PrimitiveType.Cube,p+Vector3.up*.06f,new Vector3(.1f,.08f,.8f),material,Quaternion.Euler(0,j*72,0));}
            void Umbrella(Vector3 p)
            {
                Add(PrimitiveType.Cylinder,p+Vector3.up*1.45f,new Vector3(.11f,1.45f,.11f),bark);
                Add(PrimitiveType.Sphere,p+Vector3.up*2.8f,new Vector3(3.5f,.8f,3.5f),red);
                for(int j=0;j<4;j++)Add(PrimitiveType.Cube,p+Vector3.up*2.98f,new Vector3(.3f,.08f,3.3f),white,Quaternion.Euler(0,j*45,0));
                Add(PrimitiveType.Cube,p+new Vector3(2,.35f,-.5f),new Vector3(.9f,.2f,2.2f),white,Quaternion.Euler(-10,0,0));
            }
            void Windmill(Vector3 p)
            {
                Add(PrimitiveType.Cylinder,p+Vector3.up*2.8f,new Vector3(3,2.8f,3),cream);
                Add(PrimitiveType.Capsule,p+Vector3.up*5.8f,new Vector3(3.2f,.9f,3.2f),red);
                Add(PrimitiveType.Cube,p+new Vector3(0,1,-1.52f),new Vector3(.8f,2,.15f),bark);
                Vector3 hub=p+new Vector3(0,4.2f,-1.8f);
                for(int i=0;i<4;i++)
                {
                    Quaternion turn=Quaternion.Euler(0,0,25+i*90);Add(PrimitiveType.Cube,hub+turn*Vector3.up*1.6f,new Vector3(.18f,4.1f,.16f),bark,turn);
                    Add(PrimitiveType.Cube,hub+turn*new Vector3(.28f,2.2f,0),new Vector3(.75f,1.8f,.10f),white,turn);
                }
                Add(PrimitiveType.Sphere,hub,new Vector3(.55f,.55f,.3f),bark);
            }
            void Fence(Vector3 start,Vector3 end)
            {
                Vector3 direction=end-start;float length=direction.magnitude;Quaternion rotation=Quaternion.LookRotation(direction);
                for(float t=0;t<=length;t+=2.5f)Add(PrimitiveType.Cube,start+direction.normalized*t+Vector3.up*.55f,new Vector3(.16f,1.1f,.16f),bark);
                Add(PrimitiveType.Cube,(start+end)/2+Vector3.up*.65f,new Vector3(.12f,.12f,length),bark,rotation);
            }
            void Column(Vector3 p,float h,bool broken)
            {
                Add(PrimitiveType.Cube,p+Vector3.up*.2f,new Vector3(1.6f,.4f,1.6f),cream);
                Add(PrimitiveType.Cylinder,p+Vector3.up*(h/2+.35f),new Vector3(.92f,h/2,.92f),cream);
                if(!broken)Add(PrimitiveType.Cube,p+Vector3.up*(h+.55f),new Vector3(1.45f,.4f,1.45f),cream);
                Add(PrimitiveType.Sphere,p+new Vector3(.6f,.15f,.4f),new Vector3(.7f,.2f,.5f),leaf);
            }
            void Arch(Vector3 p,float angle,float scale)
            {
                Quaternion rotation=Quaternion.Euler(0,angle,0);
                Column(p+rotation*Vector3.left*2.3f*scale,4.5f*scale,false);Column(p+rotation*Vector3.right*2.3f*scale,4.5f*scale,false);
                // Small voussoir blocks form a real open arch silhouette.
                for(int i=0;i<=10;i++)
                {
                    float a=i*Mathf.PI/10;Vector3 local=new Vector3(Mathf.Cos(a)*2.3f,4.9f+Mathf.Sin(a)*2.3f,0)*scale;
                    Add(PrimitiveType.Cube,p+rotation*local,new Vector3(.78f,.80f,1.1f)*scale,cream,rotation*Quaternion.Euler(0,0,a*Mathf.Rad2Deg));
                }
            }
            void Reeds(Vector3 p)
            {
                for(int i=0;i<4;i++)
                {
                    float h=Range(.7f,1.5f);Vector3 point=p+new Vector3(Range(-.45f,.45f),0,Range(-.45f,.45f));
                    Add(PrimitiveType.Cube,point+Vector3.up*h/2,new Vector3(.07f,h,.07f),reed,Quaternion.Euler(Range(-9,9),0,Range(-9,9)));
                    Add(PrimitiveType.Capsule,point+Vector3.up*h,new Vector3(.14f,.2f,.14f),bark);
                }
            }
            void DeadTree(Vector3 p,float h)
            {
                Add(PrimitiveType.Cylinder,p+Vector3.up*h*.45f,new Vector3(.40f,h*.45f,.40f),darkBark,Quaternion.Euler(0,0,8));
                for(int j=0;j<3;j++)Add(PrimitiveType.Cylinder,p+new Vector3(j%2==0?.65f:-.65f,h*(.5f+j*.15f),0),new Vector3(.16f,h*.25f,.16f),darkBark,Quaternion.Euler(0,j*80,j%2==0?-45:45));
                Add(PrimitiveType.Sphere,p+new Vector3(0,h*.7f,0),new Vector3(2,.18f,1.5f),leaf);
            }
            void Bridge(Vector3 p,float angle)
            {
                batchZone=IslandMap.GetZone(p);Quaternion rotation=Quaternion.Euler(0,angle,0);
                for(int j=0;j<11;j++)Add(PrimitiveType.Cube,p+rotation*new Vector3(0,.06f,(j-5)*.5f),new Vector3(4.5f,.16f,.43f),bark,rotation);
                for(int side=-1;side<=1;side+=2)
                {
                    for(int j=-1;j<=1;j+=2)Add(PrimitiveType.Cube,p+rotation*new Vector3(side*2.2f,.65f,j*2.3f),new Vector3(.16f,1.3f,.16f),bark,rotation);
                    Add(PrimitiveType.Cube,p+rotation*new Vector3(side*2.2f,.95f,0),new Vector3(.13f,.12f,4.6f),bark,rotation);
                }
            }
            void Sign(Vector3 p,string title,Color color)
            {
                Add(PrimitiveType.Cylinder,p+Vector3.up*1.2f,new Vector3(.15f,1.2f,.15f),bark);
                Add(PrimitiveType.Cube,p+Vector3.up*2.25f,new Vector3(5,.8f,.18f),darkBark);
                var go=new GameObject(title,typeof(TextMesh));go.transform.SetParent(root,false);go.transform.position=p+new Vector3(0,2.3f,-.12f);go.transform.rotation=Quaternion.identity;
                var text=go.GetComponent<TextMesh>();text.text=title;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=52;text.characterSize=.22f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(1,.94f,.70f);
                go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            }
            void Strip(string name,Vector3[] path,float width,float y,Material material)
            {
                var v=new Vector3[path.Length*2];var t=new int[(path.Length-1)*6];
                for(int i=0;i<path.Length;i++)
                {
                    Vector3 direction=path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)];direction.y=0;
                    Vector3 side=Vector3.Cross(Vector3.up,direction.normalized)*width*.5f;
                    v[i*2]=path[i]-side;v[i*2+1]=path[i]+side;v[i*2].y=v[i*2+1].y=y;
                    if(i==path.Length-1)continue;int k=i*6;t[k]=i*2;t[k+1]=i*2+2;t[k+2]=i*2+1;t[k+3]=i*2+1;t[k+4]=i*2+2;t[k+5]=i*2+3;
                }
                var mesh=new Mesh{name=name,vertices=v,triangles=t};mesh.RecalculateNormals();mesh.RecalculateBounds();resources.Own(mesh);MeshObject(name,mesh,material);
            }
            void MeshObject(string name,Mesh mesh,Material material)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
            void Add(PrimitiveType type,Vector3 position,Vector3 scale,Material material,Quaternion? rotation=null)
            {
                string key=((int)batchZone)+":"+material.GetInstanceID();
                if(!batches.TryGetValue(key,out var batch)){batch=new Batch(IslandMap.Name(batchZone)+" — "+material.name,material);batches.Add(key,batch);}
                batch.Add(SceneryMesh.For(type),Matrix4x4.TRS(position,rotation??Quaternion.identity,scale));
            }
            float Range(float minimum,float maximum){return Mathf.Lerp(minimum,maximum,(float)random.NextDouble());}
            void Lighting()
            {
                if(Object.FindFirstObjectByType<Light>()==null)
                {
                    var sun=new GameObject("Солнце над островом").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;sun.color=new Color(1,.94f,.79f);sun.intensity=1.25f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(49,-36,0);
                }
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.50f,.74f,.86f);RenderSettings.ambientEquatorColor=new Color(.44f,.63f,.57f);RenderSettings.ambientGroundColor=new Color(.36f,.32f,.20f);
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.40f,.78f,.89f);RenderSettings.fogDensity=.0018f;RenderSettings.skybox=null;
            }
        }
        sealed class Batch
        {
            readonly string name;readonly Material material;readonly List<CombineInstance> items=new List<CombineInstance>();
            public Batch(string name,Material material){this.name=name;this.material=material;}
            public void Add(Mesh mesh,Matrix4x4 transform){items.Add(new CombineInstance{mesh=mesh,transform=transform});}
            public void Finish(Transform parent,IslandResources resources)
            {
                int cursor=0,chunk=0;
                while(cursor<items.Count)
                {
                    var portion=new List<CombineInstance>();int count=0;
                    while(cursor<items.Count&&(count+items[cursor].mesh.vertexCount<50000||portion.Count==0)){count+=items[cursor].mesh.vertexCount;portion.Add(items[cursor++]);}
                    var mesh=new Mesh{name=name+" "+chunk};mesh.CombineMeshes(portion.ToArray(),true,true);mesh.RecalculateBounds();resources.Own(mesh);
                    var go=new GameObject(name+" "+chunk++,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
                }
            }
        }
        static class SceneryMesh
        {
            static Mesh cube,sphere,cylinder;
            public static Mesh For(PrimitiveType type)
            {
                if(type==PrimitiveType.Cube)return cube!=null?cube:(cube=Cube());
                if(type==PrimitiveType.Cylinder)return cylinder!=null?cylinder:(cylinder=Cylinder());
                return sphere!=null?sphere:(sphere=Sphere());
            }
            static Mesh Cube()
            {
                // A 24-vertex box keeps flat, crisp lighting on ruins and bridges.
                var positions=new List<Vector3>();var triangles=new List<int>();
                Vector3[] normals={Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up,Vector3.down};
                foreach(var n in normals)
                {
                    Vector3 up=Mathf.Abs(n.y)>.5f?Vector3.forward:Vector3.up;Vector3 right=Vector3.Cross(up,n);
                    int k=positions.Count;positions.Add((n-right-up)*.5f);positions.Add((n+right-up)*.5f);positions.Add((n+right+up)*.5f);positions.Add((n-right+up)*.5f);
                    triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
                }
                return Finish("Куб декораций",positions.ToArray(),triangles.ToArray());
            }
            static Mesh Sphere()
            {
                const int sides=10,rings=6;var v=new Vector3[(rings+1)*sides];var t=new List<int>();
                for(int r=0;r<=rings;r++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,b=r*Mathf.PI/rings;v[r*sides+i]=new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b),Mathf.Sin(a)*Mathf.Sin(b))*.5f;}
                for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int a=r*sides+i,b=r*sides+(i+1)%sides,c=(r+1)*sides+i,d=(r+1)*sides+(i+1)%sides;t.AddRange(new[]{a,b,c,b,d,c});}
                return Finish("Низкополигональная крона",v,t.ToArray());
            }
            static Mesh Cylinder()
            {
                const int n=10;var v=new Vector3[n*2+2];var t=new List<int>();v[n*2]=Vector3.up;v[n*2+1]=Vector3.down;
                for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;v[i]=new Vector3(Mathf.Cos(a)*.5f,1,Mathf.Sin(a)*.5f);v[n+i]=new Vector3(Mathf.Cos(a)*.5f,-1,Mathf.Sin(a)*.5f);}
                for(int i=0;i<n;i++){int next=(i+1)%n;t.AddRange(new[]{i,next,n+i,next,n+next,n+i,n*2,next,i,n*2+1,n+i,n+next});}
                return Finish("Сегмент ствола",v,t.ToArray());
            }
            static Mesh Finish(string name,Vector3[] v,int[] t){var mesh=new Mesh{name=name,vertices=v,triangles=t};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        }
    }
    /// <summary>Generated geometry/materials are released when a test scene or world is unloaded.</summary>
    sealed class IslandResources:MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        public void Own(Object asset){owned.Add(asset);}
        void OnDestroy(){foreach(var asset in owned)if(asset!=null)Destroy(asset);}
    }
}

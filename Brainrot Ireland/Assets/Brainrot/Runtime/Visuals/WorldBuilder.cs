using UnityEngine;
using UnityEngine.Rendering;

namespace Brainrot
{
    /// <summary>Self-contained, deterministic tropical arena. Gameplay surface is the y=0 plane.</summary>
    public static class WorldBuilder
    {
        static Material sand, grass, bark, leaf, stone, foam, coral, leafLight;
        public static Material Material(Color color, string name = "Island material")
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = name };
            m.color = color; m.enableInstancing=true;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .12f);
            return m;
        }
        public static void Build()
        {
            var old = GameObject.Find("Procedural Island");
            if (old != null) Object.Destroy(old);
            var root = new GameObject("Procedural Island").transform;
            sand = Material(Color.white, "Vanilla sand — hand-painted procedural palette");
            sand.SetTexture("_BaseMap",SandPalette());
            grass = Material(new Color(.56f,.81f,.32f), "Lime vegetation");
            bark = Material(new Color(.63f,.42f,.24f), "Palm trunk");
            leaf = Material(new Color(.32f,.78f,.33f), "Palm leaves");
            stone = Material(new Color(.66f,.75f,.79f), "Coastal slate");
            foam = Material(new Color(.96f,1,.96f), "Foam");
            coral=Material(new Color(.98f,.61f,.48f),"Peach seashells");
            leafLight=Material(new Color(.68f,.89f,.32f),"Young lime leaves");
            leaf.SetFloat("_Cull",0);leafLight.SetFloat("_Cull",0);grass.SetFloat("_Cull",0);
            var rng = new System.Random(731);
            Island(root);
            // Physics support below the ocean: tumbling bodies cannot fall forever outside the sand.
            var support=new GameObject("Дно для физических тел",typeof(BoxCollider));support.transform.SetParent(root,false);support.transform.position=new Vector3(0,-.8f,0);support.GetComponent<BoxCollider>().size=new Vector3(160,.3f,160);
            var water = Shader.Find("Brainrot/TropicalWater");
            var ocean=GameBalance.Current.Ocean;
            var waterMat = water != null ? new Material(water) : Material(ocean.DeepColor, "Ocean");
            waterMat.name="Soft tropical ocean";
            if(water!=null)
            {
                waterMat.SetColor("_BaseColor",ocean.DeepColor);
                waterMat.SetColor("_ShallowColor",ocean.ShallowColor);
                waterMat.SetFloat("_WaveSpeed",ocean.WaveSpeed);
                waterMat.SetFloat("_WaveScale",ocean.WaveScale);
                waterMat.SetFloat("_WaveStrength",ocean.WaveStrength);
            }
            Primitive("Endless ocean", PrimitiveType.Plane, root, new Vector3(0,-.43f,0), new Vector3(400,1,400), waterMat);
            // Animated foam now lives in the water shader; no opaque floating foam ring.
            // Shore colour is blended smoothly by the ocean shader instead of a hard turquoise ring.
            for (int i = 0; i < 13; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2;
                float r = 17.3f + (float)rng.NextDouble() * 2.7f;
                var p = new Vector3(Mathf.Cos(a)*r, 0, Mathf.Sin(a)*r);
                if (i % 3 != 0) Palm(root, p, 2.9f + (float)rng.NextDouble()*1.5f, a);
                else
                {
                    var rock = Primitive("Beach rock", PrimitiveType.Sphere, root, p + Vector3.up*.28f, new Vector3(1.3f,.8f,1.1f), stone);
                    rock.GetComponent<MeshFilter>().sharedMesh=BeachRockMesh();
                    rock.transform.Rotate(12, a*Mathf.Rad2Deg, 23);
                }
            }
            for (int i=0; i<35; i++)
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2;
                float r=16+(float)rng.NextDouble()*5;
                var shell=Primitive("Shell",PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*r,.04f,Mathf.Sin(a)*r),new Vector3(.13f,.06f,.18f),foam);
                shell.transform.Rotate(0,a*Mathf.Rad2Deg,0);
            }
            // Coastal clusters leave the central combat arena visually quiet and unobstructed.
            for(int i=0;i<12;i++){
                float a=i*Mathf.PI*2/12+.16f;float r=18.2f+(float)rng.NextDouble()*1.7f;
                BeachPlants(root,new Vector3(Mathf.Cos(a)*r,-.01f,Mathf.Sin(a)*r),a);
                if(i%3==0)Starfish(root,new Vector3(Mathf.Cos(a+.06f)*(r+1),.015f,Mathf.Sin(a+.06f)*(r+1)),a);
            }
            Light sun=null;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional&&(old==null||!light.transform.IsChildOf(old.transform))){sun=light;break;}
            if(sun==null){sun=new GameObject("Тропическое солнце").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;}
            sun.name="Персиковое тропическое солнце";
            sun.color=new Color(1,.94f,.82f);sun.intensity=1.35f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.40f;sun.shadowBias=.035f;sun.shadowNormalBias=.18f;
            sun.transform.rotation=Quaternion.Euler(48,-38,0);RenderSettings.sun=sun;
            // Sky and sand bounce light keep silhouettes readable without extra shadow maps.
            var fill=new GameObject("Небесный заполняющий свет").AddComponent<Light>();fill.transform.SetParent(root,false);
            fill.type=LightType.Directional;fill.color=new Color(.73f,.88f,1);fill.intensity=.30f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(28,142,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=new Color(.75f,.88f,1);
            RenderSettings.ambientEquatorColor=new Color(.70f,.82f,.84f);
            RenderSettings.ambientGroundColor=new Color(.66f,.59f,.43f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.69f,.88f,.97f);RenderSettings.fogDensity=.0022f;
            RenderSettings.reflectionIntensity=.38f;
            var skyShader=Shader.Find("Brainrot/TropicalSky");
            if(skyShader!=null){var sky=new Material(skyShader){name="Акварельное тропическое небо"};sky.SetColor("_Zenith",new Color(.21f,.63f,.91f));sky.SetColor("_Horizon",RenderSettings.fogColor);sky.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=sky;}
            root.gameObject.AddComponent<WorldAtmosphere>();
        }

        static void Island(Transform parent)
        {
            const int n=128;
            var v=new Vector3[n+1];var uv=new Vector2[n+1];uv[0]=Vector2.one*.5f; var colors=new Color[n+1]; var tris=new int[n*3];
            v[0]=new Vector3(0,-.025f,0); colors[0]=new Color(1,.88f,.59f);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n;
                float r=22f+Mathf.Sin(a*5)*.3f+Mathf.Cos(a*9)*.17f;
                v[i+1]=new Vector3(Mathf.Cos(a)*r,-.11f,Mathf.Sin(a)*r);
                uv[i+1]=new Vector2(v[i+1].x/45+.5f,v[i+1].z/45+.5f);
                colors[i+1]=new Color(.93f,.70f,.33f);
                tris[i*3]=0; tris[i*3+1]=(i+1)%n+1; tris[i*3+2]=i+1;
            }
            var mesh=new Mesh { name="Sandy island disk", vertices=v, triangles=tris, colors=colors,uv=uv };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go=new GameObject("Sand arena",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(parent,false); go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=sand; go.GetComponent<MeshCollider>().sharedMesh=mesh;
            Primitive("Island foundation",PrimitiveType.Cylinder,parent,new Vector3(0,-1.2f,0),new Vector3(44,1.08f,44),sand);
        }
        static void Ring(Transform root,string name,float radius,float width,float y,Material mat,int segments)
        {
            var v=new Vector3[segments*2]; var t=new int[segments*6];
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float ripple=Mathf.Sin(a*11)*.12f;
                v[i*2]=direction*(radius+ripple-width*.5f)+Vector3.up*y;
                v[i*2+1]=direction*(radius+ripple+width*.5f)+Vector3.up*y;
                int k=i*6,j=i*2,next=(i+1)%segments*2;
                t[k]=j;t[k+1]=next;t[k+2]=j+1;t[k+3]=j+1;t[k+4]=next;t[k+5]=next+1;
            }
            var mesh=new Mesh{vertices=v,triangles=t};mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Palm(Transform root,Vector3 p,float height,float angle)
        {
            var tree=new GameObject("Coconut palm").transform;tree.SetParent(root,false);tree.position=p;
            for(int j=0;j<5;j++)
            {
                var part=Primitive("Trunk segment",PrimitiveType.Cylinder,tree,new Vector3(j*j*.024f,height*(j+.5f)/5,0),new Vector3(.30f-j*.025f,height/10+.02f,.30f-j*.025f),bark);
                part.transform.localRotation=Quaternion.Euler(0,0,-j*3);
            }
            Vector3 crown=new Vector3(.5f,height,0);
            // A combined tapered frond crown replaces stretched spheres: broad leaves, curved tips and readable edges.
            var crownObject=new GameObject("Мягкая крона пальмы",typeof(MeshFilter),typeof(MeshRenderer));
            crownObject.transform.SetParent(tree,false);crownObject.transform.localPosition=crown;
            crownObject.GetComponent<MeshFilter>().sharedMesh=PalmCrown(angle,2.25f,8);
            crownObject.GetComponent<MeshRenderer>().sharedMaterial=leaf;
            crownObject.AddComponent<WorldPalmSway>().Phase=angle;
            var young=new GameObject("Светлые молодые листья",typeof(MeshFilter),typeof(MeshRenderer));young.transform.SetParent(crownObject.transform,false);
            young.transform.localPosition=Vector3.up*.08f;young.transform.localRotation=Quaternion.Euler(0,24,0);
            young.GetComponent<MeshFilter>().sharedMesh=PalmCrown(angle,1.35f,4);young.GetComponent<MeshRenderer>().sharedMaterial=leafLight;
            for(int j=0;j<3;j++)Primitive("Coconut",PrimitiveType.Sphere,tree,crown+new Vector3(j*.16f-.16f,-.12f,.12f),Vector3.one*.24f,bark);
            Primitive("Palm grass",PrimitiveType.Sphere,tree,new Vector3(0,.025f,0),new Vector3(1.6f,.08f,1.5f),grass);
        }
        static Texture2D SandPalette()
        {
            const int n=256;var texture=new Texture2D(n,n,TextureFormat.RGB24,false){name="Painted vanilla sand",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n,v=(y+.5f)/n;float r=Vector2.Distance(new Vector2(u,v),Vector2.one*.5f)*2;
                float wet=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,r));float patches=Mathf.PerlinNoise(x*.025f+13,y*.025f+7);
                var color=Color.Lerp(new Color(1,.91f,.70f),new Color(.92f,.78f,.52f),wet*.62f);
                color*=.985f+patches*.035f;
                // Rare tiny darker grains, restrained enough to stay soft at gameplay distance.
                float grain=Mathf.PerlinNoise(x*.83f,y*.89f);color*=Mathf.Lerp(.965f,1.02f,grain);
                pixels[y*n+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        static Mesh PalmCrown(float angle,float length,int count)
        {
            const int slices=7;var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            for(int frond=0;frond<count;frond++){
                float a=angle+frond*Mathf.PI*2/count;var forward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,forward);
                int first=vertices.Count;
                for(int j=0;j<=slices;j++){
                    float t=j/(float)slices;float width=Mathf.Sin(t*Mathf.PI)*length*.18f;
                    Vector3 mid=forward*(length*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.46f-t*t*.61f);
                    vertices.Add(mid-side*width-Vector3.up*width*.17f);vertices.Add(mid+Vector3.up*.035f);vertices.Add(mid+side*width-Vector3.up*width*.17f);
                    if(j==slices)continue;
                    for(int strip=0;strip<2;strip++){
                        int k=first+j*3+strip;int next=k+3;
                        triangles.Add(k);triangles.Add(next);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(next);triangles.Add(next+1);
                    }
                }
            }
            var mesh=new Mesh{name="Curved cartoon palm fronds"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh BeachRockMesh()
        {
            // Rounded low-poly boulder, generated once per beach rock (only four in the scene).
            const int sides=9;var vertices=new Vector3[sides*3+2];vertices[0]=Vector3.down*.48f;vertices[vertices.Length-1]=Vector3.up*.48f;
            for(int ring=0;ring<3;ring++)for(int j=0;j<sides;j++){
                float a=(j+ring*.18f)*Mathf.PI*2/sides;float r=ring==1?.52f:.34f;
                vertices[1+ring*sides+j]=new Vector3(Mathf.Cos(a)*r*(1+Mathf.Sin(j*5.1f)*.07f),(ring-1)*.31f,Mathf.Sin(a)*r);
            }
            var t=new System.Collections.Generic.List<int>();
            for(int j=0;j<sides;j++){
                int next=(j+1)%sides;t.Add(0);t.Add(1+j);t.Add(1+next);
                for(int ring=0;ring<2;ring++){int k=1+ring*sides+j,n=1+ring*sides+next;t.Add(k);t.Add(k+sides);t.Add(n);t.Add(n);t.Add(k+sides);t.Add(n+sides);}
                t.Add(vertices.Length-1);t.Add(1+2*sides+next);t.Add(1+2*sides+j);
            }
            var mesh=new Mesh{name="Rounded coastal boulder",vertices=vertices,triangles=t.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void BeachPlants(Transform root,Vector3 position,float angle)
        {
            var patch=new GameObject("Прибрежные листья",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(root,false);patch.transform.localPosition=position;
            patch.GetComponent<MeshFilter>().sharedMesh=PalmCrown(angle,.67f,5);patch.GetComponent<MeshRenderer>().sharedMaterial=grass;
            patch.transform.localScale=new Vector3(1,.68f,1);patch.transform.localPosition+=Vector3.up*.13f;
        }
        static void Starfish(Transform root,Vector3 p,float angle)
        {
            var vertices=new Vector3[11];var triangles=new int[30];vertices[0]=Vector3.up*.035f;
            for(int j=0;j<10;j++){float a=angle+j*Mathf.PI/5;float r=j%2==0?.28f:.10f;vertices[j+1]=new Vector3(Mathf.Cos(a)*r,.01f,Mathf.Sin(a)*r);triangles[j*3]=0;triangles[j*3+1]=(j+1)%10+1;triangles[j*3+2]=j+1;}
            var mesh=new Mesh{name="Peach starfish",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();
            var go=new GameObject("Морская звезда",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.localPosition=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=coral;
        }
        public static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>();if(collider!=null&&name!="Beach rock"&&name!="Trunk segment")Object.Destroy(collider);
            return go;
        }
    }
}

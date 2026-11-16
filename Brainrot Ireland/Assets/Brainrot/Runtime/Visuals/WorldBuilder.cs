using UnityEngine;
using UnityEngine.Rendering;

namespace Brainrot
{
    /// <summary>Self-contained, deterministic tropical arena. Gameplay surface is the y=0 plane.</summary>
    public static class WorldBuilder
    {
        static Material sand, grass, bark, leaf, stone, foam, coral, leafLight, flower, flowerGold, shellMaterial, coconut;
        static Mesh beachRock,beachPlant,shellMesh;
        static WorldGeneratedResources resources;
        static T Own<T>(T value) where T:Object { if(resources!=null)resources.Own(value);return value; }
        static Material WorldMaterial(Color color,string name) {return Own(Material(color,name));}
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
            if (old != null){old.SetActive(false);Object.Destroy(old);}
            var root = new GameObject("Procedural Island").transform;
            resources=root.gameObject.AddComponent<WorldGeneratedResources>();
            sand = WorldMaterial(Color.white, "Vanilla sand — hand-painted procedural palette");
            sand.SetTexture("_BaseMap",SandPalette());
            grass = WorldMaterial(new Color(.51f,.78f,.10f), "Lime vegetation");
            bark = WorldMaterial(Color.white, "Golden ringed palm bark");bark.SetTexture("_BaseMap",BarkPalette());
            leaf = WorldMaterial(Color.white, "Glossy emerald palm leaves");leaf.SetTexture("_BaseMap",LeafPalette());leaf.SetFloat("_Smoothness",.34f);
            stone = WorldMaterial(Color.white, "Warm coastal granite with painted facets");
            stone.SetTexture("_BaseMap",StonePalette());stone.SetFloat("_Smoothness",.18f);
            foam = WorldMaterial(new Color(.96f,1,.96f), "Foam");
            coral=WorldMaterial(new Color(1,.30f,.075f),"Tangerine starfish");
            flower=WorldMaterial(new Color(1,.065f,.09f),"Red hibiscus petals");flowerGold=WorldMaterial(new Color(1,.71f,.03f),"Golden flower stamens");
            shellMaterial=WorldMaterial(Color.white,"Cream seashells with peach growth bands");shellMaterial.SetTexture("_BaseMap",ShellPalette());
            coconut=WorldMaterial(new Color(.64f,.32f,.09f),"Golden coconut husks");
            leafLight=WorldMaterial(new Color(.91f,1,.55f),"Young lime leaves");leafLight.SetTexture("_BaseMap",LeafPalette());leafLight.SetFloat("_Smoothness",.28f);
            beachRock=BeachRockMesh();beachPlant=PalmCrown(0,.9f,6);shellMesh=ShellMesh();
            leaf.SetFloat("_Cull",0);leafLight.SetFloat("_Cull",0);grass.SetFloat("_Cull",0);
            var rng = new System.Random(731);
            Island(root);
            // Physics support below the ocean: tumbling bodies cannot fall forever outside the sand.
            var support=new GameObject("Дно для физических тел",typeof(BoxCollider));support.transform.SetParent(root,false);support.transform.position=new Vector3(0,-.8f,0);support.GetComponent<BoxCollider>().size=new Vector3(160,.3f,160);
            var water = Shader.Find("Brainrot/TropicalWater");
            var ocean=GameBalance.Current.Ocean;
            var waterMat = water != null ? Own(new Material(water)) : WorldMaterial(ocean.DeepColor, "Ocean");
            waterMat.name="Bright turquoise ocean with white rolling surf";
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
                if (i % 3 != 0) Palm(root, p, 3.8f + (float)rng.NextDouble()*1.7f, a);
                else
                {
                    var rock = Primitive("Beach rock", PrimitiveType.Sphere, root, p + Vector3.up*.28f, new Vector3(1.9f,1.3f,1.5f), stone);
                    rock.GetComponent<MeshFilter>().sharedMesh=beachRock;
                    rock.transform.Rotate(12, a*Mathf.Rad2Deg, 23);
                }
            }
            for (int i=0; i<24; i++)
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2;
                float r=16+(float)rng.NextDouble()*5;
                var shell=Primitive("Ребристая ракушка",PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*r,.015f,Mathf.Sin(a)*r),Vector3.one*(.30f+(float)rng.NextDouble()*.20f),shellMaterial);
                shell.GetComponent<MeshFilter>().sharedMesh=shellMesh;
                shell.transform.Rotate(0,a*Mathf.Rad2Deg,0);
            }
            // Coastal clusters leave the central combat arena visually quiet and unobstructed.
            for(int i=0;i<12;i++){
                float a=i*Mathf.PI*2/12+.16f;float r=18.2f+(float)rng.NextDouble()*1.7f;
                BeachPlants(root,new Vector3(Mathf.Cos(a)*r,-.01f,Mathf.Sin(a)*r),a);
                if(i%3==0)Starfish(root,new Vector3(Mathf.Cos(a+.06f)*(r+1),.015f,Mathf.Sin(a+.06f)*(r+1)),a);
                if(i%2==0)Hibiscus(root,new Vector3(Mathf.Cos(a)*r,.16f,Mathf.Sin(a)*r),a);
            }
            DistantIslands(root);
            CombineStaticDecoration(root);
            Light sun=null;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional&&(old==null||!light.transform.IsChildOf(old.transform))){sun=light;break;}
            if(sun==null){sun=new GameObject("Тропическое солнце").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;}
            sun.name="Персиковое тропическое солнце";
            sun.color=new Color(1,.95f,.86f);sun.intensity=1.32f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.shadowBias=.025f;sun.shadowNormalBias=.13f;
            sun.transform.rotation=Quaternion.Euler(51,-38,0);RenderSettings.sun=sun;
            // Sky and sand bounce light keep silhouettes readable without extra shadow maps.
            var fill=new GameObject("Небесный заполняющий свет").AddComponent<Light>();fill.transform.SetParent(root,false);
            fill.type=LightType.Directional;fill.color=new Color(.66f,.84f,1);fill.intensity=.22f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(28,142,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=new Color(.54f,.74f,.95f);
            RenderSettings.ambientEquatorColor=new Color(.50f,.67f,.73f);
            RenderSettings.ambientGroundColor=new Color(.57f,.43f,.25f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.43f,.81f,.99f);RenderSettings.fogDensity=.0015f;
            RenderSettings.reflectionIntensity=.52f;
            var skyShader=Shader.Find("Brainrot/TropicalSky");
            if(skyShader!=null){var sky=Own(new Material(skyShader){name="Солнечное лазурное небо"});sky.SetColor("_Zenith",new Color(.035f,.43f,.94f));sky.SetColor("_Horizon",RenderSettings.fogColor);sky.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=sky;}
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
            var mesh=Own(new Mesh{ name="Sandy island disk", vertices=v, triangles=tris, colors=colors,uv=uv });
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go=new GameObject("Sand arena",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(parent,false); go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=sand; go.GetComponent<MeshCollider>().sharedMesh=mesh;
            Primitive("Island foundation",PrimitiveType.Cylinder,parent,new Vector3(0,-1.2f,0),new Vector3(44,1.08f,44),sand);
            ShoreSlope(parent);
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
            var mesh=Own(new Mesh{vertices=v,triangles=t});mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Palm(Transform root,Vector3 p,float height,float angle)
        {
            var tree=new GameObject("Кокосовая пальма").transform;tree.SetParent(root,false);tree.localPosition=p;
            tree.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);
            // One smoothly curved trunk with sculpted rings, rather than disconnected cylinders.
            const int rings=24,sides=10;var vertices=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[vertices.Length];var tris=new int[rings*sides*6];
            for(int j=0;j<=rings;j++)for(int k=0;k<=sides;k++){
                float t=j/(float)rings,a=k*Mathf.PI*2/sides;float radius=Mathf.Lerp(.29f,.16f,t)*(j%2==0?1.04f:.94f);
                int id=j*(sides+1)+k;vertices[id]=new Vector3(t*t*.92f+Mathf.Cos(a)*radius,height*t,Mathf.Sin(a)*radius);
                uv[id]=new Vector2(k/(float)sides,t*4);
                if(j==rings||k==sides)continue;int n=(j*sides+k)*6;
                tris[n]=id;tris[n+1]=id+sides+1;tris[n+2]=id+1;tris[n+3]=id+1;tris[n+4]=id+sides+1;tris[n+5]=id+sides+2;
            }
            var trunk=Own(new Mesh{name="Curved ringed palm trunk",vertices=vertices,triangles=tris,uv=uv});trunk.RecalculateNormals();trunk.RecalculateBounds();
            MeshObject("Ствол пальмы",tree,Vector3.zero,trunk,bark);
            var collision=tree.gameObject.AddComponent<CapsuleCollider>();collision.center=new Vector3(.30f,height*.5f,0);collision.height=height;collision.radius=.27f;
            Vector3 crown=new Vector3(.92f,height,0);
            var crownObject=MeshObject("Пышная крона пальмы",tree,crown,PalmCrown(angle,2.8f,10),leaf);
            crownObject.AddComponent<WorldPalmSway>().Phase=angle;
            var young=MeshObject("Светлые молодые листья",crownObject.transform,Vector3.up*.18f,PalmCrown(angle+.4f,1.85f,6),leafLight);
            young.transform.localRotation=Quaternion.Euler(0,24,0);
            for(int j=0;j<4;j++){float a=j*2.4f;Primitive("Кокос",PrimitiveType.Sphere,tree,crown+new Vector3(Mathf.Cos(a)*.24f,-.22f,Mathf.Sin(a)*.24f),new Vector3(.38f,.48f,.38f),coconut);}
            BeachPlants(tree,new Vector3(0,.02f,0),angle);
        }
        static GameObject MeshObject(string name,Transform parent,Vector3 position,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static Texture2D SandPalette()
        {
            // Half the former dimensions, packed upload and mipmaps: no 16 MB float-colour array
            // or unmipped grains shimmering as the camera moves. A quiet centre keeps telegraphs readable.
            const int n=512;var texture=Own(new Texture2D(n,n,TextureFormat.RGBA32,true){name="Warm fine sand with wet shoreline",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=2});
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n,v=(y+.5f)/n;float r=Vector2.Distance(new Vector2(u,v),Vector2.one*.5f)*2;
                float wet=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1.03f,r));
                float patches=Mathf.PerlinNoise(x*.014f+13,y*.014f+7);
                var color=Color.Lerp(new Color(1,.81f,.51f),new Color(.69f,.48f,.25f),wet*.56f);
                color*=.95f+patches*.08f;
                float grain=Mathf.PerlinNoise(x*1.63f,y*1.71f);color*=Mathf.Lerp(.95f,1.045f,grain);
                float ripple=Mathf.Sin(x*.36f+Mathf.PerlinNoise(x*.024f,y*.024f)*8+y*.08f);color*=1+ripple*.011f;
                pixels[y*n+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }
        static Texture2D StonePalette()
        {
            const int n=128;var texture=Own(new Texture2D(n,n,TextureFormat.RGBA32,true){name="Warm granite with lavender mineral flecks",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear});
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float t=y/(float)(n-1),cloud=Mathf.PerlinNoise(x*.048f+3,y*.051f+8),grain=Mathf.PerlinNoise(x*.7f,y*.7f);
                Color color=Color.Lerp(new Color(.43f,.48f,.52f),new Color(.78f,.74f,.65f),Mathf.SmoothStep(0,1,t));
                color=Color.Lerp(color,new Color(.61f,.55f,.67f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.88f,cloud))*.24f);
                color*=.93f+cloud*.10f+grain*.06f;pixels[y*n+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }
        static Texture2D ShellPalette()
        {
            const int n=128;var texture=Own(new Texture2D(n,n,TextureFormat.RGBA32,true){name="Peach scallop growth rings",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear});
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=x/(float)(n-1),v=y/(float)(n-1);
                float ring=Mathf.Pow(Mathf.Max(0,Mathf.Sin(v*Mathf.PI*12)),5)*.24f;
                float ridge=Mathf.Pow(Mathf.Abs(Mathf.Sin(u*Mathf.PI*12)),6)*.09f;
                Color color=Color.Lerp(new Color(1,.92f,.76f),new Color(.93f,.60f,.44f),ring+ridge+(1-v)*.12f);
                pixels[y*n+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }
        static void ShoreSlope(Transform parent)
        {
            // The arena remains the existing plane. This visual-only skirt hides its vertical foundation
            // and actually meets the sea, so the foam is no longer drawn underneath an opaque beach rim.
            const int segments=128,rings=3;var vertices=new Vector3[(segments+1)*rings];var uv=new Vector2[vertices.Length];var tris=new int[segments*(rings-1)*6];
            for(int j=0;j<rings;j++)for(int i=0;i<=segments;i++){
                float a=i*Mathf.PI*2/segments,coast=22+Mathf.Sin(a*5)*.3f+Mathf.Cos(a*9)*.17f;
                float radius=coast+(j==0?-.03f:j==1?.45f:1.5f),height=j==0?-.109f:j==1?-.39f:-.70f;
                int id=j*(segments+1)+i;vertices[id]=new Vector3(Mathf.Cos(a)*radius,height,Mathf.Sin(a)*radius);
                uv[id]=new Vector2(vertices[id].x/45+.5f,vertices[id].z/45+.5f);
                if(i==segments||j==rings-1)continue;int k=(j*segments+i)*6,next=id+segments+1;
                tris[k]=id;tris[k+1]=id+1;tris[k+2]=next;tris[k+3]=id+1;tris[k+4]=next+1;tris[k+5]=next;
            }
            var mesh=Own(new Mesh{name="Smooth wet sand shore slope",vertices=vertices,triangles=tris,uv=uv});mesh.RecalculateNormals();mesh.RecalculateBounds();
            MeshObject("Пологий мокрый берег",parent,Vector3.zero,mesh,sand);
        }
        static Mesh PalmCrown(float angle,float length,int count)
        {
            const int slices=14;var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();var uv=new System.Collections.Generic.List<Vector2>();
            for(int frond=0;frond<count;frond++){
                float a=angle+frond*Mathf.PI*2/count;var forward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,forward);
                int first=vertices.Count;float lift=.55f+Mathf.Sin(frond*2.1f)*.12f;
                for(int j=0;j<=slices;j++){
                    float t=j/(float)slices;float width=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.8f)*length*.19f*(j%2==0?1:.87f);
                    Vector3 mid=forward*(length*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*lift-t*t*.76f);
                    vertices.Add(mid-side*width-Vector3.up*width*.30f);vertices.Add(mid+Vector3.up*.045f);vertices.Add(mid+side*width-Vector3.up*width*.30f);
                    uv.Add(new Vector2(0,t));uv.Add(new Vector2(.5f,t));uv.Add(new Vector2(1,t));
                    if(j==slices)continue;
                    for(int strip=0;strip<2;strip++){
                        int k=first+j*3+strip;int next=k+3;
                        triangles.Add(k);triangles.Add(next);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(next);triangles.Add(next+1);
                    }
                }
            }
            var mesh=Own(new Mesh{name="Broad ribbed cartoon palm fronds"});mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Texture2D LeafPalette()
        {
            const int n=128;var tex=Own(new Texture2D(n,n,TextureFormat.RGB24,true){name="Painted lime leaf ribs",wrapMode=TextureWrapMode.Clamp});var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=x/(float)(n-1),v=y/(float)(n-1),edge=Mathf.Abs(u-.5f)*2;
                var c=Color.Lerp(new Color(.43f,.79f,.035f),new Color(.10f,.40f,.025f),edge*.70f);
                c=Color.Lerp(c,new Color(.76f,.94f,.12f),(1-Mathf.SmoothStep(0,1,Mathf.Abs(u-.5f)/.045f))*.8f);
                float vein=Mathf.Pow(Mathf.Max(0,Mathf.Cos((v-edge*.18f)*Mathf.PI*22)),12);c*=.96f+vein*.10f;
                pixels[y*n+x]=c;
            }
            tex.SetPixels(pixels);tex.Apply(true,true);return tex;
        }
        static Texture2D BarkPalette()
        {
            const int n=128;var tex=Own(new Texture2D(n,n,TextureFormat.RGB24,true){name="Painted coconut bark bands",wrapMode=TextureWrapMode.Repeat});var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float ring=Mathf.Pow(Mathf.Max(0,Mathf.Cos(y/(float)n*Mathf.PI*6)),16);
                var c=Color.Lerp(new Color(.78f,.47f,.20f),new Color(.43f,.24f,.10f),ring*.70f);
                c*=.91f+Mathf.PerlinNoise(x*.13f,y*.10f)*.17f;pixels[y*n+x]=c;
            }
            tex.SetPixels(pixels);tex.Apply(true,true);return tex;
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
            var uv=new Vector2[vertices.Length];for(int i=0;i<vertices.Length;i++)uv[i]=new Vector2(Mathf.Atan2(vertices[i].z,vertices[i].x)/(Mathf.PI*2)+.5f,vertices[i].y+.5f);
            var mesh=Own(new Mesh{name="Rounded coastal boulder",vertices=vertices,triangles=t.ToArray(),uv=uv});mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void BeachPlants(Transform root,Vector3 position,float angle)
        {
            var patch=new GameObject("Прибрежные листья",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(root,false);patch.transform.localPosition=position;
            patch.GetComponent<MeshFilter>().sharedMesh=beachPlant;patch.GetComponent<MeshRenderer>().sharedMaterial=leafLight;
            patch.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);
            patch.transform.localScale=new Vector3(1.4f,1.05f,1.4f);patch.transform.localPosition+=Vector3.up*.13f;
        }
        static void Starfish(Transform root,Vector3 p,float angle)
        {
            var vertices=new Vector3[11];var triangles=new int[30];vertices[0]=Vector3.up*.035f;
            for(int j=0;j<10;j++){float a=angle+j*Mathf.PI/5;float r=j%2==0?.40f:.14f;vertices[j+1]=new Vector3(Mathf.Cos(a)*r,.01f,Mathf.Sin(a)*r);triangles[j*3]=0;triangles[j*3+1]=(j+1)%10+1;triangles[j*3+2]=j+1;}
            var mesh=Own(new Mesh{name="Peach starfish",vertices=vertices,triangles=triangles});mesh.RecalculateNormals();
            var go=new GameObject("Морская звезда",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.localPosition=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=coral;
        }
        static Mesh ShellMesh()
        {
            const int rows=8,sides=24;var v=new Vector3[(rows+1)*(sides+1)];var tri=new int[(rows*2-1)*sides*3];var uv=new Vector2[v.Length];int nextTriangle=0;
            for(int j=0;j<=rows;j++)for(int k=0;k<=sides;k++){
                float t=j/(float)rows,a=(k/(float)sides-.5f)*Mathf.PI*1.2f,r=t*(1+.035f*Mathf.Cos(k*Mathf.PI));int id=j*(sides+1)+k;
                v[id]=new Vector3(Mathf.Sin(a)*r,Mathf.Sin(t*Mathf.PI)*.43f*(.90f+.10f*Mathf.Cos(k*Mathf.PI)),Mathf.Cos(a)*r-.3f);uv[id]=new Vector2(k/(float)sides,t);
                if(j==rows||k==sides)continue;
                if(j>0){tri[nextTriangle++]=id;tri[nextTriangle++]=id+sides+1;tri[nextTriangle++]=id+1;}
                tri[nextTriangle++]=id+1;tri[nextTriangle++]=id+sides+1;tri[nextTriangle++]=id+sides+2;
            }
            var m=Own(new Mesh{name="Scalloped sculpted seashell",vertices=v,triangles=tri,uv=uv});m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void Hibiscus(Transform root,Vector3 p,float angle)
        {
            var plant=new GameObject("Красный гибискус").transform;plant.SetParent(root,false);plant.localPosition=p;plant.localRotation=Quaternion.Euler(-20,angle*Mathf.Rad2Deg,0);
            for(int j=0;j<3;j++){
                var foliage=MeshObject("Листья гибискуса",plant,new Vector3((j-1)*.4f,.1f,0),beachPlant,leaf);
                foliage.transform.localScale=Vector3.one*1.15f;foliage.transform.localRotation=Quaternion.Euler(-25,j*70,20);
            }
            Vector3 center=new Vector3(0,.60f,0);
            for(int j=0;j<5;j++){
                float a=j*Mathf.PI*2/5;var petal=Primitive("Лепесток гибискуса",PrimitiveType.Sphere,plant,center+new Vector3(Mathf.Cos(a)*.24f,.02f,Mathf.Sin(a)*.24f),new Vector3(.42f,.10f,.55f),flower);
                petal.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);
            }
            Primitive("Сердцевина гибискуса",PrimitiveType.Sphere,plant,center+Vector3.up*.07f,Vector3.one*.13f,flowerGold);
            Primitive("Тычинка гибискуса",PrimitiveType.Capsule,plant,center+Vector3.up*.22f,new Vector3(.035f,.16f,.035f),flowerGold);
        }
        static void DistantIslands(Transform root)
        {
            // Decorative backdrop is beyond the spawn ring and has no physics or navigation objects.
            for(int island=0;island<5;island++){
                float a=.6f+island*Mathf.PI*2/5,distance=155+island%2*55;var center=new Vector3(Mathf.Cos(a)*distance,-1.3f,Mathf.Sin(a)*distance);
                var group=new GameObject("Дальний тропический остров").transform;group.SetParent(root,false);group.localPosition=center;
                for(int j=0;j<5;j++){
                    float height=7+(j==2?12:Mathf.Sin(j*3+island)*3),x=(j-2)*6;var rock=MeshObject("Скалы на горизонте",group,new Vector3(x,height*.24f,j%2*3),beachRock,stone);
                    rock.transform.localScale=new Vector3(13,height,11);rock.transform.localRotation=Quaternion.Euler(8,j*37,7);
                    var green=MeshObject("Зелень дальнего острова",group,new Vector3(x,height*.60f,j%2*3),beachRock,grass);green.transform.localScale=new Vector3(9,2.2f,7);
                }
            }
        }
        static void CombineStaticDecoration(Transform root)
        {
            // Keep swaying crowns separate; combine fixed decoration by material in 32 m cells.
            // Collider transforms remain intact; only the renderer is replaced. No per-frame batching work.
            var groups=new System.Collections.Generic.Dictionary<(Material material,int x,int z),System.Collections.Generic.List<CombineInstance>>();
            var renderers=new System.Collections.Generic.List<MeshRenderer>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
                if(filter.sharedMesh==null||filter.GetComponentInParent<WorldPalmSway>()!=null)continue;
                if(filter.name=="Sand arena"||filter.name=="Island foundation"||filter.name=="Endless ocean")continue;
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||renderer.sharedMaterial==null)continue;
                // Spatial batches retain culling between the beach and each distant island.
                Vector3 location=filter.transform.position;var key=(renderer.sharedMaterial,Mathf.FloorToInt(location.x/32),Mathf.FloorToInt(location.z/32));
                if(!groups.TryGetValue(key,out var list)){list=new System.Collections.Generic.List<CombineInstance>();groups.Add(key,list);}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderers.Add(renderer);
            }
            foreach(var pair in groups){
                var combined=Own(new Mesh{name="Combined tropical decoration: "+pair.Key.material.name,indexFormat=IndexFormat.UInt32});combined.CombineMeshes(pair.Value.ToArray(),true,true);combined.RecalculateBounds();
                MeshObject("Декорации — "+pair.Key.material.name,root,Vector3.zero,combined,pair.Key.material);
            }
            foreach(var renderer in renderers)renderer.enabled=false;
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

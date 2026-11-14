using UnityEngine;
using UnityEngine.Rendering;

namespace Brainrot
{
    /// <summary>Self-contained, deterministic tropical arena. Gameplay surface is the y=0 plane.</summary>
    public static class WorldBuilder
    {
        static Material sand, grass, bark, leaf, stone, foam;
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
            sand = Material(new Color(.98f,.82f,.52f), "Honey sand");
            grass = Material(new Color(.38f,.72f,.23f), "Lime vegetation");
            bark = Material(new Color(.49f,.28f,.12f), "Palm trunk");
            leaf = Material(new Color(.12f,.55f,.28f), "Palm leaves");
            stone = Material(new Color(.35f,.48f,.52f), "Coastal slate");
            foam = Material(new Color(.87f,.98f,.92f), "Foam");
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
            Light sun=null;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun==null){sun=new GameObject("Тропическое солнце").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;}
            sun.color=new Color(1,.95f,.84f);sun.intensity=1.15f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.74f;sun.shadowBias=.045f;sun.shadowNormalBias=.22f;sun.transform.rotation=Quaternion.Euler(46,-34,0);
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=new Color(.54f,.74f,.87f);
            RenderSettings.ambientEquatorColor=new Color(.50f,.66f,.67f);
            RenderSettings.ambientGroundColor=new Color(.45f,.37f,.24f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.62f,.83f,.90f);RenderSettings.fogDensity=.0027f;
            RenderSettings.reflectionIntensity=.45f;
            var skyShader=Shader.Find("Brainrot/TropicalSky");
            if(skyShader!=null){var sky=new Material(skyShader){name="Тропическое небо"};sky.SetColor("_Zenith",new Color(.12f,.44f,.76f));sky.SetColor("_Horizon",RenderSettings.fogColor);sky.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=sky;}
        }

        static void Island(Transform parent)
        {
            const int n=128;
            var v=new Vector3[n+1]; var colors=new Color[n+1]; var tris=new int[n*3];
            v[0]=new Vector3(0,-.025f,0); colors[0]=new Color(1,.88f,.59f);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n;
                float r=22f+Mathf.Sin(a*5)*.3f+Mathf.Cos(a*9)*.17f;
                v[i+1]=new Vector3(Mathf.Cos(a)*r,-.11f,Mathf.Sin(a)*r);
                colors[i+1]=new Color(.93f,.70f,.33f);
                tris[i*3]=0; tris[i*3+1]=(i+1)%n+1; tris[i*3+2]=i+1;
            }
            var mesh=new Mesh { name="Sandy island disk", vertices=v, triangles=tris, colors=colors };
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
            for(int j=0;j<7;j++)
            {
                float a=angle+j*Mathf.PI*2/7;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                var frond=Primitive("Palm frond",PrimitiveType.Sphere,tree,crown+dir*.95f-Vector3.up*.12f,new Vector3(.48f,.12f,2.8f),leaf);
                frond.transform.localRotation=Quaternion.LookRotation(dir)*Quaternion.Euler(12,0,0);
            }
            for(int j=0;j<3;j++)Primitive("Coconut",PrimitiveType.Sphere,tree,crown+new Vector3(j*.16f-.16f,-.12f,.12f),Vector3.one*.24f,bark);
            Primitive("Palm grass",PrimitiveType.Sphere,tree,new Vector3(0,.025f,0),new Vector3(1.6f,.08f,1.5f),grass);
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

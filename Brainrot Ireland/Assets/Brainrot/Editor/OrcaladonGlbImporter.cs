using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot.Editor
{
    [InitializeOnLoad]
    static class OrcaladonSourceRefresh
    {
        static bool queued;
        static OrcaladonSourceRefresh(){EditorApplication.delayCall+=Refresh;}
        static void Refresh(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Refresh;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode||queued||!File.Exists(OrcaladonGlbImporter.Source))return;
            queued=true;
            try{
                string stamp="Assets/Brainrot/Generated/Enemies/OrcaladonNew/source.sha256";
                string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(OrcaladonGlbImporter.Source)))+"|"+OrcaladonOptimizedGeometry.Signature;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Brainrot/Generated/Resources/ImportedOrcaladon.prefab");
                if(File.Exists(stamp)&&File.ReadAllText(stamp)==hash&&prefab!=null&&prefab.GetComponent<OrcaladonVisual>()!=null&&prefab.GetComponent<OrcaladonVisual>().NeutralArmsDown)return;
                EnemyModelImporter.BuildOrcaladon();
                prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Brainrot/Generated/Resources/ImportedOrcaladon.prefab");
                if(prefab!=null&&prefab.GetComponent<OrcaladonVisual>()!=null&&prefab.GetComponent<OrcaladonVisual>().NeutralArmsDown)File.WriteAllText(stamp,hash);
            }catch(Exception exception){Debug.LogError("Не удалось обновить новую модель Оркаладона: "+exception.Message);}
            finally{queued=false;}
        }
    }
    /// <summary>Imports the owner's plain, uncompressed GLB without a third-party runtime package.</summary>
    public static class OrcaladonGlbImporter
    {
        public const string Source="Assets/Brainrot/Art/Enemies/Orcaladon/OrcaladonNew.glb";
        const string Root="Assets/Brainrot/Generated/Enemies/OrcaladonNew/";
        [Serializable] sealed class Document {public Accessor[] accessors;public View[] bufferViews;public MeshData[] meshes;public Picture[] images;}
        [Serializable] sealed class Accessor {public int bufferView,byteOffset,componentType,count;public string type;}
        [Serializable] sealed class View {public int byteOffset,byteLength,byteStride;}
        [Serializable] sealed class MeshData {public Primitive[] primitives;}
        [Serializable] sealed class Primitive {public Attributes attributes;public int indices,mode=4;}
        [Serializable] sealed class Attributes {public int POSITION,NORMAL,TEXCOORD_0;}
        [Serializable] sealed class Picture {public int bufferView;}
        static Document doc;static byte[] data;static int bin;
        static int Offset(int id,int index,int components){var a=doc.accessors[id];var v=doc.bufferViews[a.bufferView];return checked(bin+v.byteOffset+a.byteOffset+index*(v.byteStride>0?v.byteStride:components*(a.componentType==5123?2:4)));}
        static float Float(int id,int index,int component,int components){var a=doc.accessors[id];if(a.componentType!=5126)throw new InvalidDataException("Ожидались float-координаты GLB.");return BitConverter.ToSingle(data,Offset(id,index,components)+component*4);}
        public static GameObject Build(out Texture2D texture){
            texture=null;if(!File.Exists(Source))return null;
            data=File.ReadAllBytes(Source);
            if(data.Length<28||BitConverter.ToUInt32(data,0)!=0x46546c67||BitConverter.ToUInt32(data,4)!=2)throw new InvalidDataException("Неверный GLB Оркаладона.");
            int jsonLength=checked((int)BitConverter.ToUInt32(data,12));doc=JsonUtility.FromJson<Document>(Encoding.UTF8.GetString(data,20,jsonLength));bin=checked(20+jsonLength+8);
            if(doc==null||doc.meshes==null||doc.meshes.Length!=1||doc.meshes[0].primitives.Length!=1)throw new InvalidDataException("Ожидалась одна сетка нового Оркаладона.");
            var primitive=doc.meshes[0].primitives[0];if(primitive.mode!=4)throw new InvalidDataException("GLB должен содержать треугольники.");
            Vector3[] positions,normals;Vector2[] uv;int[] triangles;
            if(!OrcaladonOptimizedGeometry.TryLoad(data,out positions,out normals,out uv,out triangles)){
            var at=primitive.attributes;int sourceCount=doc.accessors[at.POSITION].count;positions=new Vector3[sourceCount];normals=new Vector3[sourceCount];uv=new Vector2[sourceCount];
            for(int i=0;i<sourceCount;i++){
                // The source faces -X. Reflect/rotate into Unity's +Z forward, +Y up.
                positions[i]=new Vector3(-Float(at.POSITION,i,2,3),Float(at.POSITION,i,1,3),-Float(at.POSITION,i,0,3));
                normals[i]=new Vector3(-Float(at.NORMAL,i,2,3),Float(at.NORMAL,i,1,3),-Float(at.NORMAL,i,0,3));uv[i]=new Vector2(Float(at.TEXCOORD_0,i,0,2),1-Float(at.TEXCOORD_0,i,1,2));
            }
            var indicesAccessor=doc.accessors[primitive.indices];triangles=new int[indicesAccessor.count];
            for(int i=0;i<triangles.Length;i++){int o=Offset(primitive.indices,i,1);triangles[i]=indicesAccessor.componentType==5125?checked((int)BitConverter.ToUInt32(data,o)):indicesAccessor.componentType==5123?BitConverter.ToUInt16(data,o):throw new InvalidDataException("Неверный индекс GLB.");}
            }
            int count=positions.Length;var bounds=new Bounds(positions[0],Vector3.zero);foreach(var position in positions)bounds.Encapsulate(position);
            float scale=2.6f/Mathf.Max(.001f,bounds.size.y);var center=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            for(int i=0;i<count;i++)positions[i]=(positions[i]-center)*scale;
            for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            Directory.CreateDirectory(Root);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var mesh=new Mesh{name="OrcaladonNew — исходная геометрия с мягким ригом",indexFormat=count<=65535?IndexFormat.UInt16:IndexFormat.UInt32};mesh.vertices=positions;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;
            var model=new GameObject("OrcaladonNew");
            string[] names={"Body","UpperArm.L","UpperArm.R","Forearm.L","Forearm.R","Thigh.L","Thigh.R","Shin.L","Shin.R"};
            Vector3[] pivots={new Vector3(0,1,0),new Vector3(.72f,1.28f,.45f),new Vector3(-.72f,1.28f,.45f),new Vector3(.91f,.64f,.70f),new Vector3(-.91f,.64f,.70f),new Vector3(.65f,.85f,-.65f),new Vector3(-.65f,.85f,-.65f),new Vector3(.70f,.36f,-.65f),new Vector3(-.70f,.36f,-.65f)};
            var bones=new Transform[names.Length];int[] parents={-1,0,0,1,2,0,0,5,6};
            for(int i=0;i<names.Length;i++){bones[i]=new GameObject("Orca_"+names[i]).transform;bones[i].SetParent(parents[i]<0?model.transform:bones[parents[i]],false);bones[i].position=pivots[i];}
            var weights=new BoneWeight[count];var bind=new Matrix4x4[bones.Length];
            for(int i=0;i<bones.Length;i++)bind[i]=bones[i].worldToLocalMatrix*model.transform.localToWorldMatrix;
            for(int i=0;i<count;i++){
                var p=positions[i];bool left=p.x>0;float side=Mathf.Abs(p.x);
                float arm=Smooth(.40f,.84f,side)*Smooth(-.12f,.35f,p.z)*(1-Smooth(1.25f,1.65f,p.y));
                float leg=Smooth(.28f,.62f,side)*(1-Smooth(-.35f,.05f,p.z))*Smooth(-1.35f,-.95f,p.z)*(1-Smooth(.75f,1.15f,p.y))*(1-arm);
                float fore=1-Smooth(.50f,.90f,p.y),shin=1-Smooth(.30f,.58f,p.y);
                if(arm>=leg)weights[i]=new BoneWeight{boneIndex0=0,weight0=1-arm,boneIndex1=left?1:2,weight1=arm*(1-fore),boneIndex2=left?3:4,weight2=arm*fore};
                else weights[i]=new BoneWeight{boneIndex0=0,weight0=1-leg,boneIndex1=left?5:6,weight1=leg*(1-shin),boneIndex2=left?7:8,weight2=leg*shin};
            }
            mesh.boneWeights=weights;mesh.bindposes=bind;mesh.RecalculateBounds();var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"OrcaladonNew.asset");
            if(existing==null)AssetDatabase.CreateAsset(mesh,Root+"OrcaladonNew.asset");else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            var renderer=model.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=bones;renderer.rootBone=bones[0];renderer.localBounds=mesh.bounds;renderer.updateWhenOffscreen=true;
            var image=doc.bufferViews[doc.images[0].bufferView];var png=new byte[image.byteLength];Buffer.BlockCopy(data,bin+image.byteOffset,png,0,png.Length);File.WriteAllBytes(Root+"OrcaladonNew.jpg",png);AssetDatabase.ImportAsset(Root+"OrcaladonNew.jpg",ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(Root+"OrcaladonNew.jpg") as TextureImporter;if(importer!=null&&SahurModelImporter.ConfigureTexture2D(importer,false,true,2048))importer.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"OrcaladonNew.jpg");data=null;doc=null;return model;
        }
        static float Smooth(float a,float b,float value){float t=Mathf.Clamp01((value-a)/(b-a));return t*t*(3-2*t);}
    }
}

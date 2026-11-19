using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Editor-only UV-preserving derivative. The untouched owner GLB remains the authoritative source.</summary>
    public static class OrcaladonOptimizedGeometry
    {
        const string Root="Assets/Brainrot/Generated/Editor/OrcaOptimized/";
        [Serializable] sealed class Manifest {public int version,decoded_bytes,vertices,triangles;public string source_sha256,sha256;public string[] chunks;}
        public static string Signature=>File.Exists(Root+"manifest.json")?File.ReadAllText(Root+"manifest.json"):"source-only";
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static bool TryLoad(byte[] source,out Vector3[] positions,out Vector3[] normals,out Vector2[] uv,out int[] triangles){
            positions=normals=null;uv=null;triangles=null;
            if(!File.Exists(Root+"manifest.json"))return false;
            var manifest=JsonUtility.FromJson<Manifest>(Signature);
            if(manifest==null||manifest.version!=1)throw new InvalidDataException("Неверный пакет геометрии Оркаладона.");
            // A newly supplied model must never inherit the old model's derivative.
            if(manifest.source_sha256!=Hash(source))return false;
            if(manifest.vertices<3||manifest.vertices>65535||manifest.triangles<1||manifest.triangles>100000||manifest.chunks==null||manifest.chunks.Length<1||manifest.chunks.Length>128||manifest.decoded_bytes!=36+manifest.vertices*16+manifest.triangles*6)
                throw new InvalidDataException("Неверный бюджет геометрии Оркаладона.");
            var text=new StringBuilder();
            for(int i=0;i<manifest.chunks.Length;i++){
                string expected=i.ToString("000")+".b64";if(manifest.chunks[i]!=expected)throw new InvalidDataException("Неверный порядок частей геометрии.");
                string part=File.ReadAllText(Root+expected);if(part.Length>32768)throw new InvalidDataException("Слишком большая часть геометрии.");text.Append(part);
            }
            byte[] packed=Convert.FromBase64String(text.ToString());byte[] bytes=new byte[manifest.decoded_bytes];
            using(var input=new MemoryStream(packed))using(var gzip=new GZipStream(input,CompressionMode.Decompress)){
                int offset=0;while(offset<bytes.Length){int read=gzip.Read(bytes,offset,bytes.Length-offset);if(read<=0)throw new InvalidDataException("Обрезанная геометрия.");offset+=read;}
                if(gzip.ReadByte()!=-1)throw new InvalidDataException("Лишние данные геометрии.");
            }
            if(Hash(bytes)!=manifest.sha256)throw new InvalidDataException("Контрольная сумма геометрии не совпадает.");
            using(var reader=new BinaryReader(new MemoryStream(bytes))){
                if(reader.ReadUInt32()!=0x314d504f||reader.ReadInt32()!=manifest.vertices||reader.ReadInt32()!=manifest.triangles*3)throw new InvalidDataException("Неверный заголовок геометрии.");
                var low=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());var high=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                positions=new Vector3[manifest.vertices];normals=new Vector3[manifest.vertices];uv=new Vector2[manifest.vertices];
                for(int i=0;i<positions.Length;i++){
                    float x=Mathf.Lerp(low.x,high.x,reader.ReadUInt16()/65535f),y=Mathf.Lerp(low.y,high.y,reader.ReadUInt16()/65535f),z=Mathf.Lerp(low.z,high.z,reader.ReadUInt16()/65535f);
                    float nx=reader.ReadInt16()/32767f,ny=reader.ReadInt16()/32767f,nz=reader.ReadInt16()/32767f;
                    positions[i]=new Vector3(-z,y,-x);normals[i]=new Vector3(-nz,ny,-nx).normalized;uv[i]=new Vector2(reader.ReadUInt16()/65535f,1-reader.ReadUInt16()/65535f);
                }
                triangles=new int[manifest.triangles*3];for(int i=0;i<triangles.Length;i++){int index=reader.ReadUInt16();if(index>=positions.Length)throw new InvalidDataException("Индекс вне сетки.");triangles[i]=index;}
            }
            return true;
        }
    }
}

"""Bake an Editor-only, UV-preserving Orcaladon derivative. Requires Blender only to rebake.
The original owner GLB is never rewritten. Unity restores the geometry once during import.
"""
import argparse, base64, gzip, hashlib, json, math, struct, subprocess, tempfile, uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/Brainrot/Art/Enemies/Orcaladon/OrcaladonNew.glb'
OUTPUT=ROOT/'Assets/Brainrot/Generated/Editor/OrcaOptimized'

def load_glb(path):
    data=Path(path).read_bytes(); length=struct.unpack_from('<I',data,12)[0]
    doc=json.loads(data[20:20+length]); binary=data[28+length:]
    primitive=doc['meshes'][0]['primitives'][0]
    def values(accessor):
        a=doc['accessors'][accessor];v=doc['bufferViews'][a['bufferView']]
        count={'SCALAR':1,'VEC2':2,'VEC3':3}[a['type']];fmt={5126:'f',5125:'I',5123:'H'}[a['componentType']]
        size=struct.calcsize('<'+fmt)*count;offset=v.get('byteOffset',0)+a.get('byteOffset',0)
        return [struct.unpack_from('<'+fmt*count,binary,offset+i*v.get('byteStride',size)) for i in range(a['count'])]
    attrs=primitive['attributes'];return values(attrs['POSITION']),values(attrs['NORMAL']),values(attrs['TEXCOORD_0']),[x[0] for x in values(primitive['indices'])]

def encode_geometry(positions,normals,uv,indices):
    n=len(positions)
    if n<3 or n>65535 or len(normals)!=n or len(uv)!=n or len(indices)%3:raise ValueError('Invalid mesh dimensions')
    low=[min(v[i] for v in positions) for i in range(3)];high=[max(v[i] for v in positions) for i in range(3)]
    if any(not math.isfinite(x) for row in positions+normals+uv for x in row):raise ValueError('Nonfinite mesh')
    if any(x<0 or x>=n for x in indices):raise ValueError('Index outside mesh')
    out=bytearray(struct.pack('<4sII6f',b'OPM1',n,len(indices),*low,*high))
    for p,normal,t in zip(positions,normals,uv):
        q=[round((p[i]-low[i])/max(1e-12,high[i]-low[i])*65535) for i in range(3)]
        r=[round(max(-1,min(1,x))*32767) for x in normal]
        if any(x<0 or x>1 for x in t):raise ValueError('UV outside unit interval')
        out.extend(struct.pack('<3H3h2H',*q,*r,*(round(x*65535) for x in t)))
    out.extend(struct.pack('<'+'H'*len(indices),*indices));return bytes(out)

def meta(path,folder=False):
    target=Path(str(path)+'.meta')
    if not target.exists():target.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+('folderAsset: yes\n' if folder else ''))

def bake(glb):
    p,n,u,t=load_glb(glb);raw=encode_geometry(p,n,u,t);packed=gzip.compress(raw,compresslevel=9,mtime=0)
    OUTPUT.mkdir(parents=True,exist_ok=True);meta(OUTPUT,True)
    encoded=base64.b64encode(packed).decode('ascii');chunks=[]
    for i,start in enumerate(range(0,len(encoded),32768)):
        name=f'{i:03}.b64';path=OUTPUT/name;path.write_text(encoded[start:start+32768],encoding='ascii');meta(path);chunks.append(name)
    manifest=dict(version=1,source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),sha256=hashlib.sha256(raw).hexdigest(),decoded_bytes=len(raw),vertices=len(p),triangles=len(t)//3,chunks=chunks)
    path=OUTPUT/'manifest.json';path.write_text(json.dumps(manifest,indent=2)+'\n');meta(path)
    print(json.dumps({**manifest,'gzip_bytes':len(packed),'parts':len(chunks)}))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--from-glb',type=Path);parser.add_argument('--blender',default='blender');args=parser.parse_args()
    if args.from_glb:bake(args.from_glb)
    else:
        with tempfile.TemporaryDirectory(prefix='orca-opt-') as directory:
            target=Path(directory)/'mesh.glb';script=Path(directory)/'bake.py'
            script.write_text("import bpy\nbpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)\nbpy.ops.import_scene.gltf(filepath="+repr(str(SOURCE))+")\no=next(x for x in bpy.context.scene.objects if x.type=='MESH');bpy.context.view_layer.objects.active=o\nm=o.modifiers.new('UV preserving reduction','DECIMATE');m.ratio=.20;m.use_collapse_triangulate=True\nbpy.ops.object.modifier_apply(modifier=m.name)\nbpy.ops.export_scene.gltf(filepath="+repr(str(target))+",export_format='GLB',export_materials='NONE',export_yup=True,export_apply=True)\n")
            subprocess.run([args.blender,'-b','--python',str(script)],check=True);bake(target)

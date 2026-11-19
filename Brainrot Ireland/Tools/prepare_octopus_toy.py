"""Run with Blender: blender -b --python Tools/prepare_octopus_toy.py -- source.glb output_dir
Recreates the lightweight static FBX; runtime eyes/sway are built by EnemyModelImporter.
"""
import json
import math
from pathlib import Path
import struct
import sys
import bpy
from mathutils import Matrix

source, output = map(Path, sys.argv[sys.argv.index('--') + 1:])
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source.resolve()))
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bpy.context.view_layer.objects.active = obj
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
modifier = obj.modifiers.new('Playable toy mesh', 'DECIMATE')
modifier.ratio = min(1.0, 24000 / max(1, len(obj.data.polygons)))
bpy.ops.object.modifier_apply(modifier=modifier.name)
# The provided model faces +X in Blender; Unity's convention requires Blender -Y.
obj.data.transform(Matrix.Rotation(-math.pi / 2, 4, 'Z'))
obj.name = 'OctopusToy'
obj.data.name = 'OctopusToyMesh'
for polygon in obj.data.polygons:
    polygon.use_smooth = True
bpy.ops.export_scene.fbx(filepath=str((output / 'OctopusToy.fbx').resolve()), use_selection=True,
                        object_types={'MESH'}, add_leaf_bones=False, bake_anim=False,
                        axis_forward='-Z', axis_up='Y', path_mode='STRIP', embed_textures=False)
raw = source.read_bytes()
json_size = struct.unpack_from('<I', raw, 12)[0]
document = json.loads(raw[20:20 + json_size])
binary = raw[28 + json_size:]
image_index = document['textures'][document['materials'][0]['pbrMetallicRoughness']['baseColorTexture']['index']]['source']
view = document['bufferViews'][document['images'][image_index]['bufferView']]
start = view.get('byteOffset', 0)
(output / 'OctopusAlbedo.jpg').write_bytes(binary[start:start + view['byteLength']])
print('Exported vertices:', len(obj.data.vertices), 'faces:', len(obj.data.polygons))

"""Static structure/syntax verification. Does not replace Unity compilation or PlayMode tests.
Optional syntax parser: pip install tree-sitter tree-sitter-c-sharp
"""
from pathlib import Path
import json,sys,re,math
from menu_art_payloads import read_payloads,png_size
root=Path(__file__).resolve().parents[1]
errors=[]
for f in [root/'Packages/manifest.json',*root.glob('Assets/**/*.asmdef')]:
 try:json.loads(f.read_text())
 except Exception as e:errors.append(f'{f}: {e}')
# Validate atlas crop metadata before Unity import; wrong coordinates otherwise trigger fallback art.
art=root/'Assets/Brainrot/Art/UI/Resources'
try:
 resources={}
 packed_manifest=root/'Assets/Brainrot/Generated/Editor/MenuArtPayloads/manifest.json'
 packed=read_payloads(packed_manifest) if packed_manifest.exists() else {}
 # Resources.Load is keyed by the path below any Resources folder, including Generated.
 for image in (root/'Assets/Brainrot').rglob('*.png'):
  if 'Resources' not in image.parts:continue
  folder=max(index for index,part in enumerate(image.parts) if part=='Resources')
  resource=Path(*image.parts[folder+1:]).with_suffix('').as_posix()
  key=resource.casefold()
  if key in resources:errors.append('Duplicate UI texture resource '+resource)
  else:resources[key]=image
 for resource,data in packed.items():
  key=resource.casefold()
  image=resources.get(key)
  expected=root/'Assets/Brainrot/Generated/Resources'/(resource+'.png')
  if image is not None:
   if image.resolve()!=expected.resolve():errors.append('Duplicate packed UI texture resource '+resource)
   # A stale/corrupt materialized image must fail, never silently use the packed fallback.
   if image.read_bytes()!=data:errors.append('Packed UI texture output differs from verified source: '+resource)
  else:resources[key]=data
 sizes={}
 names=set()
 for entry in json.loads((art/'TropicalUIAtlasLayout.json').read_text())['sprites']:
  name=entry['name']
  if name in names:errors.append('Duplicate UI sprite '+name)
  names.add(name)
  resource=entry.get('resource','TropicalUIAtlas')
  if not isinstance(resource,str) or not resource or '\\' in resource or ':' in resource or any(part in ('','.','..') for part in resource.split('/')):
   errors.append('Invalid UI texture resource: '+str(resource));continue
  image=resources.get(resource.casefold())
  if image is None:errors.append('Missing UI texture resource: '+resource);continue
  if resource.casefold() not in sizes:
   sizes[resource.casefold()]=png_size(image if isinstance(image,bytes) else image.read_bytes())
  width,height=sizes[resource.casefold()]
  x,y,w,h=(entry[key] for key in ('x','y','width','height'))
  if any(isinstance(value,bool) or not isinstance(value,(float,int)) or not math.isfinite(value) for value in (x,y,w,h)):
   errors.append('Invalid UI sprite coordinates: '+name);continue
  if x<0 or y<0 or w<=0 or h<=0 or x+w>width or y+h>height:errors.append('UI sprite outside texture: '+name)
  border=entry.get('border',0)
  if not isinstance(border,(float,int)) or not math.isfinite(border) or border<0 or border>min(w,h)/2:errors.append('Invalid UI sprite border: '+name)
except Exception as e:errors.append('UI metadata: '+str(e))
try:
 import tree_sitter,tree_sitter_c_sharp
 parser=tree_sitter.Parser(tree_sitter.Language(tree_sitter_c_sharp.language()))
 for f in root.glob('Assets/**/*.cs'):
  tree=parser.parse(f.read_bytes())
  # CS0819 is grammatically valid, but C# forbids multiple declarators with var.
  stack=[tree.root_node]
  while stack:
   node=stack.pop()
   if node.type=='variable_declaration':
    kind=node.child_by_field_name('type')
    if kind is not None and kind.text==b'var' and sum(child.type=='variable_declarator' for child in node.named_children)>1:
     errors.append(f'{f.relative_to(root)}:{node.start_point[0]+1}: var cannot declare multiple variables (CS0819)')
   stack.extend(node.named_children)
  if tree.root_node.has_error:
   def walk(n):
    if n.type=='ERROR' or n.is_missing:errors.append(f'{f.relative_to(root)}:{n.start_point[0]+1}: {n.type}')
    for c in n.children:walk(c)
   walk(tree.root_node)
except ImportError:errors.append('Install tree-sitter and tree-sitter-c-sharp for syntax validation.')
# Syntax parsing cannot detect C# collection Add overload errors. Check string dictionaries too.
for f in root.glob('Assets/**/*.cs'):
 source=f.read_text()
 for block in re.finditer(r'Dictionary<string,\s*string>\s+\w+\s*=\s*new Dictionary<string,\s*string>\s*\{(.*?)\};',source,re.S):
  keys=set()
  for entry in re.finditer(r'\{([^{}]*)\}',block.group(1)):
   values=re.findall(r'"(?:\\.|[^"\\])*"',entry.group(1))
   if len(values)!=2:errors.append(f'{f.relative_to(root)}: dictionary entry has {len(values)} strings, expected key/value pair')
   elif values[0] in keys:errors.append(f'{f.relative_to(root)}: duplicate dictionary key {values[0]}')
   else:keys.add(values[0])
required=['GameManager','PlayerController','EnemyController','EnemySpawner','CombatSystem','UpgradeSystem','BossSystem','UIManager','SaveSystem','ObjectPool']
for name in required:
 if not list(root.glob(f'Assets/**/{name}.cs')):errors.append('Missing '+name)
meta={}
for f in root.glob('Assets/**/*.meta'):
 match=re.search(r'^guid: ([0-9a-f]{32})$',f.read_text(),re.M)
 if not match:errors.append('Missing GUID '+str(f));continue
 guid=match[1]
 if guid in meta:errors.append('Duplicate GUID '+str(f))
 meta[guid]=str(f)
scene=(root/'Assets/Scenes/BrainrotIreland.unity').read_text()
if '7859db8af784412aaf6fa6510dfbe299' not in scene:errors.append('Bootstrap reference missing')
if len(re.findall(r'Make\(UpgradeKind\.',(root/'Assets/Brainrot/Runtime/UpgradeSystem.cs').read_text()))<12:errors.append('Fewer than 12 upgrades')
for e in errors:print('FAIL:',e)
print(f'{len(list(root.glob("Assets/**/*.cs")))} C# files checked; {len(errors)} errors. Unity execution NOT performed.')
sys.exit(bool(errors))

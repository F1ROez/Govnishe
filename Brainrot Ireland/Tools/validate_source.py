"""Static structure/syntax verification. Does not replace Unity compilation or PlayMode tests.
Optional syntax parser: pip install tree-sitter tree-sitter-c-sharp
"""
from pathlib import Path
import json,sys,re,struct
root=Path(__file__).resolve().parents[1]
errors=[]
for f in [root/'Packages/manifest.json',*root.glob('Assets/**/*.asmdef')]:
 try:json.loads(f.read_text())
 except Exception as e:errors.append(f'{f}: {e}')
# Validate atlas crop metadata before Unity import; wrong coordinates otherwise trigger fallback art.
art=root/'Assets/Brainrot/Art/UI/Resources'
try:
 names=set()
 for entry in json.loads((art/'TropicalUIAtlasLayout.json').read_text())['sprites']:
  name=entry['name']
  if name in names:errors.append('Duplicate UI sprite '+name)
  names.add(name)
  image=art/(entry.get('resource','TropicalUIAtlas')+'.png')
  with image.open('rb') as stream:header=stream.read(24)
  if header[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('Invalid PNG '+str(image))
  width,height=struct.unpack('>II',header[16:24])
  x,y,w,h=(entry[key] for key in ('x','y','width','height'))
  if x<0 or y<0 or w<=0 or h<=0 or x+w>width or y+h>height:errors.append('UI sprite outside texture: '+name)
except Exception as e:errors.append('UI metadata: '+str(e))
try:
 import tree_sitter,tree_sitter_c_sharp
 parser=tree_sitter.Parser(tree_sitter.Language(tree_sitter_c_sharp.language()))
 for f in root.glob('Assets/**/*.cs'):
  tree=parser.parse(f.read_bytes())
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

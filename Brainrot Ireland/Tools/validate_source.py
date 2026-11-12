"""Static structure/syntax verification. Does not replace Unity compilation or PlayMode tests.
Optional syntax parser: pip install tree-sitter tree-sitter-c-sharp
"""
from pathlib import Path
import json,sys,re
root=Path(__file__).resolve().parents[1]
errors=[]
for f in [root/'Packages/manifest.json',*root.glob('Assets/**/*.asmdef')]:
 try:json.loads(f.read_text())
 except Exception as e:errors.append(f'{f}: {e}')
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

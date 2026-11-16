#!/usr/bin/env python3
"""Check Windows player ZIP structure and its matching build metadata. Does not launch the game."""
import argparse, hashlib, json, pathlib, sys, zipfile

def inspect(path, metadata=None):
    errors=[]; warnings=[]
    with zipfile.ZipFile(path) as archive:
        entries=archive.infolist(); names={entry.filename for entry in entries if not entry.is_dir()}
        folded=set()
        for entry in entries:
            name=entry.filename; parts=pathlib.PurePosixPath(name).parts
            if '\\' in name or name.startswith('/') or '..' in parts or (parts and ':' in parts[0]): errors.append('Unsafe archive path: '+name)
            lower=name.casefold()
            if lower in folded: errors.append('Duplicate/case-colliding archive path: '+name)
            folded.add(lower)
            if any(part.casefold() in {'.git','library','temp','obj','logs'} for part in parts): errors.append('Project/cache content inside player ZIP: '+name)
            if (entry.external_attr >> 16) & 0o170000 == 0o120000: errors.append('Symlink inside player archive: '+name)
            if any('donotship' in part.casefold().replace('_','') or 'dontship' in part.casefold().replace('_','') or 'backupthisfolder' in part.casefold().replace('_','') for part in parts): errors.append('Unity debugging backup must not ship: '+name)
            if pathlib.PurePosixPath(name).suffix.lower() in {'.keystore','.jks','.p12','.pem'}: errors.append('Signing/private key material must not ship: '+name)
            if entry.flag_bits & 1: errors.append('Encrypted archive member: '+name)
            if pathlib.PurePosixPath(name).suffix.lower() in {'.cs','.csproj','.sln','.unity','.fbx','.mp3','.meta'}: warnings.append('Unexpected source/loose asset in player archive: '+name)
        for required in ('BrainrotIreland.exe','UnityPlayer.dll'):
            if required not in names: errors.append('Missing root player artifact: '+required)
            elif archive.getinfo(required).file_size==0: errors.append('Empty player artifact: '+required)
        if 'README_ALPHA.txt' not in names: warnings.append('Player instructions README_ALPHA.txt missing.')
        if 'LICENSES/IslandRounded-OFL.txt' not in names: warnings.append('Bundled IslandRounded font license notice missing.')
        if not any(n.startswith('BrainrotIreland_Data/') for n in names): errors.append('Missing BrainrotIreland_Data/ contents.')
        bad=archive.testzip()
        if bad: errors.append('Corrupt ZIP member: '+bad)
        if metadata:
            data=json.loads(pathlib.Path(metadata).read_text(encoding='utf-8-sig'))
            if 'BrainrotIreland.exe' in names:
                actual=hashlib.sha256(archive.read('BrainrotIreland.exe')).hexdigest()
                if data.get('playerSha256')!=actual: errors.append('EXE hash differs from build metadata.')
            if not data.get('sourceRevision') or data.get('sourceRevision')=='unknown': warnings.append('Build source revision is unknown.')
    return {'status':'failed' if errors else 'structure-passed','errors':errors,'warnings':warnings,'scope':'Archive integrity/structure only. Game execution and asset rights are not verified.','zip':str(path)}

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('zip',type=pathlib.Path);parser.add_argument('--metadata',type=pathlib.Path);parser.add_argument('--report',type=pathlib.Path);args=parser.parse_args()
    try: result=inspect(args.zip,args.metadata)
    except (OSError,ValueError,zipfile.BadZipFile,json.JSONDecodeError) as exception: result={'status':'failed','errors':[str(exception)]}
    text=json.dumps(result,ensure_ascii=False,indent=2)
    if args.report: args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(text,encoding='utf-8')
    print(text);return 1 if result['status']=='failed' else 0
if __name__=='__main__':sys.exit(main())

#!/usr/bin/env python3
"""Run real Unity checks, retain XML/logs, then build/package the Windows alpha.
No Unity executable means a blocked report and nonzero exit, never a successful test.
"""
from __future__ import annotations
import argparse, datetime as dt, hashlib, json, os, pathlib, shutil, subprocess, sys, time, xml.etree.ElementTree as ET, zipfile
ROOT = pathlib.Path(__file__).resolve().parents[1]

def unity_path(explicit: str | None) -> pathlib.Path | None:
    candidates = [explicit, os.environ.get('UNITY_EDITOR'), shutil.which('Unity'), shutil.which('unity')]
    version_file = ROOT / 'ProjectSettings/ProjectVersion.txt'
    version = next((s.split(':', 1)[1].strip() for s in version_file.read_text().splitlines() if s.startswith('m_EditorVersion:')), '')
    for base in [pathlib.Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Unity/Hub/Editor', pathlib.Path('/Applications/Unity/Hub/Editor'), pathlib.Path.home() / 'Unity/Hub/Editor']:
        candidates += [str(base / version / 'Editor/Unity.exe'), str(base / version / 'Unity.app/Contents/MacOS/Unity'), str(base / version / 'Editor/Unity')]
    for value in candidates:
        if value and pathlib.Path(value).is_file(): return pathlib.Path(value).resolve()
    return None

def test_result(path: pathlib.Path) -> dict:
    if not path.is_file(): raise ValueError(f'Unity did not write test results: {path}')
    node = ET.parse(path).getroot()
    tests = list(node.iter('test-case'))
    if not tests: raise ValueError(f'No test cases ran in {path}')
    failed = [t.get('fullname', t.get('name', '?')) for t in tests if t.get('result', '').lower() in {'failed', 'error', 'cancelled'}]
    passed = sum(t.get('result', '').lower() in {'passed', 'success'} for t in tests)
    if failed or passed == 0 or node.get('result', '').lower() in {'failed', 'error', 'cancelled'}:
        raise ValueError(f'Test results failed: passed={passed}, failures={failed[:20]}')
    return {'cases': len(tests), 'passed': passed, 'skipped': len(tests) - passed, 'xml': str(path)}

def package_windows(build: pathlib.Path, destination: pathlib.Path) -> dict:
    required = [build / 'BrainrotIreland.exe', build / 'UnityPlayer.dll', build / 'BrainrotIreland_Data']
    if not all(p.exists() for p in required): raise ValueError('Missing Windows player artifacts; cannot package source as a game.')
    destination.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(build.rglob('*')):
            relative=path.relative_to(build)
            dont_ship=any('donotship' in part.casefold().replace('_','') or 'dontship' in part.casefold().replace('_','') or 'backupthisfolder' in part.casefold().replace('_','') for part in relative.parts)
            if path.is_file() and path.name != 'alpha-build-report.json' and not dont_ship and path.suffix.lower() not in {'.pdb','.mdb'}: archive.write(path, relative.as_posix())
        archive.write(ROOT / 'Assets/Brainrot/Art/Fonts/OFL.txt', 'LICENSES/IslandRounded-OFL.txt')
        archive.writestr('README_ALPHA.txt', 'Brainrot Ireland — Windows test alpha\nExtract the complete ZIP and start BrainrotIreland.exe.\nWASD movement; mouse camera; LMB strike, hold/release heavy strike; Shift sprint; Space dash; V camera lock; Escape pause.\nThis package passed only the automated stages recorded in the external summary.json. Manual gameplay/hardware acceptance and asset rights must be reviewed before public release.\n')
    return {'zip': str(destination), 'bytes': destination.stat().st_size, 'sha256': hashlib.sha256(destination.read_bytes()).hexdigest()}

def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--unity', help='Full Unity editor executable path; UNITY_EDITOR is also supported.')
    p.add_argument('--reports', type=pathlib.Path, default=ROOT / 'Logs/AlphaChecks')
    p.add_argument('--timeout', type=int, default=3600, help='Seconds allowed per Unity process.')
    p.add_argument('--development', action='store_true', help='Development build; diagnostics opt-in command is documented in TEST_PLAN.')
    p.add_argument('--skip-build', action='store_true', help='Run validation and tests only; does not mark a player built.')
    p.add_argument('--dry-run', action='store_true', help='Print commands only; no checks or build are claimed.')
    args = p.parse_args()
    editor = unity_path(args.unity)
    report_dir = args.reports.resolve()
    plans = [
        ('validate', ['-executeMethod', 'Brainrot.Editor.GameBuilder.Validate', '-quit']),
        ('editmode', ['-runTests', '-testPlatform', 'EditMode', '-testResults', str(report_dir / 'editmode.xml')]),
        ('playmode', ['-runTests', '-testPlatform', 'PlayMode', '-testResults', str(report_dir / 'playmode.xml')]),
    ]
    if not args.skip_build:
        plans.append(('windows', ['-executeMethod', 'Brainrot.Editor.AlphaValidationBuild.BuildWindows', '-quit'] + (['--brainrot-development'] if args.development else [])))
    commands = [(name, [str(editor or '<UNITY_EDITOR>'), '-batchmode', '-projectPath', str(ROOT), '-buildTarget', 'win64', '-logFile', str(report_dir / f'{name}.log'), *flags]) for name, flags in plans]
    if args.dry_run:
        print(json.dumps({'status': 'dry-run', 'executed': False, 'commands': commands}, indent=2)); return 0
    report_dir.mkdir(parents=True, exist_ok=True)
    summary = {'status': 'running', 'started_utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'unity': str(editor) if editor else None, 'project': str(ROOT), 'stages': [], 'development': args.development}
    summary_file = report_dir / 'summary.json'
    def write(): summary_file.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
    if editor is None:
        summary.update(status='blocked', reason='Unity Editor executable not found. No Unity compilation, tests or build were executed.');write();print(summary['reason']);return 2
    environment = dict(os.environ)
    revision = subprocess.run(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True, capture_output=True) if shutil.which('git') else None
    environment['BRAINROT_SOURCE_REVISION'] = revision.stdout.strip() if revision and revision.returncode == 0 else 'unknown'
    summary['source_revision'] = environment['BRAINROT_SOURCE_REVISION']
    write()
    try:
        for name, command in commands:
            # Delete previous XML only; stale XML must not turn a failed/crashed runner into a pass.
            result_file = report_dir / f'{name}.xml'
            if name in {'editmode', 'playmode'}: result_file.unlink(missing_ok=True)
            stage = {'name': name, 'command': command, 'status': 'running'};summary['stages'].append(stage);write()
            (report_dir / f'{name}.log').unlink(missing_ok=True)
            if name == 'windows': (ROOT / 'Builds/Windows/alpha-build-report.json').unlink(missing_ok=True)
            started = time.monotonic()
            with (report_dir / f'{name}-console.log').open('w', encoding='utf-8') as output:
                process = subprocess.run(command, cwd=ROOT, env=environment, stdout=output, stderr=subprocess.STDOUT, timeout=max(30,args.timeout))
            stage.update(exit_code=process.returncode, seconds=round(time.monotonic()-started, 2))
            if process.returncode != 0: raise ValueError(f'Unity {name} exited with code {process.returncode}; inspect {name}.log.')
            if name in {'editmode', 'playmode'}: stage['tests'] = test_result(result_file)
            if name == 'validate':
                log = report_dir / 'validate.log'
                if not log.is_file() or 'BRAINROT_VALIDATION_OK' not in log.read_text(encoding='utf-8',errors='replace'): raise ValueError('Validation marker missing; a zero exit code alone is insufficient.')
            if name == 'windows':
                metadata = ROOT / 'Builds/Windows/alpha-build-report.json'
                if not metadata.is_file(): raise ValueError('Windows build metadata missing.')
                stage['build'] = json.loads(metadata.read_text(encoding='utf-8'))
                if stage['build'].get('sourceRevision') != environment['BRAINROT_SOURCE_REVISION']: raise ValueError('Build metadata source revision mismatch.')
                summary['package'] = package_windows(ROOT / 'Builds/Windows', ROOT / 'Builds/Packages/BrainrotIreland-Windows-alpha.zip')
            stage['status'] = 'passed';write()
        summary['status'] = 'passed';summary['scope'] = 'Unity validation/tests' if args.skip_build else 'Unity validation/tests and Windows build/package'
        summary['manual_acceptance'] = 'NOT_RUN: run the 30-minute, hardware, visual, license and restart scenarios in Docs/TEST_PLAN.md.'
        write();print(str(summary_file));return 0
    except (ValueError, OSError, ET.ParseError, json.JSONDecodeError, subprocess.TimeoutExpired) as exception:
        if summary['stages'] and summary['stages'][-1]['status'] == 'running': summary['stages'][-1]['status'] = 'failed'
        summary.update(status='failed',reason=str(exception));write();print(str(exception),file=sys.stderr);return 1

if __name__ == '__main__': raise SystemExit(main())

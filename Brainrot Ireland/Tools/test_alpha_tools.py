"""Tool regression checks use synthetic files only, never a real player or user save directory."""
import hashlib, importlib.util, json, pathlib, tempfile, unittest, zipfile
HERE=pathlib.Path(__file__).resolve().parent

def module(name):
    spec=importlib.util.spec_from_file_location(name,HERE/(name+'.py'));value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
runner=module('run_alpha_checks');inspector=module('inspect_windows_zip')

class AlphaToolTests(unittest.TestCase):
    def setUp(self):self.temporary=tempfile.TemporaryDirectory(prefix='brainrot-alpha-tools-');self.root=pathlib.Path(self.temporary.name)
    def tearDown(self):self.temporary.cleanup()
    def xml(self,content):
        p=self.root/'results.xml';p.write_text(content);return p
    def archive(self,extra=None):
        p=self.root/'player.zip'
        with zipfile.ZipFile(p,'w') as z:
            for name,content in {'BrainrotIreland.exe':b'fake-unit-test-only','UnityPlayer.dll':b'fake-dll','BrainrotIreland_Data/globalgamemanagers':b'data',**(extra or {})}.items():z.writestr(name,content)
        return p
    def test_zero_test_cases_cannot_pass(self):
        with self.assertRaises(ValueError):runner.test_result(self.xml('<test-run result="Passed" total="0"/>'))
    def test_skipped_only_cannot_pass(self):
        with self.assertRaises(ValueError):runner.test_result(self.xml('<test-run result="Passed"><test-case result="Skipped"/></test-run>'))
    def test_any_failed_case_rejects_even_if_root_claims_pass(self):
        with self.assertRaises(ValueError):runner.test_result(self.xml('<test-run result="Passed"><test-case result="Passed"/><test-case result="Failed" fullname="regression"/></test-run>'))
    def test_actual_passing_case_is_counted(self):
        self.assertEqual(1,runner.test_result(self.xml('<test-run result="Passed"><test-case result="Passed"/></test-run>'))['passed'])
    def test_unfinished_case_cannot_be_counted_as_skipped(self):
        with self.assertRaises(ValueError):runner.test_result(self.xml('<test-run result="Passed"><test-case result="Passed"/><test-case name="unfinished"/></test-run>'))
    def test_unfinished_root_cannot_pass(self):
        with self.assertRaises(ValueError):runner.test_result(self.xml('<test-run result="Running"><test-case result="Passed"/></test-run>'))
    def test_archive_path_traversal_rejected(self):
        result=inspector.inspect(self.archive({'../outside.txt':b'bad'}));self.assertEqual('failed',result['status']);self.assertTrue(any('Unsafe' in e for e in result['errors']))
    def test_source_archive_is_not_a_player(self):
        p=self.root/'source.zip'
        with zipfile.ZipFile(p,'w') as z:z.writestr('Assets/Player.cs','source')
        self.assertEqual('failed',inspector.inspect(p)['status'])
    def test_case_colliding_windows_names_rejected(self):
        self.assertEqual('failed',inspector.inspect(self.archive({'unityplayer.dll':b'collision'}))['status'])
    def test_debug_backups_and_signing_keys_rejected(self):
        for name in ['BrainrotIreland_BackUpThisFolder_ButDontShipItWithYourGame/source.cpp','signing.jks']:
            self.assertEqual('failed',inspector.inspect(self.archive({name:b'private'}))['status'])
    def test_metadata_hash_tamper_rejected(self):
        p=self.root/'metadata.json';p.write_text(json.dumps({'sourceRevision':'fake','playerSha256':'0'*64}))
        self.assertEqual('failed',inspector.inspect(self.archive(),p)['status'])
    def test_synthetic_structure_pass_is_not_execution_claim(self):
        p=self.root/'metadata.json';p.write_text(json.dumps({'sourceRevision':'fake','playerSha256':hashlib.sha256(b'fake-unit-test-only').hexdigest()}))
        result=inspector.inspect(self.archive(),p);self.assertEqual('structure-passed',result['status']);self.assertIn('Game execution',result['scope'])
    def manifest(self):
        contents={'BrainrotIreland.exe':b'fake-unit-test-only','UnityPlayer.dll':b'fake-dll','BrainrotIreland_Data/globalgamemanagers':b'data'}
        return {'sourceRevision':'fake','playerSha256':hashlib.sha256(contents['BrainrotIreland.exe']).hexdigest(),'shippingFiles':[{'path':name,'bytes':len(value),'sha256':hashlib.sha256(value).hexdigest()} for name,value in contents.items()]}
    def metadata(self,data):
        path=self.root/'metadata.json';path.write_text(json.dumps(data));return path
    def test_shipping_manifest_matches_all_player_files(self):
        self.assertEqual('structure-passed',inspector.inspect(self.archive(),self.metadata(self.manifest()))['status'])
    def test_shipping_manifest_detects_modified_data_even_with_valid_zip_crc(self):
        result=inspector.inspect(self.archive({'BrainrotIreland_Data/globalgamemanagers':b'changed'}),self.metadata(self.manifest()))
        self.assertEqual('failed',result['status']);self.assertTrue(any('Build file hash differs' in error for error in result['errors']))
    def test_shipping_manifest_detects_unlisted_and_missing_files(self):
        data=self.manifest();data['shippingFiles'].append({'path':'missing.dll','bytes':1,'sha256':'0'*64})
        result=inspector.inspect(self.archive({'extra.dll':b'extra'}),self.metadata(data))
        self.assertEqual('failed',result['status']);self.assertTrue(any('missing from package' in error for error in result['errors']));self.assertTrue(any('absent from build manifest' in error for error in result['errors']))
    def test_package_adds_instructions_license_and_excludes_debug_backups(self):
        build=self.root/'build';build.mkdir();(build/'BrainrotIreland.exe').write_bytes(b'fake-unit-test-only');(build/'UnityPlayer.dll').write_bytes(b'fake-dll')
        (build/'BrainrotIreland_Data').mkdir();(build/'BrainrotIreland_Data/globalgamemanagers').write_bytes(b'data')
        backup=build/'BrainrotIreland_BackUpThisFolder_ButDontShipItWithYourGame';backup.mkdir();(backup/'source.cpp').write_text('private')
        (build/'debug.pdb').write_text('debug');(build/'alpha-build-report.json').write_text('{}')
        package=self.root/'package.zip';runner.package_windows(build,package)
        with zipfile.ZipFile(package) as archive:
            self.assertIn('README_ALPHA.txt',archive.namelist());self.assertIn('LICENSES/IslandRounded-OFL.txt',archive.namelist());self.assertNotIn('debug.pdb',archive.namelist());self.assertFalse(any('DontShip' in name for name in archive.namelist()))
        self.assertEqual('structure-passed',inspector.inspect(package,self.metadata(self.manifest()))['status'])
    def test_empty_player_data_cannot_be_packaged(self):
        build=self.root/'build';build.mkdir();(build/'BrainrotIreland.exe').write_bytes(b'fake');(build/'UnityPlayer.dll').write_bytes(b'fake');(build/'BrainrotIreland_Data').mkdir()
        with self.assertRaises(ValueError):runner.package_windows(build,self.root/'empty.zip')

if __name__=='__main__':unittest.main()

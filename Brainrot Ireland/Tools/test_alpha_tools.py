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

if __name__=='__main__':unittest.main()

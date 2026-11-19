import base64,gzip,hashlib,json,struct,unittest
from pathlib import Path
from optimize_orca_mesh import encode_geometry,OUTPUT,SOURCE
class OrcaGeometryTests(unittest.TestCase):
    def test_quantization_preserves_bounds_and_uv(self):
        raw=encode_geometry([(0.,0.,0.),(1.,2.,3.),(.4,1.,2.)],[(0.,1.,0.)]*3,[(0.,0.),(1.,1.),(.5,.25)],[0,1,2])
        self.assertEqual(struct.unpack_from('<4sII',raw),(b'OPM1',3,3));self.assertEqual(len(raw),36+3*16+6)
        first=struct.unpack_from('<3H3h2H',raw,36);last=struct.unpack_from('<3H3h2H',raw,52)
        self.assertEqual(first[:3],(0,0,0));self.assertEqual(last[:3],(65535,65535,65535));self.assertEqual(last[-2:],(65535,65535))
    def test_invalid_indices_and_uv_are_rejected(self):
        args=[[(0.,0.,0.),(1.,1.,1.),(.5,.5,.5)],[(0.,1.,0.)]*3,[(0.,0.)]*3,[0,1,7]]
        with self.assertRaises(ValueError):encode_geometry(*args)
        args[3]=[0,1,2];args[2]=[(0.,0.),(2.,0.),(0.,0.)]
        with self.assertRaises(ValueError):encode_geometry(*args)
    def test_shipped_derivative_has_valid_indices_and_reduced_budget(self):
        m=json.loads((OUTPUT/'manifest.json').read_text());self.assertEqual(m['source_sha256'],hashlib.sha256(SOURCE.read_bytes()).hexdigest())
        packed=base64.b64decode(''.join((OUTPUT/p).read_text() for p in m['chunks']),validate=True);raw=gzip.decompress(packed)
        self.assertEqual(hashlib.sha256(raw).hexdigest(),m['sha256']);self.assertEqual(len(raw),m['decoded_bytes'])
        magic,n,count=struct.unpack_from('<4sII',raw);self.assertEqual(magic,b'OPM1');self.assertLessEqual(n,20000);self.assertLessEqual(count//3,20000)
        self.assertTrue(all(i<n for i in struct.unpack_from('<'+'H'*count,raw,36+n*16)))
    def test_every_delivery_part_stays_small(self):
        m=json.loads((OUTPUT/'manifest.json').read_text())
        self.assertEqual(m['chunks'],[f'{i:03}.b64' for i in range(len(m['chunks']))])
        self.assertTrue(all(0<(OUTPUT/p).stat().st_size<=32768 for p in m['chunks']))
if __name__=='__main__':unittest.main()

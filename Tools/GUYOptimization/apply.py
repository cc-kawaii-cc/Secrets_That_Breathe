"""Apply the six validated FBX geometry changes, keeping original .meta and all scene assets."""
from pathlib import Path
import json,hashlib,os,shutil,datetime
out=Path(os.environ.get('GUY_OPT_OUTPUT','Tools/GUYOptimization'));stage=Path('Tools/CompileChecks/GUYOptimizationUnity')
validation=json.loads((stage/'mesh-validation.json').read_text(encoding='utf-8'))
assert validation['success'],validation.get('error')
rows=json.loads((out/'merge-report.json').read_text(encoding='utf-8'))
digest=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
for r in rows:
    p=Path(r['path']);assert digest(p)==r['originalSHA256'],('Source changed since inspection',p)
    assert digest(r['output'])==r['optimizedSHA256']
    assert Path(str(p)+'.meta').read_bytes()==Path(r['backup']+'.meta').read_bytes(),('Meta changed',p)
beforeAssets={str(p):digest(p) for p in Path('Assets/GUY').rglob('*') if p.is_file()}
for i,r in enumerate(rows):
    temporary=out/f'swap-{i}.tmp';shutil.copyfile(r['output'],temporary)
    os.replace(temporary,Path(r['path']))
afterAssets={str(p):digest(p) for p in Path('Assets/GUY').rglob('*') if p.is_file()}
changed=[p for p in beforeAssets if beforeAssets[p]!=afterAssets[p]]
assert set(changed)=={str(Path(r['path'])) for r in rows}
result={'appliedAt':datetime.datetime.now().isoformat(),'changedAssets':changed,'beforeTriangles':sum(r['beforeTriangles'] for r in rows),
        'afterTriangles':sum(r['afterTriangles'] for r in rows),'beforeBytes':sum(r['beforeBytes'] for r in rows),
        'afterBytes':sum(r['afterBytes'] for r in rows),'unityMeshIDsHierarchyTransformsAndMaterialSlotsValidated':True,
        'allOtherGUYFilesUnchanged':True,'metaFilesUnchanged':True,'models':rows}
(out/'applied.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
shutil.copy2(stage/'mesh-validation.json',out/'unity-mesh-validation.json')
print(json.dumps({k:v for k,v in result.items() if k!='models'},indent=2))

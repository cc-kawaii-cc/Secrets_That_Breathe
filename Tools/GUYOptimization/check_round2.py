from pathlib import Path
import os,json,subprocess,shutil,re
base=Path(__file__).resolve().parent;out=base/'Round2';env=os.environ.copy()
env['GUY_OPT_OUTPUT']=str(out);env['BLENDER_USER_RESOURCES']=str(base/'BlenderUser');env['PYTHONIOENCODING']='utf-8'
exe=json.loads((base/'blender-path.json').read_text())
for i in [0,2,4]:
    with (out/f'lit{i}.log').open('w',encoding='utf-8') as f:
        subprocess.run([exe,'--background','--factory-startup','--python-exit-code','1','--python',str(base/'render_lit.py'),'--',str(i)],env=env,stdout=f,stderr=subprocess.STDOUT,check=True)
    print('Lit view checked',i,flush=True)
subprocess.run([__import__('sys').executable,'-X','utf8',str(base/'merge_geometry.py')],env=env,check=True)
stage=base.parent/'CompileChecks/GUYOptimizationUnity'
script=(base/'CheckMeshes.cs').read_text().replace('"../../../GUYOptimization"','"../../../GUYOptimization/Round2"')
(stage/'Assets/Editor/CheckMeshes.cs').write_text(script)
for i,r in enumerate(json.loads((out/'merge-report.json').read_text())):
    dest=stage/f'Assets/Models/Model{i}.fbx';shutil.copy2(r['backup'],dest)
    meta=Path(r['backup']+'.meta').read_text(encoding='utf-8-sig')
    Path(str(dest)+'.meta').write_text(re.sub(r'(generateSecondaryUV:) 1',r'\1 0',meta),encoding='utf-8')
print('Prepared Round2 Unity check',flush=True)

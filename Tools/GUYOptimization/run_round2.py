from pathlib import Path
import json,os,subprocess,sys
base=Path(__file__).resolve().parent;out=base/'Round2';out.mkdir(exist_ok=True)
first=json.loads((base/'applied.json').read_text())
config=[{'path':r['path'],'ratio':ratio,'minimumTriangles':800,'minimumBudget':200,'weldCoincidentVertices':True}
        for r,ratio in zip(first['models'],[.1,.2,.2,.2,.25,.25])]
config[4]['keepMeshes']=['Bucket_Rim']  # Retain the paint surface's authored normals.
(out/'config.json').write_text(json.dumps(config,indent=2))
exe=json.loads((base/'blender-path.json').read_text());env=os.environ.copy()
env['GUY_OPT_OUTPUT']=str(out);env['BLENDER_USER_RESOURCES']=str(base/'BlenderUser');env['PYTHONIOENCODING']='utf-8'
for i in ([int(v) for v in sys.argv[1:]] if len(sys.argv)>1 else range(6)):
    with (out/f'model{i}.log').open('w',encoding='utf-8') as log:
        result=subprocess.run([exe,'--background','--factory-startup','--python-exit-code','1','--python',str(base/'optimize_blender.py'),'--',str(i)],env=env,stdout=log,stderr=subprocess.STDOUT)
    path=out/f'Model{i}/result.json'
    if result.returncode or not path.exists():raise RuntimeError(f'Model {i} failed; inspect its log')
    r=json.loads(path.read_text());print(i,r['before'],'->',r['after'],flush=True)

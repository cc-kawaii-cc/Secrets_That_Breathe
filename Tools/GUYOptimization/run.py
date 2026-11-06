from pathlib import Path
import json,os,subprocess,sys
out=Path(__file__).resolve().parent
audit=json.loads((out.parent/'GUYAudit/audit.json').read_text(encoding='utf-8'))
models=sorted(audit['models'],key=lambda m:m['triangles'],reverse=True)[:6]
config=[{'path':m['path'],'ratio':ratio} for m,ratio in zip(models,[.02,.05,.07,.08,.04,.04])]
(out/'config.json').write_text(json.dumps(config,indent=2),encoding='utf-8')
exe=json.loads((out/'blender-path.json').read_text());env=os.environ.copy()
env['BLENDER_USER_RESOURCES']=str(out/'BlenderUser');env['PYTHONIOENCODING']='utf-8'
indexes=[int(s) for s in sys.argv[1:]] if len(sys.argv)>1 else list(range(6))
for index in indexes:
    with (out/f'model{index}.log').open('w',encoding='utf-8') as f:
        result=subprocess.run([exe,'--background','--factory-startup','--python-exit-code','1','--python',str(out/'optimize_blender.py'),'--',str(index)],env=env,stdout=f,stderr=subprocess.STDOUT)
    marker=out/f'Model{index}/result.json'
    if result.returncode or not marker.exists():raise RuntimeError(f'Model {index} failed, see model{index}.log')
    r=json.loads(marker.read_text());print(index,r['path'],r['before'],'->',r['after'],flush=True)

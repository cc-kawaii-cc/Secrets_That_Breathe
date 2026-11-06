from pathlib import Path
import os,json,subprocess
out=Path(__file__).resolve().parent;tools=out.parent/'GUYOptimization'
exe=json.loads((tools/'blender-path.json').read_text());env=os.environ.copy();env['BLENDER_USER_RESOURCES']=str(tools/'BlenderUser');env['PYTHONIOENCODING']='utf-8'
with (out/'blender.log').open('w',encoding='utf-8') as f:
    subprocess.run([exe,'--background','--factory-startup','--python-exit-code','1','--python',str(out/'optimize_chair.py')],env=env,stdout=f,stderr=subprocess.STDOUT,check=True)
print((out/'optimization.json').read_text())

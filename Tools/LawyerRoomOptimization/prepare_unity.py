from pathlib import Path
import json,shutil
out=Path('Tools/LawyerRoomOptimization');stage=Path('Tools/CompileChecks/LawyerRoomAuditUnity')
r=json.loads((out/'source.json').read_text());dest=stage/r['source'];dest.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(r['backup'],dest);shutil.copy2(r['backup']+'.meta',str(dest)+'.meta')
shutil.copy2(out/'BuildChair.cs',stage/'Assets/Editor/BuildChair.cs')
previous=stage/'Tools/LawyerRoomPerformance/batch.result.json'
if previous.exists():shutil.copy2(previous,out/'before-stage-audit.json')
print('Prepared isolated native mesh build and saved-scene verification.')

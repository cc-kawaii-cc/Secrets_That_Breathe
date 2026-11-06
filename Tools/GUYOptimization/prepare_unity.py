from pathlib import Path
import json,shutil,re
out=Path('Tools/GUYOptimization');stage=Path('Tools/CompileChecks/GUYOptimizationUnity')
for name in ['Assets/Models','Assets/Editor','Packages','ProjectSettings']:(stage/name).mkdir(parents=True,exist_ok=True)
(stage/'Packages/manifest.json').write_text('{"dependencies":{}}')
shutil.copy2('ProjectSettings/ProjectVersion.txt',stage/'ProjectSettings/ProjectVersion.txt')
shutil.copy2(out/'CheckMeshes.cs',stage/'Assets/Editor/CheckMeshes.cs')
for i,r in enumerate(json.loads((out/'merge-report.json').read_text())):
    dest=stage/f'Assets/Models/Model{i}.fbx';shutil.copy2(r['backup'],dest)
    meta=Path(r['backup']+'.meta').read_text(encoding='utf-8-sig')
    # UV generation is disabled in this isolated import check only, on both
    # versions. This test checks stable IDs/hierarchy, not lightmap quality.
    meta=re.sub(r'(generateSecondaryUV:) 1',r'\1 0',meta)
    Path(str(dest)+'.meta').write_text(meta,encoding='utf-8')
print(stage.resolve())

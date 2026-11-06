from pathlib import Path
import json,hashlib,shutil,os
from PIL import Image,ImageDraw
out=Path('Tools/LawyerRoomOptimization');source=json.loads((out/'source.json').read_text());build=json.loads((out/'unity-build.json').read_text())
before=json.loads(Path('Tools/LawyerRoomPerformance/scene-audit.json').read_text())
afterpath=Path('Tools/CompileChecks/LawyerRoomAuditUnity/Tools/LawyerRoomPerformance/batch.result.json');after=json.loads(afterpath.read_text())
digest=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
assert build['success'] and after['success']
assert build['guidBefore']==build['guidAfter'] and build['idBefore']==build['idAfter']==4300000
assert build['trianglesBefore']==77696 and build['trianglesAfter']==7999
assert before['lights']==after['lights'] and before['meshColliders']==after['meshColliders']
old={m['objectPath']:m for m in before['meshes']};new={m['objectPath']:m for m in after['meshes']};assert old.keys()==new.keys()
changed=[]
for key in old:
    if old[key]!=new[key]:
        assert {k for k in old[key] if old[key][k]!=new[key][k]}=={'vertices','triangles'},key
        assert new[key]['meshGuid']==build['guidBefore'] and new[key]['triangles']==7999
        changed.append(key)
assert len(changed)==6,changed
for p,hash_ in source['protectedFiles'].items():assert digest(p)==hash_,('Protected file changed',p)
path=Path(source['source']);assert digest(path)==source['sha256']
assert digest(str(path)+'.meta')==source['metaSHA256']
candidate=out/'optimized-chair.asset';temporary=out/'chair-swap.tmp';shutil.copyfile(candidate,temporary);os.replace(temporary,path)
assert digest(path)==digest(candidate);assert digest(str(path)+'.meta')==source['metaSHA256']
shutil.copy2(afterpath,out/'scene-after.json')
active=lambda report:sum(m['triangles'] for m in report['meshes'] if m['active'] and m['rendererEnabled'])
result={'success':True,'changedAsset':str(path),'changedSceneInstances':changed,'beforeSceneTriangles':active(before),'afterSceneTriangles':active(after),
        'beforeChairTriangles':77696,'afterChairTriangles':7999,'beforeChairVertices':build['verticesBefore'],'afterChairVertices':build['verticesAfter'],
        'beforeBytes':Path(source['backup']).stat().st_size,'afterBytes':path.stat().st_size,
        'sourceSHA256':source['sha256'],'optimizedSHA256':digest(path),'metaPreserved':True,'scenePrefabAndSeatingSetupPreserved':True,
        'backup':source['backup'],'unityStableMeshReferenceValidated':True}
(out/'applied.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
canvas=Image.new('RGB',(1600,680),(28,28,28));draw=ImageDraw.Draw(canvas)
for row,tag in enumerate(['before','after']):
    draw.text((10,row*340+8),f'{tag}: '+('77,696' if row==0 else '7,999')+' triangles',fill='white')
    for i in range(5):canvas.paste(Image.open(out/f'{tag}{i}.png').convert('RGB').resize((320,320)),(i*320,row*340+20))
canvas.save(out/'comparison.png')
md=['# Lawyer Room — ลด mesh เก้าอี้','',
    '| รายการ | ก่อน | หลัง |','|---|---:|---:|',
    '| Triangles ต่อเก้าอี้ | 77,696 | 7,999 |',
    '| Vertices ต่อ mesh | 133,650 | 7,153 |',
    '| Triangles เก้าอี้ 6 ตัว | 466,176 | 47,994 |',
    f'| Mesh ที่เปิดในฉากที่บันทึกไว้รวม | {result["beforeSceneTriangles"]:,} | {result["afterSceneTriangles"]:,} |','',
    f'ลด geometry ตามยอดเปิดในฉาก {100*(1-result["afterSceneTriangles"]/result["beforeSceneTriangles"]):.2f}% ไม่ใช่ผลวัด FPS','',
    'แก้เฉพาะ standalone Mesh asset ใต้ Assets/SecretsThatBreathe/Props/SmoothedChairs ซึ่งเก้าอี้ทั้ง 6 ตัวใน Lawyer Room อ้างอยู่ ไม่เปลี่ยน FBX รวมเฟอร์นิเจอร์ ต้นฉบับถูกสำรองใน Backups ตามโครงเส้นทางเดิม','',
    'ลดจุดซ้ำและหน้าเสื่อมสภาพ แล้ว simplify ภายใต้งบประมาณ 8,000 triangles ตรวจระยะผิวจาก/ไปต้นฉบับ โดยความคลาดเคลื่อนตัวอย่างสูงสุดประมาณ 0.0634% ของเส้นทแยงมุมโมเดล ถ่ายทอด normals จากผิว Smooth60 ต้นฉบับเพื่อลดรอยเงาเป็นคลื่น เก็บ UV0 และ UV2 ไว้','',
    'ตรวจภาพก่อน/หลัง 5 มุมใน Blender Workbench (comparison.png) และนำ mesh กลับเข้า Unity 6000.3.8f1 ในโปรเจกต์สำเนา สร้าง native .asset โดยคง GUID d33858a2ac6bb694a93c7b4c42df5b08 และ local file ID 4300000','',
    'ตรวจฉากในสำเนาว่ามีเพียง vertices/triangles ของ mesh เก้าอี้ 6 รายการเปลี่ยน รายการไฟและ MeshCollider เท่าเดิมทุกค่า ตรวจ SHA256 ของ Scene, chair.prefab และ ChairSmoothShadingSetup ว่าไม่เปลี่ยน การใช้งานจุดนั่ง/สคริปต์ไม่ถูกแก้ แต่ยังไม่ได้ทดสอบนั่งจริงใน Play Mode','',
    'chair.prefab/FBX เดิมเก็บไว้ตามเดิม งานนี้เปลี่ยน mesh ที่ฉาก Lawyer Room override ใช้อยู่ ส่วน ChairSmoothShadingSetup จะโหลด asset เดิมที่ลดแล้วเมื่อมีเก้าอี้ใหม่เข้าฉาก','',
    'ยังไม่ได้วัด FPS หรือ CPU/GPU Profiler ในเกมจริง และไม่ได้ Bake แสงใหม่หรือแก้ไฟ/Canvas จำนวน triangles เป็นยอดรายการ active/enabled ในไฟล์ฉากก่อน Play Mode ไม่ใช่จำนวนที่กล้องวาดต่อเฟรม','',
    'ต้นฉบับยังมีไฟล์ Recovery อ้าง mesh เดียวกัน ดังนั้นเมื่อเปิด Recovery ภาพเก้าอี้ก็จะใช้ข้อมูล mesh รุ่นใหม่ผ่าน GUID เดียวกันด้วย; backup ในโฟลเดอร์นี้เก็บรุ่นเก่าไว้ครบ','']
(out/'RESULTS.md').write_text('\n'.join(md),encoding='utf-8')
print(json.dumps(result,indent=2))

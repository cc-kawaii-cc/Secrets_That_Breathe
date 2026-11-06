"""Summarize the complete audit and serialized MainScenes references."""
from pathlib import Path
import json, re, hashlib, collections, csv

out=Path('Tools/GUYAudit'); report=json.loads((out/'audit.json').read_text(encoding='utf-8'))
models=report['models']; prefabs=report['prefabs']
# A GUID outside Assets can still be supplied by a Unity package.
unknown={g for p in prefabs for g in p['missingGUIDs']}
package_resolved={}
for meta in Path('Library/PackageCache').rglob('*.meta'):
    if not unknown: break
    with meta.open(encoding='utf-8-sig',errors='replace') as f: header=f.read(200)
    m=re.search(r'^guid: (\w+)',header,re.M)
    if m and m[1] in unknown:
        package_resolved[m[1]]=str(meta)[:-5];unknown.remove(m[1])
for p in prefabs:p['missingGUIDs']=[g for g in p['missingGUIDs'] if g not in package_resolved]
report['packageReferencesResolved']=package_resolved
with (out/'prefabs.csv').open(encoding='utf-8-sig',newline='') as f: fields=next(csv.reader(f))
with (out/'prefabs.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');writer.writeheader();writer.writerows(prefabs)
def guid(p):
    m=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(encoding='utf-8-sig'),re.M)
    return m[1]
assets={guid(Path(r['path'])):r for r in models+prefabs}
usage=collections.defaultdict(list)
for p in sorted(Path('Assets').rglob('*.unity')):
    refs=set(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8-sig',errors='replace')))
    used=set()
    for g in refs & assets.keys():
        a=assets[g];used.add(a['path']);used.update(a['dependencies'])
    for m in models:
        if m['path'] in used:usage[m['path']].append(p.as_posix())
duplicates=collections.defaultdict(list)
for m in models:duplicates[hashlib.sha256(Path(m['path']).read_bytes()).hexdigest()].append(m['path'])
report['identicalFBXFiles']=[v for v in duplicates.values() if len(v)>1]
report['serializedSceneReferencesIncludingInactiveObjects']=dict(usage)
(out/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
ranked=sorted(models,key=lambda r:r['triangles'],reverse=True)
top6=sum(m['triangles'] for m in ranked[:6]);total=sum(m['triangles'] for m in models)
md=['# ผลตรวจโมเดลใน Assets/GUY', '',
    'ตรวจแบบอ่านไฟล์อย่างเดียว: FBX 45 ไฟล์, Prefab 80 ไฟล์, texture 99 ไฟล์ รวม dependencies ที่พบ ไม่แก้โมเดล ค่า Import ฉาก หรือ Prefab', '',
    f'จำนวน triangle ใน geometry ของไฟล์ FBX รวม {total:,}; หกไฟล์แรกมี {top6:,} ({top6/total*100:.1f}%) การนับนี้รวมไฟล์สำเนา และไม่ใช่จำนวน triangle ที่เปิดพร้อมกันในเกม', '',
    '## สาเหตุที่พบ', '',
    '- Geometry หนาแน่นมากใน Toolbox2, ตู้รางวัล, Toolbox4, Toolbox และถังสีทั้งสองแบบ เป็นหลักฐานชัดว่ามีภาระประมวลผลและข้อมูล mesh สูงกว่าพร็อพอื่นในชุด',
    '- Generate Lightmap UVs เปิดใน 38/45 ไฟล์ รวมไฟล์หนักหกอันดับแรก การ unwrap/pack UV เพิ่มงานระหว่าง Import/Reimport โดยไม่ได้ทำซ้ำทุกครั้งที่โหลด Scene ใน Build',
    '- Toolbox2 มี Cube.023 ชิ้นเดียว 1,072,484 triangles และชิ้นย่อยหลายชิ้นชิ้นละ 139,964 triangles; ถังสีทั้งสองมี Torus.001 ชิ้นละ 216,644 triangles',
    '- ไม่มี LODGroup ที่บันทึกโดยตรงใน Prefab ทั้ง 80 ไฟล์ และไม่มี Auto Mesh LOD ใน FBX ทั้ง 45 ไฟล์ การไม่มี LOD เพิ่มภาระวาดระยะไกล แต่ไม่ได้แปลว่าเป็นสาเหตุของเวลาโหลดเพียงอย่างเดียว',
    '- ToolBox4 มี material ภายใน FBX 25 ตัว จึงมีโอกาสเพิ่ม draw calls ตามการใช้ submesh/material และ batching จริง',
    '- พบ texture เนื้อหาไฟล์เหมือนกัน 22 กลุ่ม กระจายหลายตำแหน่ง หากอ้างหลาย GUID ใน Scene อาจโหลดหลาย asset ต้องตรวจการใช้จริงก่อนรวมไฟล์',
    '- Read/Write ของ mesh ปิดครบทุกไฟล์; Model Importer ไม่ได้เปิดสร้าง MeshCollider อัตโนมัติ; ไม่พบ MeshCollider บันทึกโดยตรงใน Prefab ชุดนี้',
    '- Import Animation เปิดครบ 45 ไฟล์ แต่ต้นฉบับไม่มี AnimationCurve เป็นค่าที่ทำความสะอาดได้ภายหลัง ไม่ใช่หลักฐานว่านี่คือคอขวดหลัก', '',
    '## ผลครบทุก FBX', '',
    '| ไฟล์ (ใต้ Assets/GUY) | Triangles | Mesh | Material ใน FBX | MiB | สร้าง Lightmap UV | ประเด็น |',
    '|---|---:|---:|---:|---:|:---:|---|']
for m in ranked:
    t=m['triangles']; concern='หนักมาก' if t>=200000 else 'ควรตรวจ geometry' if t>=20000 else 'geometry ไม่หนักเมื่อเทียบในชุดนี้'
    if m['embeddedMB']>1:concern+=f'; embedded data {m["embeddedMB"]:.2f} MiB'
    md.append(f'| {m["path"].removeprefix("Assets/GUY/")} | {t:,} | {m["meshes"]} | {m["materials"]} | {m["MB"]:.2f} | {"เปิด" if m["settings"].get("generateSecondaryUV")==1 else "ปิด"} | {concern} |')
md+=['', '## ชิ้นส่วนที่หนักในหกอันดับแรก','']
for m in ranked[:6]:
    md.append(f'- **{Path(m["path"]).name}**: '+', '.join(f'{x["name"]} {x["triangles"]:,} tris' for x in sorted(m['meshDetails'],key=lambda x:x['triangles'],reverse=True)[:4]))
md+=['','## อ้างอิงใน Scene ที่ตรวจพบ','',
      'ค้นการอ้างอิงที่ serialize ไว้ใน Scene ทั้งหมดใต้ Assets รวม dependency ของ Prefab GUY ที่ตรวจไว้ รวมวัตถุ inactive ด้วย จึงไม่ใช่หลักฐานว่า asset กำลังถูกแสดงหรือโหลดในเส้นทางเล่นจริงทุกกรณี การอ้างผ่าน Prefab นอก GUY หรือโค้ดโหลดแบบ dynamic อาจไม่อยู่ในผลนี้','']
for m in ranked[:8]:
    scenes=usage.get(m['path'],[])
    md.append(f'- **{Path(m["path"]).name}**: '+('; '.join(scenes) if scenes else 'ไม่พบอ้างอิงในขอบเขต Scene ที่สแกน'))
md+=['','## ลำดับที่ควรแก้','',
      '1. ลดความหนาแน่น geometry ในสำเนาของ Toolbook2 ก่อน โดยตรวจ silhouette และรักษาชิ้นเปิด/ขยับกับ pivot เดิม จากนั้นตู้รางวัล, ToolBox4, Toolbox และ Torus ของถังสี',
      '2. ตรวจว่าพร็อพแต่ละตัวใช้ baked lightmap หรือไม่ ก่อนปิด Generate Lightmap UVs ในตัวที่ไม่ใช้ อย่าปิดรวมทั้งโฟลเดอร์เพราะอาจกระทบแสง bake',
      '3. เพิ่ม LOD สำหรับพร็อพที่เห็นไกล/วางซ้ำ และตรวจ material/submesh ของ ToolBox4',
      '4. รวม texture สำเนาเมื่อยืนยัน dependency แล้ว และตั้ง compression/resolution ให้เหมาะกับขนาดบนจอ', '',
      '## ข้อจำกัดของผลตรวจ','',
      '- Triangles คำนวณจาก polygon ใน FBX (จำนวนมุมลบสองต่อ polygon) ก่อน Unity แยก vertices จาก UV/normal/material และ optimize',
      '- ขนาดไฟล์ FBX บนดิสก์ไม่เท่ากับ RAM/VRAM ในเกม; texture CSV ระบุขนาด RGBA32+mip แบบ upper bound ไม่ใช่ค่าหน่วยความจำหลัง platform compression',
      '- บันทึก Import ที่ยังอยู่ไม่มีระยะเวลานำเข้า FBX เหล่านี้ครบ จึงยังระบุเป็นวินาทีไม่ได้ว่าไฟล์ใดทำให้รอเท่าไร ไม่ได้สั่ง Reimport เพื่อทดสอบ',
      '- ยังไม่ได้จับ Unity Profiler ตอนเปิด Scene/โหลดระหว่างเล่น และไม่ได้ตรวจหน้าตา topology/UV/normals ผ่าน viewport จึงระบุสาเหตุจากข้อมูลไฟล์และค่า Import ไม่ใช่ผล benchmark',
      '- รายละเอียด Prefab ทั้ง 80 ตัวอยู่ใน prefabs.csv, texture ทั้ง 99 ตัวอยู่ใน textures.csv, dependency/ชิ้นส่วน mesh/สำเนาอยู่ใน audit.json','']
(out/'GUY-model-audit.md').write_text('\n'.join(md),encoding='utf-8')
print(f'Top six: {top6:,}/{total:,} = {top6/total*100:.2f}%')
print('FBX duplicate groups',len(report['identicalFBXFiles']))
for m in ranked[:8]:print(Path(m['path']).name,usage.get(m['path'],[]))

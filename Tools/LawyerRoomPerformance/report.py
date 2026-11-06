from pathlib import Path
import json,csv,collections,shutil,hashlib,re
out=Path('Tools/LawyerRoomPerformance');stage=Path('Tools/CompileChecks/LawyerRoomAuditUnity')
src=stage/'Tools/LawyerRoomPerformance/batch.result.json';r=json.loads(src.read_text(encoding='utf-8'));assert r['success'],r['error']
shutil.copy2(src,out/'scene-audit.json')
manifest=json.loads((out/'stage-manifest.json').read_text())
scene=Path(r['scenePath']);assert hashlib.sha256(scene.read_bytes()).hexdigest()==manifest['sourceSceneSHA256'],'Scene changed during audit'
rows=r['meshes'];groups=collections.defaultdict(list)
for m in rows:
    key=m['prefabAsset'] or ('Scene objects: '+m['meshAsset']+' / '+m['meshName'])
    groups[key].append(m)
models=[]
for key,group in groups.items():
    units=collections.defaultdict(list)
    for m in group:units[m['prefabRoot'] or m['objectPath']].append(m)
    totals=[sum(m['triangles'] for m in unit) for unit in units.values()]
    active=[m for m in group if m['active'] and m['rendererEnabled']]
    models.append({'model':Path(key).stem if not key.startswith('Scene objects:') else group[0]['meshName'],
                   'prefabAsset':group[0]['prefabAsset'],'instances':len(units),'activeInstances':sum(any(m['active'] and m['rendererEnabled'] for m in unit) for unit in units.values()),
                   'perInstanceMin':min(totals),'perInstanceMax':max(totals),'totalTriangles':sum(totals),
                   'activeTriangles':sum(m['triangles'] for m in active),'renderers':len(group),
                   'activeMaterialSlots':sum(m['materialSlots'] for m in active),
                   'samplePath':group[0]['objectPath']})
models.sort(key=lambda m:m['totalTriangles'],reverse=True)
def export(name,data,fields):
    with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');w.writeheader();w.writerows(data)
export('models.csv',models,list(models[0]))
export('mesh-instances.csv',rows,['objectPath','meshAsset','meshName','triangles','vertices','submeshes','materialSlots','active','rendererEnabled','castsShadows','skinned','prefabAsset','prefabRoot','meshGuid','meshFileId'])
export('lights.csv',r['lights'],['objectPath','type','bakeType','shadows','active','range','intensity'])
active=[m for m in rows if m['active'] and m['rendererEnabled']]
light_active=[l for l in r['lights'] if l['active']]
shadow_lights=[l for l in light_active if l['bakeType']!='Baked' and l['shadows']!='None']
chair=next(m for m in models if m['model']=='chair')
summary={'savedScene':r['scenePath'],'modelGroups':len(models),'meshRenderers':len(rows),'activeMeshRenderers':len(active),
         'totalTrianglesIncludingInactive':sum(m['triangles'] for m in rows),'activeTriangles':sum(m['triangles'] for m in active),
         'activeMaterialSlots':sum(m['materialSlots'] for m in active),'chairTrianglesPerInstance':chair['perInstanceMax'],
         'chairInstances':chair['instances'],'chairTotal':chair['activeTriangles'],
         'chairSharePercent':chair['activeTriangles']/sum(m['triangles'] for m in active)*100,
         'activeLights':len(light_active),'activeRealtimeOrMixedShadowLights':len(shadow_lights),
         'meshColliders':len(r['meshColliders']),'meshColliderTriangles':sum(c['triangles'] for c in r['meshColliders']),
         'activeCameras':r['activeCameras'],'activeParticleSystems':r['activeParticleSystems'],
         'savedOcclusionCullingDataPresent':'m_OcclusionCullingData: {fileID: 0}' not in scene.read_text(encoding='utf-8-sig'),
         'sourceSceneUnchanged':True,'isolatedSavedSceneAudit':True}
(out/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
md=['# Lawyer Room — จำนวน triangles และจุดที่ควรลด','',
    'ตรวจฉากที่บันทึกไว้ `Assets/MainScenes/From_PM/Lawyer Room.unity` ด้วย Unity 6000.3.8f1 ในโปรเจกต์แยก อ่านอย่างเดียว ไม่แก้โมเดล/Prefab/แสงหรือบันทึกฉากใหม่','',
    f'- Mesh renderer ที่มี mesh: {len(rows)} รายการ; เปิด GameObject และ Renderer อยู่ {len(active)} รายการ',
    f'- รวม triangles ของรายการที่เปิด: {summary["activeTriangles"]:,}; รวม inactive ด้วย: {summary["totalTrianglesIncludingInactive"]:,}',
    f'- เก้าอี้ {chair["instances"]} ตัว × {chair["perInstanceMax"]:,} = {chair["activeTriangles"]:,} triangles คิดเป็น {summary["chairSharePercent"]:.2f}% ของยอดที่เปิด',
    '- เก้าอี้ใช้ mesh Smooth60 ซึ่งมี 133,650 vertices ต่อ mesh แม้หนึ่ง mesh จะใช้ซ้ำได้ การวาดซ้ำและเงายังคงมีงาน geometry ตามการมองเห็นจริง', '',
    '## ตารางครบทุกโมเดล/Prefab ที่อยู่ในฉาก','',
    'จำนวนชิ้นใช้จาก instance root ของ Prefab ที่ใกล้ mesh ที่สุด รวมวัตถุ inactive ด้วย คอลัมน์เปิดคือ GameObject active และ Renderer enabled ในไฟล์ฉาก ไม่ใช่จำนวนที่กล้องมองเห็นในเฟรมจริง','',
    '| โมเดล/Prefab | Tris ต่อชิ้น | จำนวนทั้งหมด | จำนวนเปิด | Tris รวมทั้งหมด | Tris รวมที่เปิด |',
    '|---|---:|---:|---:|---:|---:|']
for m in models:
    per=f'{m["perInstanceMin"]:,}' if m['perInstanceMin']==m['perInstanceMax'] else f'{m["perInstanceMin"]:,}–{m["perInstanceMax"]:,}'
    md.append(f'| {m["model"]} | {per} | {m["instances"]} | {m["activeInstances"]} | {m["totalTriangles"]:,} | {m["activeTriangles"]:,} |')
predicted=summary['activeTriangles']-chair['activeTriangles']+8000*chair['instances']
md+=['','## ลดตรงไหนคุ้มที่สุด','',
     '1. **เก้าอี้** ควรตรวจ topology และลดก่อน เป้าลองประมาณ 5,000–10,000 triangles ต่อเก้าอี้ โดยเทียบเบาะ พนักพิง ที่วางแขน ขา ล้อ และ UV ในระยะนั่งใกล้ ตัวเลขนี้เป็นเป้าลอง ไม่ใช่ผลที่ผ่านการตรวจภาพแล้ว ต้องรักษา pivot/collider/script ที่ใช้นั่ง',
     f'   ถ้าทำได้ 8,000 ต่อเก้าอี้ จะประเมินยอด mesh ที่เปิดทั้งฉากได้ประมาณ {predicted:,} triangles แทน {summary["activeTriangles"]:,} ลด geometry ตามยอดนี้ราว {(1-predicted/summary["activeTriangles"])*100:.1f}% ไม่ได้แปลว่า FPS จะเพิ่มเท่ากัน',
     '2. **ตู้ 3 ชั้น** ตัวละ 3,010 แต่มี 19 ตัว รวม 57,190 เป็นลำดับถัดไป พิจารณาลดรายละเอียดที่มองไม่เห็น/LOD โดยตรวจระยะใกล้ก่อน',
     '3. **โต๊ะ** ตัวละ 4,476 มี 7 ตัว รวม 31,332 ลดได้บ้าง แต่ผลรวมน้อยกว่าเก้าอี้มาก',
     '4. **พร็อพส่วนใหญ่** อยู่หลักสิบถึงหลักร้อย triangles ไม่ควรลดแรงจนเสียรูป แฟ้ม 93 ตัวมีแค่ 2,604 triangles รวม ให้ตรวจ batching/จำนวน renderers ถ้า CPU rendering หนักก่อนลด polygon เพิ่ม', '',
     '## ปัจจัยอื่นที่ตรวจพบ','',
     f'- มีไฟเปิด {len(light_active)} ดวง โดยมี Spot ที่เป็น Realtime/Mixed และเปิดเงา {len(shadow_lights)} ดวง ไฟฉาย/ไฟบอกใบ้บางดวงปิดไว้ในฉากและจะขึ้นกับ gameplay ต้องดู Profiler ขณะเล่นเพื่อประเมินต้นทุนจริง',
     f'- MeshCollider {len(r["meshColliders"])} ตัว รวม {summary["meshColliderTriangles"]} triangles จากไฟล์ฉาก ไม่ใช่ตัวหนักหลักที่พบ',
     '- ไม่พบ baked occlusion data ที่เชื่อมอยู่ในไฟล์ฉาก การทำ occlusion อาจช่วยชิ้นที่ถูกผนังบัง แต่ควรตรวจผลจริงตามเส้นทางกล้อง',
     '- Scene มีหลายโมเดลใช้ซ้ำ ความหนัก geometry, material slots, shadow passes และงาน CPU ไม่ใช่ตัวเลขเดียวกัน จำนวน Material Slot ใน CSV เป็นขอบเขตงานที่อาจต้องวาด ไม่ใช่ draw call ที่วัดจริง เพราะ batching/instancing/culling อาจเปลี่ยน', '',
     '## ขอบเขตและข้อจำกัด','',
     '- นับ triangles ด้วย Mesh.GetIndexCount()/3 ของ Unity ที่ import แล้ว ไม่ใช่จำนวน faces/quads ใน Blender',
     '- ค่ารวมคือจำนวนที่บันทึกอยู่ในฉาก ไม่ใช่ยอด GPU ต่อเฟรม ไม่มี frustum/occlusion culling หรือ shadow-pass multiplication ในตัวเลขนี้',
     '- โปรเจกต์ตรวจไม่ได้คัดลอกสคริปต์เกม เสียง วิดีโอ หรือใช้ render pipeline ของเกม และปิดสร้าง Lightmap UV เฉพาะสำเนาโมเดล การนับนี้จึงเป็นสถานะก่อน Play Mode ไม่รวม mesh ที่สคริปต์สร้าง/สลับระหว่างเล่น รวมถึง UI/TextMeshPro ที่สร้าง mesh ตอนรัน',
     '- ยังไม่ได้จับ Unity Profiler/Frame Debugger ในเกมจริง จึงยังสรุปไม่ได้ว่า CPU, GPU, เงา หรือสคริปต์เป็นคอขวด FPS',
     '- ยังไม่ลดโมเดลหรือแก้แสงจากงานตรวจครั้งนี้ รายงานทุก instance พร้อม Hierarchy path อยู่ใน mesh-instances.csv; สรุปทุกชนิดอยู่ใน models.csv; แสงอยู่ใน lights.csv','']
(out/'Lawyer-Room-performance.md').write_text('\n'.join(md),encoding='utf-8')
# Cancel a live-editor request if it has not run. Avoid a stale deferred audit.
pending=out/'audit.request.json'
if pending.exists():pending.replace(out/'audit.not-run.json')
print(json.dumps(summary,indent=2));print('Report generated:',out/'Lawyer-Room-performance.md')

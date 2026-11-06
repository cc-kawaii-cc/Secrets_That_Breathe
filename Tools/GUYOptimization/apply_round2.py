from pathlib import Path
import os,subprocess,sys,json
base=Path(__file__).resolve().parent;out=base/'Round2';env=os.environ.copy();env['GUY_OPT_OUTPUT']=str(out)
subprocess.run([sys.executable,'-X','utf8',str(base/'apply.py')],env=env,check=True)
result=json.loads((out/'applied.json').read_text());original=json.loads((base/'applied.json').read_text())
lines=['# GUY — ผลลดโมเดลรอบสอง','',
       '| โมเดล | หลังรอบแรก | หลังรอบสอง |','|---|---:|---:|']
for r in result['models']:lines.append(f'| {Path(r["path"]).stem} | {r["beforeTriangles"]:,} | {r["afterTriangles"]:,} |')
lines+=['',f'รวม {result["beforeTriangles"]:,} → {result["afterTriangles"]:,} triangles ลดเพิ่ม {(1-result["afterTriangles"]/result["beforeTriangles"])*100:.2f}% เทียบรอบแรก',
        f'เทียบต้นฉบับก่อนแก้ {original["beforeTriangles"]:,} triangles ลดรวม {(1-result["afterTriangles"]/original["beforeTriangles"])*100:.2f}%',
        f'ขนาดหกไฟล์รอบแรก {result["beforeBytes"]/1048576:.2f} MiB → {result["afterBytes"]/1048576:.2f} MiB','',
        'ล้างจุดเกือบซ้ำและหน้าเสื่อมสภาพก่อน simplify เพื่อไม่ให้ตัวลดหน้าสูญเสียรูปทรงจาก topology ที่ผิดปกติ คงชิ้นผิวสีและขอบถังเปิด (Bucket_Rim) จากรอบแรก เพราะการลดชิ้นนี้ต่อทำให้ shading เปลี่ยนแม้ระยะผิวจะผ่านเกณฑ์','',
        'ตรวจภาพ Workbench 4 มุมต่อโมเดล และตรวจแสง/texture จริงด้วย Eevee เพิ่มสำหรับ Toolbook2, ToolBox4 และถังสีเปิด ผลภาพอยู่ใน Model0, Model2, Model4/lit-before.png และ lit-after.png','',
        'นำเข้า Unity 6000.3.8f1 ในโปรเจกต์แยกผ่านทั้งหกไฟล์: mesh local file IDs, hierarchy, local transforms และ material slot counts ตรงกับรอบแรก ใช้การทดสอบเดียวกับรอบแรก โดยปิดสร้าง lightmap UV เฉพาะสำเนาตรวจทั้งก่อน/หลัง ค่า Import ในเกมจริงไม่เปลี่ยน','',
        'แก้เฉพาะ FBX หกไฟล์ใต้ Assets/GUY รักษา .meta/GUID, Prefab, Material, Texture และฉากเดิม ใช้วิธีแทน geometry ในโครง FBX เดิม จึงคง IDs และข้อมูล node อื่นไว้','',
        'สำรองรอบแรกก่อนเปลี่ยนอยู่ใน Round2/Backups/Assets/GUY/Model ส่วนต้นฉบับก่อนลดทุกรอบยังอยู่ใน ../Backups/Assets/GUY/Model','',
        'ตัวเลข triangles ในตารางนับจาก FBX ก่อน Unity ตัดหน้าเสื่อมสภาพ/optimize ผล GPU/เวลาโหลดฉากจริงยังไม่ได้ benchmark; ถ้าใช้ baked lightmap ควร Bake ใหม่เมื่อจะตรวจแสงสุดท้าย','']
(out/'RESULTS.md').write_text('\n'.join(lines),encoding='utf-8')

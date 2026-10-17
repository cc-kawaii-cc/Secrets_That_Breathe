# แทน prop ในฉาก Unity ที่เปิดอยู่

บทเรียนจาก Main Door / Lawyer Room วันที่ 2026-10-17 ใช้กับงานแทนโมเดลในฉากเดิม ไม่ใช่ข้อบังคับว่าทุก asset ต้องใช้เครื่องมือแบบเดียวกัน

- ตรวจ mesh จริงก่อนตั้งเป้าลด polygon งานนี้ของเดิมคือ Cube 12 triangles จึงต้องแทน blockout ด้วยโมเดล Lowpoly ที่ออกแบบแล้ว ไม่ใช่ decimate กล่องให้ต่ำลง รายงานจำนวนหลัง triangulation ตามจริง
- ตรวจผนังและพื้นที่รอบ prop ด้วย งานนี้ประตูปิดทับผนังเต็มที่ไม่มีช่อง จึงใช้บานทึบและคงพฤติกรรมเดิม ไม่เพิ่มกระจกใสหรือการเปิดทะลุผนังโดยไม่จัดการ geometry และขอบเขตเกมเพลย์
- เก็บ GlobalObjectId, serialized IDs ของ root/components, transform, collider, child trigger, static flags และ script references ก่อนทำงาน ขนาดช่องเปิดอ้างจากของเดิม; trim เพิ่มได้เมื่อไม่ชนผนัง พื้น หรือวัตถุรอบข้าง
- ถ้า root เดิม scale ไม่เป็น 1 ให้รักษามันเมื่อมีระบบอ้างอิงอยู่ เพิ่ม visual child พร้อม inverse local scale ที่ตรวจจากผล world จริง หลีกเลี่ยงการ reset root แล้วทำให้ collider/target ของเกมเปลี่ยน
- เก็บ renderer/filter เดิมและปิดเฉพาะการแสดงผลของ placeholder เมื่อจำเป็นต่อการรักษา references ใช้วัสดุของ asset ใหม่ อย่าแก้ shared material ของฉากโดยไม่ได้ตรวจผู้ใช้อื่น
- หาก Unity MCP ติดตั้งแต่ไม่มี bridge ทำงาน อย่าเรียกว่ามี live connection ใช้ Blender CLI แบบแยก process ได้ ส่วน Unity editor ที่เปิดอยู่สามารถใช้ Editor-only tool ของโปรเจกต์ที่รับเฉพาะ operation ที่กำหนดผ่านไฟล์ request/result โดยไม่เปิดฉากใหม่ ไม่ใช้ generic code execution หรือเพิ่ม network server ถ้าไม่จำเป็น
- File bridge ต้องใช้ request ID และบริโภค/ลบ request ที่จัดการแล้ว ป้องกัน replay หลัง Unity restart อย่าพึ่ง SessionState อย่างเดียวเพราะข้อมูลหายเมื่อปิด editor; เมื่อไม่มี request ต้องไม่แก้ฉากเอง
- สำรองสถานะฉากที่เปิดอยู่ด้วย SaveScene(saveAsCopy:true) ก่อนแก้ และตรวจ dirty state ไม่เขียนไฟล์ฉากจากนอก editor ทับงานที่เปิดค้างอยู่ ตรวจโมเดลหลังใส่ฉากก่อนบันทึกฉากจริง
- แสงสยองขวัญที่ตั้งใจมืดอาจทำให้ประตูใหม่ดำเกือบเท่า blockout ให้ตรวจภาพด้วยไฟฉายของเกมหรือแสงตรวจชั่วคราว และระบุว่าเป็น diagnostic capture อย่าเพิ่ม emission ให้ไม้หรือเปลี่ยนไฟทั้งห้องเพื่อทำให้ภาพตรวจสว่างขึ้น
- Mesh ที่เพิ่งเพิ่มยังไม่มีผล baked lightmap เดิม แม้ UV2 พร้อมแล้ว รายงานว่าต้อง rebake เมื่อผู้ใช้ต้องการผล GI สุดท้าย แยกการตรวจ geometry/material ภายใต้ไฟฉายออกจากการตรวจผล bake

## Blender CLI และชิ้นบาง

- Blender อาจจบ process ด้วย exit code 0 แม้ Python ใน --python ยก exception งานนี้พบจาก topology validation จึงใช้ --python-exit-code 1 และตรวจ completion marker พร้อมไฟล์ส่งออก อย่าดู exit code อย่างเดียว
- Bevel ของชิ้นบาง เช่น keyhole plate อาจ clamp จนสร้างหน้า area เกือบศูนย์ แม้ไม่มี non-manifold edges ตรวจ degenerate faces ด้วย จำกัด bevel ตามความหนาหรือไม่ bevel รายละเอียดที่ไม่ช่วย silhouette แล้วตรวจซ้ำ
- รวมรายละเอียด rigid ตาม leaf/material ช่วยลด renderer count โดยยังคง pivot แยก งานนี้ source 116 mesh objects รวมสำหรับเกมเหลือ 12 renderers / 3,952 triangles; ตัวเลขนี้เป็นตัวอย่างเฉพาะงาน ไม่ใช่งบตายตัวสำหรับประตูทุกแบบ

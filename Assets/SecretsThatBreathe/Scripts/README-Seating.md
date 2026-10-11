# Lawyer Room: FPS seating

สคริปต์ในโฟลเดอร์นี้ทำเดินมากด E นั่ง Chair Kam พร้อมการเคลื่อนกล้อง แล้วกด E อีกครั้งเพื่อกิน MEMEEE ยังไม่มีเหตุการณ์ป้า

## ใช้งาน

เปิด Assets/Tonpalm/Lawyer Room.unity แล้ว Play เดินเข้า InteractionZone และหันมองเก้าอี้ในมุมกว้าง กด E เมื่อเห็น [E] Sit (จอยใช้ปุ่ม South) ระบบมองศูนย์กลางโมเดลเก้าอี้ ไม่ใช่จุดนั่งหรือ Waypoint
LawyerRoomSeatingBootstrap จะติดตั้ง components และสร้างจุดนั่งตอนโหลด Scene นี้ หากยังไม่ได้ติดตั้งผ่านเมนู โดยไม่เปลี่ยน Scene หรือ prefab บนดิสก์
กล้องจะเคลื่อนลงและก้มเล็กน้อยภายใน 1 วินาที เมื่อจบสามารถใช้เมาส์มองซ้ายขวาได้ 65 องศาและก้มเงยได้ 35 องศา โดยยังเดินไม่ได้
ไม่มีปุ่มลุกในช่วงนี้ เพื่อเตรียมต่อการกินมาม่า

## ปรับตำแหน่ง

หากต้องการปรับและบันทึกค่าก่อน Play ให้เปิด Scene แล้วใช้เมนู Tools > Secrets That Breathe > Setup Lawyer Room Seating จากนั้น Save Scene

- Chair Kam / SeatPoint: ตำแหน่ง capsule ที่ระดับพื้น และทิศหันเข้าโต๊ะ
- Chair Kam / SeatedCameraPoint: ตำแหน่งสายตาตอนนั่ง ค่าเริ่มต้นเหนือพื้น 1.1 เมตร ปรับจาก Scene view ให้ตรงกับเก้าอี้จริง
- PlayerCapsule / SeatCameraAnimation: Duration, Movement curve, Dip Degrees และ Dip Distance ปรับจังหวะการนั่ง
- PlayerCapsule / PlayerSeatController: Yaw Limit, Pitch Limit และ Mouse Sensitivity ปรับการมองตอนนั่ง
- SeatInteractor: Prompt Canvas อ้างอิง SeatPromptUI ที่ควบคุมป้ายบน Canvas; ใช้ InteractionZone บนเก้าอี้กำหนดพื้นที่กด E

## แก้ข้อความกด E บน Canvas


ใช้เมนู Setup Lawyer Room Seating อีกครั้งก่อน Play แล้ว Save Scene จะสร้าง SeatingCanvas > SitPrompt > PromptText และผูกกับ SeatInteractor ให้อัตโนมัติ
เลือก PromptText ใน Hierarchy แล้วแก้ Text Input ใน TextMeshPro - Text (UI) เป็นประโยคที่ต้องการ เช่น [E] นั่งเก้าอี้
ปรับฟอนต์ สี ขนาด และตำแหน่งได้ตามปกติ ระบบไม่เขียนทับข้อความจาก ChairInteractable.prompt อีกต่อไป และไม่ใช้ OnGUI
เมนูตั้งค่าจะเลือก PKFbasicDemo SDF ที่มีอยู่ในโปรเจกต์สำหรับ Canvas ใหม่ เปลี่ยน Font Asset เองได้ โดยใช้ฟอนต์ที่มีตัวอักษรไทยถ้าต้องการข้อความไทย
ป้ายนั่งจะปรากฏเมื่ออยู่ใน InteractionZone และหันมองเก้าอี้ ซ่อนเมื่อหันออก กดนั่ง ออกจากกรอบ หยุดเวลา หรือปิด SeatInteractor
ถ้า Play โดยยังไม่ได้ติดตั้งใน Scene ระบบจะสร้าง Canvas ตอนเล่น; ข้อความที่แก้ระหว่าง Play จะไม่ถูกบันทึกเมื่อหยุด Play
ถ้ามี UI ของตัวเองอยู่แล้ว ใส่ SeatPromptUI บน Canvas แล้วลาก Prompt Panel กับ Prompt Label จากนั้นผูกในช่อง Prompt Canvas ของ SeatInteractor

## กำหนดกรอบพื้นที่ให้ UI ขึ้น

ใช้เมนู Setup Lawyer Room Seating อีกครั้งก่อน Play จะเพิ่ม Chair Kam > InteractionZone โดยรักษาจุดนั่ง ข้อความ และกรอบที่เคยปรับไว้
เลือก InteractionZone ใน Hierarchy แล้วปรับ Box Collider > Center / Size หรือกด Edit Collider เพื่อลากกรอบใน Scene view เปิด Gizmos หากไม่เห็นกรอบ
ใช้ Move tool ย้ายกรอบไปยังตำแหน่งที่ต้องการให้ผู้เล่นเดินถึง ไม่ต้องย้าย SeatPoint หรือ SeatedCameraPoint
กรอบเริ่มต้นกว้าง 2 เมตร ยาว 2 เมตร สูง 2.2 เมตร โดยครอบระดับพื้น; รักษา Is Trigger ให้เปิดเพื่อไม่ให้กรอบขวางทางเดิน
ระบบตรวจตำแหน่ง PlayerCapsule ที่ระดับเท้าในกรอบ พร้อมทิศกล้องเทียบกับศูนย์กลางโมเดลเก้าอี้ ถ้ากรอบเก้าอี้หลายตัวทับกัน จะเลือกตัวที่มองอยู่และจุดนั่งใกล้ผู้เล่นที่สุด
ปรับ Chair Kam > ChairInteractable > Look Angle เพื่อกำหนดมุมกว้าง (เริ่มต้น 35 องศาต่อด้าน) และ Look Distance เพื่อกำหนดระยะกล้องถึงเก้าอี้ (เริ่มต้น 3 เมตร) ไม่จำเป็นต้องมี collider บนโมเดลเก้าอี้
ออกนอกกรอบจะซ่อน UI และกดนั่งไม่ได้ กรอบที่หมุนหรือปรับ scale จะตรวจตามรูปทรงจริง ไม่ใช่กล่องครอบแนวแกนโลก
Save Scene ก่อน Play เพื่อเก็บการปรับกรอบไว้

## ติดตั้งอีกครั้ง / ใช้กับ Scene อื่น

เมนู Tools > Secrets That Breathe > Setup Lawyer Room Seating ใช้กับ Lawyer Room ที่เปิดอยู่ ติดตั้งซ้ำได้และเก็บตำแหน่งจุดนั่งที่ปรับด้วยมือไว้ เมนูนี้ไม่ Save อัตโนมัติ
สำหรับ scene อื่น ใส่ PlayerSeatController, SeatCameraAnimation, SeatInteractor บน PlayerCapsule และใส่ ChairInteractable บนเก้าอี้ที่มี collider แล้วลาก references และจุดนั่งใน Inspector
หากมีสคริปต์เดิน/มอง/โต้ตอบอื่น ให้ใส่ใน Additional Controls เพื่อพักการทำงานระหว่างนั่ง ไม่ใส่ PlayerInput หรือ SeatInteractor ในรายการนี้

## กินมาม่า

หลังนั่งเสร็จ หันกล้องไปทางถ้วยในมุมกว้างจะเห็น [E] Eat noodles ปล่อย E แล้วกดใหม่เพื่อกิน โมเดล MEMEEE เดิมจะถูกยกเข้าหากล้อง เอียง 45 องศา และค่อย ๆ เพิ่มเป็น 60 องศา ก่อนวางกลับตำแหน่งเดิม
ระหว่างกินจะล็อกการมองและป้องกันกดซ้ำ กินจบแล้วไม่สามารถกินซ้ำได้ และยังนั่งอยู่เพื่อเตรียมต่อเหตุการณ์ป้า
NoodleEatingController จะถูกเพิ่มบน PlayerCapsule อัตโนมัติตอนโหลด Lawyer Room โดยใช้ MEMEEE เดิม ไม่สร้างถ้วยสำเนา
เพื่อปรับและบันทึกค่าก่อน Play ใช้เมนู Setup Lawyer Room Seating เพื่อเพิ่ม component ลง Scene แล้ว Save ครั้งเดียว หรือ Add Component > Noodle Eating Controller บน PlayerCapsule เอง แล้วลาก MEMEEE จาก Hierarchy ลงช่อง Noodles

- Held Offset: ตำแหน่งศูนย์กลางโมเดลเทียบกับกล้อง X ซ้ายขวา Y ขึ้นลง Z ระยะหน้า ถ้วยเล็ก/ใหญ่ต่างกันสามารถปรับระยะนี้ได้
- Look Angle: ความกว้างมุมที่ยอมให้มองคลาดจากศูนย์กลางถ้วย ค่าเริ่มต้น 35 องศาต่อด้าน (รวม 70 องศา) เพิ่มเพื่อให้เล็งง่ายขึ้น ลดเพื่อให้ต้องมองตรงถ้วยมากขึ้น โดยศูนย์กลางถ้วยยังต้องอยู่บนหน้าจอ
- Look Distance: ระยะสูงสุดจากกล้องถึงศูนย์กลางถ้วย ค่าเริ่มต้น 2.5 เมตร ไม่ต้องมี collider บนถ้วย เพราะใช้ขอบเขตโมเดลจริงตรวจทิศการมอง
- Sip Movement: ระยะที่ถ้วยขยับเข้าหาปากขณะเอียงจาก 45 ไป 60
- Initial Tilt / Final Tilt: มุมเอียงเริ่มกิน / ปลายการกิน ค่าเริ่มต้น 45 / 60
- Tilt Axis: แกนหมุนเทียบกับกล้อง ค่าเริ่มต้น (-1,0,0) เพื่อเอียงปากถ้วยเข้าหาผู้เล่น เปลี่ยนเครื่องหมายหากทิศของโมเดลที่เปลี่ยนใหม่กลับด้าน
- Lift Duration / Sip Duration / Hold Duration / Lower Duration: เวลาในการยก / ค่อย ๆ เอียง / ค้าง / วางกลับ
- Audio Source / Eating Sound: ใส่เสียงกินได้ ถ้าเว้นว่างจะไม่มีเสียง

ระบบหมุนโมเดลโดยใช้จุดศูนย์กลางภาพ ไม่ใช่ pivot ของไฟล์นำเข้า และคืนตำแหน่ง rotation scale พร้อม collider และ rigidbody เดิมเมื่อวางกลับหรือยกเลิก
แก้ข้อความกินที่ SeatingCanvas > EatPrompt > PromptText ใน Text Input ของ TextMeshPro แยกจากข้อความนั่ง
onMealFinished เรียกหลังวางถ้วยกลับเรียบร้อย ใช้ต่อเหตุการณ์ป้าได้ภายหลัง

## ต่อระบบภายหลัง

PlayerSeatController.State มี Exploring, SittingDown, Seated ใช้ onSeated เชื่อมการแสดงคำสั่งกินมาม่าใน Inspector ได้
ระบบพัก FirstPersonController, CharacterController และ CinemachineBrain ระหว่างนั่ง แล้วควบคุม MainCamera โดยตรง สถานะเปิด/ปิดเดิมจะถูกคืนเมื่อ CancelSeating หรือปิด component
CancelSeating คืนผู้เล่นไปยังตำแหน่งก่อนนั่ง ใช้สำหรับยกเลิกหรือ debug ไม่ใช่แอนิเมชันลุก
ไม่มีการแก้ prefab Player ส่วนกลางหรือสคริปต์ Starter Assets

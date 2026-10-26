> อัปเดต 2026-10-26: บทโทรศัพท์และบทผู้มาขอทำคดีใช้การพิมพ์ทีละคน เลื่อนขึ้นเมื่อขึ้นบรรทัดใหม่ และเปลี่ยนประโยคอัตโนมัติหลังพิมพ์จบ 1 วินาที ผู้มาคุยเปลี่ยนเป็นลุง ช้อยส์เงินไม่มีพื้นหลัง ดูการปรับสี/ตำแหน่งใน Tools/DialogueAudit/README.md ส่วนคำอธิบายป้าและการคลิกผ่านบทด้านล่างเป็นพฤติกรรมเดิมก่อนอัปเดตนี้

# Lawyer Room: FPS seating

สคริปต์ในโฟลเดอร์นี้ทำเดินมากด E นั่ง Chair Kam พร้อมการเคลื่อนกล้อง แล้วกด E อีกครั้งเพื่อกิน MEMEEE ยังไม่มีเหตุการณ์ป้า

## ใช้งาน

### ข้อความกลางห้อง

### ปลดล็อกเก้าอี้และสำรวจเก้าอี้อื่น

ต้องเดินเข้า FindDeskThoughtTrigger แล้วรอข้อความ "นี่โต๊ะเราอยู่ไหนนะ" แสดงครบ Message Duration และหายไปก่อน จึงกด E นั่ง Chair Kam หรือสำรวจ Chair (1), Chair (2), Chair, Chair (3), Chair (4) ได้ ระหว่างข้อความยังแสดงจะไม่ขึ้นป้าย E ของเก้าอี้
Chair Kam ยังทำการนั่งแบบเฟด ส่วนอีก 5 ตัวติด ChairInspectionInteractable ใช้ระยะมอง Look Angle / Look Distance จาก Chair Kam และสร้าง InteractionZone ขนาดเดียวกับกรอบหลักเริ่มต้น
กรอบแต่ละเก้าอี้ปรับที่ลูก InteractionZone > Box Collider ได้ โดยระบบไม่เขียนทับกรอบที่ปรับไว้ ส่วนค่า Look Angle / Look Distance ใช้จากเก้าอี้หลักร่วมกัน
ผู้เล่นกดสำรวจแล้วยังเดินและมองได้ ข้อความเป็นประจำเก้าอี้ ไม่วนตามลำดับที่กดแล้ว เลือกเก้าอี้ > ChairInspectionInteractable > Message Text แล้วลาก PromptText จาก Canvas มาใส่เพื่อเลือกข้อความของตัวนั้น มีข้อความเฉพาะ 5 ตัว: Chair (1)=01_Chair1 "หนังสือนี้เราเคยอ่านตอนสอบ", Chair (2)=02_Chair2 "ทำไมไม่ยอมเก็บแก้วกาแฟนะ", Chair=03_Chair "ไม่น่าใช่โต๊ะนี้", Chair (3)=04_Chair3 "เก้าอี้ตัวนี้ใครนั่งนะ", Chair (4)=05_Chair4 "เราไม่ได้ปิดโน๊ตบุ๊คหนิ"
เก้าอี้แต่ละตัวกดได้ครั้งเดียวต่อการเล่น ตัวที่กดแล้วไม่ขึ้นป้าย E อีก แม้ข้อความหมดเวลาแล้ว ข้อความสำรวจเก่า 4 ตัวถูกแทนที่ด้วยชุดใหม่ครั้งเดียว หลังย้ายแล้วสามารถแก้ Text หรือผูก Message Text เองได้โดยระบบไม่เขียนทับ
เมื่อคลิกจบประโยคขอบคุณของป้า จะล็อกเก้าอี้ทั้งหมดจนจบการเล่นครั้งนี้ ทั้ง Chair Kam และอีก 5 ตัว รวมถึงตู้กดน้ำใน Scene แม้ยังไม่เคยสำรวจ ป้าย E และข้อความสำรวจเดิมถูกซ่อน และลุกแล้วจะกดนั่ง/สำรวจซ้ำไม่ได้ การเดินและ Trigger ข้อความเดินผ่านยังใช้งานตามเงื่อนไขเดิม
ข้อความและฟอนต์แก้ที่ SeatingCanvas > MealAndSearchUI > ChairInspectionUI > 01_Chair1 ถึง 05_Chair4 > PromptText; ป้ายกด E อยู่ที่ InspectPrompt > PromptText
เวลาข้อความแสดงปรับที่ Component ChairInspectionUI บน Canvas > Message Duration เริ่มต้น 5 วินาที ป้ายความคิดกลางห้อง/หาโต๊ะและข้อความสำรวจจะแสดงทีละตัวไม่ซ้อนกัน
ป้ายของเก้าอี้ 5 ตัวเขียน [E] นั่ง และอยู่ตำแหน่งเดียวกับข้อความ เมื่อกดแล้วจะซ่อนป้าย E และเก้าอี้ตัวนั้นจะกดซ้ำไม่ได้ การกดเก้าอี้เหล่านี้ยังเป็นอ่านข้อความ ไม่ล็อกการเดินหรือทำแอนิเมชันนั่ง

เพิ่มอีกกรอบ FindDeskThoughtTrigger สำหรับ "นี่โต๊ะเราอยู่ไหนนะ" ใช้ logic เดียวกันและเก็บ Trigger Once / Message Duration แยกกัน แก้ข้อความที่ SeatingCanvas > MealAndSearchUI > FindDeskThought > PromptText
กรอบใหม่เริ่มต้นห่างจากกรอบแรก 4 เมตร ย้ายตำแหน่งใน Scene ให้ตรงจุดที่ต้องการได้ ระบบไม่ย้ายค่าที่ปรับแล้ว และจะแสดงข้อความความคิดทีละตัวเพื่อไม่ให้ซ้อนกัน

MiddleRoomThoughtTrigger เป็นกรอบ Trigger กลาง Room เมื่อ Player เดินเข้า จะขึ้น "นี่มันกี่โมงแล้วนะ คนอื่นๆกลับกันหมดแล้ว" เป็นเวลา 5 วินาที ครั้งเดียวต่อการเล่น ไม่ต้องมองหรือกด E
เลือก MiddleRoomThoughtTrigger ใน Hierarchy เพื่อย้ายตำแหน่ง และใช้ Box Collider > Edit Collider / Center / Size ปรับพื้นที่ เปิด Is Trigger ไว้ ส่วน Rigidbody ใช้ Is Kinematic และปิด Use Gravity
Message Duration ปรับเวลาแสดง, Trigger Once กำหนดให้ขึ้นครั้งเดียวหรือขึ้นใหม่เมื่อเดินเข้าอีกรอบ
แก้ข้อความ ฟอนต์ สี และขนาดที่ SeatingCanvas > MealAndSearchUI > MiddleRoomThought > PromptText ระบบไม่เขียนทับ Text ที่แก้แล้ว
กรอบเริ่มต้นวางจากขอบเขต Room และระดับ Main Floor ควรตรวจตำแหน่งให้ตรงกับทางเดินกลางห้องจริง และ Save Scene เพื่อเก็บไว้ สคริปต์ Editor เพิ่มให้หลังคอมไพล์โดยไม่ต้องกด Setup

เปิด Assets/Tonpalm/Lawyer Room.unity แล้ว Play เดินเข้า InteractionZone และหันมองเก้าอี้ในมุมกว้าง กด E เมื่อเห็น [E] Sit (จอยใช้ปุ่ม South) ระบบมองศูนย์กลางโมเดลเก้าอี้ ไม่ใช่จุดนั่งหรือ Waypoint
LawyerRoomSeatingBootstrap จะติดตั้ง components และสร้างจุดนั่งตอนโหลด Scene นี้ หากยังไม่ได้ติดตั้งผ่านเมนู โดยไม่เปลี่ยน Scene หรือ prefab บนดิสก์
ตอนกดนั่ง จอจะเฟดดำก่อน ย้ายตัวและกล้องไปยังจุดนั่งตอนดำสนิท ค้างดำ แล้วเฟดกลับ เมื่อจบสามารถใช้เมาส์มองซ้ายขวาได้ 65 องศาและก้มเงยได้ 35 องศา โดยยังเดินไม่ได้
เวลานั่งใช้ค่าชุดเดียวกับตอนลุกบน DrawerSearchScareSequence: Stand Fade Out Duration / Stand Black Hold / Stand Fade In Duration ค่าเริ่มต้น 0.7 / 0.15 / 0.7 วินาที ปรับครั้งเดียวมีผลทั้งนั่งและลุก ไม่มีแอนิเมชันลดกล้อง/ก้มระหว่างนั่งแล้ว
ไฟฉายถูกบังคับปิดทั้ง Light และสถานะที่จำไว้เมื่อกดนั่ง หลังลุกจะคืนปุ่มควบคุมไฟฉายแต่ยังปิดอยู่ ผู้เล่นกด F เพื่อเปิดใหม่เอง ไม่คืนสถานะเปิดจากก่อนนั่ง

### มือถือเป็นไฟฉาย

PhoneFlashlightView บน PlayerCapsule ใช้ Phone_Blank เป็นโมเดลถือที่มุมขวาล่างของ MainCamera ชื่อ HeldPhone จะสร้างตอน Play ไม่แทนมือถือที่วางบนโต๊ะ
กด F เปิด: ยกมือถือพร้อม Screen_Flashlight_Off ที่ใช้ Phone_Flashlight_Off.png รอ Phone On Delay (0.3 วินาที) แล้วเปลี่ยนเป็น Screen_Flashlight_On ที่ใช้ Phone_Flashlight_On.png และเปิด Spot Light เดิมพร้อมเสียงเปิด
กด F ปิด: ดับไฟและเปลี่ยนเป็น Screen_Flashlight_Off ที่ใช้ Phone_Flashlight_Off.png พร้อมเสียงปิด รอ Phone Off Delay (0.3 วินาที) แล้วซ่อนมือถือ
ปรับ Phone On Delay / Phone Off Delay ที่ FirstPersonFlashlight หัวข้อ Phone Presentation และปรับ Screen Position, Camera Distance, Local Rotation, Phone Scale ที่ PhoneFlashlightView
Screen Position เป็นพิกัดจอ 0–1 เริ่มต้น (0.78, 0.27) ที่ขวาล่าง ระยะมือถือถูกกันให้อยู่พ้น near clip ของกล้องเพื่อไม่ให้จมหาย
มีการป้องกันกดซ้ำระหว่างช่วงรอ เปิด/ปิดจะนับตามเวลาเกมและหยุดเมื่อ Pause เมื่อกดนั่งหรือปิด controller จะยกเลิกการเปลี่ยนสถานะที่ค้างอยู่ ปิดไฟ และเก็บมือถือทันที
Prefab มือถือมีไฟและตัวเปลี่ยนหน้าจอของตัวเอง แต่สำเนาที่ถือจะพักทั้งสองส่วนเพื่อให้ใช้ไฟฉาย Player ดวงเดิมเพียงดวงเดียว ไม่มี Collider บนมือถือที่ถือมาขวางผู้เล่นหรือการกด E
PhoneFlashlightSetup เพิ่มและผูกให้ใน Lawyer Room ที่เปิดอยู่หลังคอมไพล์/เปิด Scene กด Ctrl+S ก่อนทำ Build เพื่อเก็บ references ของ prefab และ Material
ไม่มีปุ่มลุกในช่วงนี้ เพื่อเตรียมต่อการกินมาม่า

## ปรับตำแหน่ง

หากต้องการปรับและบันทึกค่าก่อน Play ให้เปิด Scene แล้วใช้เมนู Tools > Secrets That Breathe > Setup Lawyer Room Seating จากนั้น Save Scene

- Chair Kam / SeatPoint: ตำแหน่ง capsule ที่ระดับพื้น และทิศหันเข้าโต๊ะ
- Chair Kam / SeatedCameraPoint: ตำแหน่งสายตาตอนนั่ง ค่าเริ่มต้นเหนือพื้น 1.1 เมตร ปรับจาก Scene view ให้ตรงกับเก้าอี้จริง
- PlayerCapsule / SeatCameraAnimation: ใช้ควบคุมตำแหน่งกล้องเมื่อนั่งและช่วงค้น/คุย ลบช่อง Duration, Movement curve และ Dip ของแอนิเมชันนั่งเดิมแล้ว เวลานั่งปรับจากค่าชุดเฟดเท่านั้น
- PlayerCapsule / PlayerSeatController: Yaw Limit, Pitch Limit และ Mouse Sensitivity ปรับการมองตอนนั่ง
- SeatInteractor: Prompt Canvas อ้างอิง SeatPromptUI ที่ควบคุมป้ายบน Canvas; ใช้ InteractionZone บนเก้าอี้กำหนดพื้นที่กด E

## แก้ข้อความกด E บน Canvas

ข้อความทั้งหมดแก้ที่ Text Input ของ TMP ใน SeatingCanvas ไม่ต้องแก้ประโยคบน PlayerCapsule อีกต่อไป:

```
SeatingCanvas
├─ WaterVendingUI
│  ├─ WaterMachinePrompt
│  └─ WaterMachineMessage
├─ AuntDialogueUI
│  ├─ 01_AuntFirstLine
│  ├─ 02_AuntSecondLine
│  ├─ 03_AuntMoneyLine
│  ├─ 04_KhemRefuseMoney
│  ├─ 05_ThoughtFirst
│  ├─ 06_ThoughtSecond
│  ├─ 07_ThoughtThird
│  ├─ 08_AuntThankYou
│  └─ AuntMoneyChoices
│     ├─ AcceptMoney / Label
│     └─ RefuseMoney / Label
└─ MealAndSearchUI
   ├─ SitPrompt
   ├─ EatPrompt
   ├─ DrawerObjective
   ├─ DrawerSearchPrompt
   └─ DrawerSearching
```

เปิดลูก PromptText หรือ Text ของแต่ละแผงแล้วแก้ Text Input ฟอนต์ ขนาด สี และตำแหน่งได้เป็นรายประโยค บทพูดป้าใช้ Text แยก 8 ตัว ระบบสลับการแสดง ไม่เขียนทับข้อความระหว่างเล่น
ลบ Prompt ของ ChairInteractable และช่องข้อความสำรองเก่าทั้ง 7 ช่องบน PlayerCapsule แล้ว Canvas เป็นแหล่งข้อความที่ใช้งานจริงทั้งหมด ค่าเริ่มต้นในสคริปต์ UI ใช้เฉพาะตอนสร้าง Text ที่ยังไม่มีเท่านั้น
สคริปต์ Editor จัดกลุ่มและย้ายให้หลังคอมไพล์/เปิด Scene โดยคง references ของตู้น้ำและตำแหน่งแผงไว้ กด Ctrl+S เพื่อเก็บการจัดกลุ่ม ไม่ต้องกด Setup ซ้ำ


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

หลังนั่งและเฟดกลับเสร็จ รอ 3 วินาที แสดง "ทำงานเหนื่อนมาทั้งวันแล้ว เติมพลังหน่อยดีกว่า" 3 วินาที แล้วเว้น 1 วินาทีก่อนขึ้นให้กด E กิน โดยยังต้องหันมองถ้วยตามเงื่อนไขเดิม ระหว่างรอจะกดกินไม่ได้
แก้ประโยคที่ SeatingCanvas > MealAndSearchUI > BeforeEatingThought > PromptText และปรับเวลาที่ NoodleEatingController > Before Eating Delay / Before Eating Text Duration / After Eating Text Delay

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
แก้ข้อความกินที่ SeatingCanvas > MealAndSearchUI > EatPrompt > PromptText ใน Text Input ของ TextMeshPro แยกจากข้อความนั่ง
onMealFinished เรียกหลังวางถ้วยกลับเรียบร้อย ใช้ต่อเหตุการณ์ป้าได้ภายหลัง

### เปลี่ยนเป็นถ้วยที่กินหมดแล้ว

นำโมเดลถ้วยเปล่ามาวางใน Scene ที่ตำแหน่งเดียวกับ MEMEEE ปรับขนาดและทิศให้ตรงกัน และวางเป็น GameObject แยกข้างกัน ไม่วางเป็นลูกของ MEMEEE หรือ Player
ลากโมเดลถ้วยเปล่าจาก Hierarchy ลงช่อง Empty Cup Model ของ NoodleEatingController แล้ว Save Scene ไม่ต้องติดตั้ง Setup ใหม่
ตอนเริ่มเล่นระบบซ่อนถ้วยเปล่า หลังแอนิเมชันกินและวางกลับเสร็จ จะซ่อน MEMEEE และเปิดถ้วยเปล่า ก่อนเรียก onMealFinished
ถ้ายกเลิกก่อนกินจบจะยังใช้ถ้วยเต็ม หากไม่ใส่ Empty Cup Model จะวางถ้วยเดิมกลับเหมือนเดิม
ระบบไม่ย้ายโมเดลถ้วยเปล่าอัตโนมัติ เพื่อให้จัดตำแหน่งได้ตรงแม้โมเดลสองตัวมี pivot ต่างกัน ต้องลากตัวที่อยู่ใน Scene ไม่ใช่ prefab จาก Project

## ต่อระบบภายหลัง

## ค้นเอกสารและ Jump Scare

DrawerSearchScareSequence บน PlayerCapsule ต่อจาก NoodleEatingController.State == Finished โดยอัตโนมัติ
1. หลังกินและวางถ้วยกลับ รอ 1 วินาที แสดง "อยากกลับบ้านแล้ว" 2 วินาที เว้น 1 วินาที แสดง "ทำไมฉันไม่ทิ้งงานแล้วหนีกลับบ้านไปเลยล่ะ" 2 วินาที เว้น 1 วินาที แสดง "ก็เป็นความคิดที่ดี" 2 วินาที เว้น 1 วินาที แล้วขึ้นคำสั่ง "เอกสารที่ต้องเซ็นต์อยู่ลิ้นชักฝั่งซ้าย" ระหว่างนี้ยังมองรอบตัวได้ แต่ยังค้นลิ้นชักไม่ได้
2. ขณะยังนั่ง หันมอง 3-tier cabinet (25) ในมุมกว้างแล้วกด E เพื่อค้นเอกสาร ไม่ใช้กรอบ InteractionZone ของเก้าอี้ในขั้นนี้
3. กดค้นแล้วกล้องหันซ้ายเพิ่ม 15 องศา ก้ม 6 องศา และขยับเข้าลิ้นชักเล็กน้อยใน 0.45 วินาที จากนั้นค้น 2.5 วินาทีโดยกล้องสั่นเบา ๆ แล้วเปิด Capsule ป้าให้เคลื่อนจากตำแหน่งเดิมไปทางขวาของ Table Kam ใน 1.2 วินาที
4. เล่นเสียงตกใจหนึ่งครั้ง พร้อมหันกล้องไป AuntFaceTarget และซูม FOV เป็น 30 ภายใน 0.18 วินาที
5. ค้างมุมซูมหน้าป้า รอ 0.5 วินาทีหลังซูมเสร็จ แล้วขึ้นประโยคแรก ข้อความไม่หมดเวลา คลิกซ้ายเพื่อไปประโยคที่สอง และประโยคที่สามที่ป้าขอจ้าง
6. คลิกซ้ายหลังประโยคขอจ้างเพื่อแสดงช้อยส์ รับเงิน (บน) / ไม่รับเงิน (ล่าง) เมาส์จะปลดล็อกเฉพาะตอนเลือก ส่วนกล้องยังค้างที่หน้าป้า
7. เลือกรับเงินครั้งที่ 1/2/3 แสดงความคิดในใจตามลำดับ ใส่เครื่องหมาย "" รอบประโยค คลิกซ้ายเพื่อกลับช้อยส์ ครั้งที่ 4 วนกลับความคิดแรก แล้ววน 1/2/3 ต่อไปไม่จำกัด
8. เลือกไม่รับเงินได้ทุกครั้ง จะแสดงประโยคเข้มรับทำคดีให้ฟรี และหยุดวงวน คลิกซ้ายเพื่อขึ้น "ขอบคุณมากนะลูก" แล้วคลิกซ้ายอีกครั้งเพื่อคืน FOV ปกติ โดยยังหันมองป้า ก่อนป้าเดินไป Main Door
9. ป้าเดิน 2 วินาที แล้วจอค่อย ๆ มืด ย้ายผู้เล่นไปยืนและซ่อนป้าเมื่อจอดำสนิท จากนั้นเฟดกลับและคืนการเดิน ไม่ต้องรอป้าถึงประตู

ระบบจะเพิ่มสคริปต์ให้ Player ที่ติดตั้งระบบนั่งแล้วใน Scene ที่เปิดอยู่หลังคอมไพล์ เพื่อให้ใส่เสียงก่อน Play ได้ กด Ctrl+S เพื่อเก็บไว้ ไม่ต้องกด Setup ซ้ำ
ถ้าเปิดเล่นโดยยังไม่ได้ติดตั้งแบบถาวร bootstrap จะเพิ่มให้เฉพาะ Lawyer Room ตอนโหลด Scene

- Drawer / Desk: ผูกกับ 3-tier cabinet (25) / Table Kam โดยเก็บค่าที่ลากเองไว้
- Aunt Object: Capsule ที่แยกจาก Player และไม่ใช่ลูกของ Player เปลี่ยนเป็นโมเดลป้าภายหลังได้
- LawyerRoomStoryPoints > AuntStandPoint: จุดหยุดทางขวาของโต๊ะและทิศหันของ Capsule ปรับให้พ้นโต๊ะ/กำแพง
- Capsule > AuntFaceTarget: จุดหน้าที่กล้องมองและซูม ปรับความสูงสำหรับ Capsule หรือโมเดลป้า
- Scare Sound: ลาก AudioClip เสียงตกใจลงช่องนี้ได้เลย ถ้าไม่ใส่ Scare Audio Source ระบบสร้างตัวเล่นเสียง 2D ให้อัตโนมัติเมื่อถึงจังหวะ
- Search Duration / Approach Duration: ระยะเวลาค้นและเวลาป้าเคลื่อนเข้ามา
- After Meal Thought Delay / Duration / Gap: เวลาก่อนประโยคแรก / เวลาแสดงแต่ละประโยค / ช่องว่างระหว่างความคิด 3 ประโยค ค่าเริ่มต้น 1 / 2 / 1 วินาที ใช้เวลาเกมและหยุดนับเมื่อ Pause
- Before Drawer Objective Delay: เวลารอเฉพาะหลัง "ก็เป็นความคิดที่ดี" หายไป ก่อนขึ้นคำสั่งเอกสารที่ลิ้นชัก ค่าเริ่มต้น 1 วินาที ปรับแยกจาก Gap ได้
- แก้ความคิดหลังมาม่าที่ SeatingCanvas > MealAndSearchUI > AfterEatingThought1, AfterEatingThought2, AfterEatingThought3 > PromptText; แก้คำสั่งลิ้นชักที่ DrawerObjective > PromptText
- Search Extra Left Angle / Search Pitch Angle: องศาหันซ้ายเพิ่มเติม / ก้มเงยระหว่างค้น เป็นมุมเพิ่มเติมจากทิศที่มองตอนกด E
- Search Lean Offset: ระยะขยับกล้องเทียบกับทิศเดิม X ลบไปซ้าย, Y ลบลง, Z บวกไปข้างหน้า; เริ่มต้น (-0.08, -0.035, 0.16) เมตร
- Search Move Duration: เวลาขยับเข้าท่าค้น เริ่มต้น 0.45 วินาที แยกจาก Search Duration
- Search Shake Position / Rotation / Frequency: ระยะสั่นเป็นเมตร / มุมสั่นเป็นองศา / ความเร็วสั่น เริ่มต้น 0.008 / 0.6 / 10 ตั้ง Position และ Rotation เป็น 0 เพื่อปิดสั่น
กล้องสั่นเฉพาะช่วงแสดงกำลังค้นหา รวมช่วงป้าเข้ามาก่อนตกใจ และหยุดสั่นเมื่อเริ่มหันซูมหน้า คืนตำแหน่งเดิมหลังจบหรือยกเลิก
- Snap Duration / Scare Field Of View: เวลาหันเร็ว / ระดับซูม
- Dialogue Start Delay: เวลารอหลังหันซูมเสร็จก่อนขึ้นบทพูด ค่าเริ่มต้น 0.5 วินาที
- ข้อความป้า / คำตอบเข้ม / ความคิดในใจ: แก้บน Text ของ 01–08 ใน SeatingCanvas > AuntDialogueUI โดยตรง ความคิดเริ่มต้นมีเครื่องหมาย "" เก็บไว้ใน Text Input
- Money Choice UI: ช้อยส์บน Canvas ที่ SeatingCanvas > AuntDialogueUI > AuntMoneyChoices > AcceptMoney / RefuseMoney ปรับข้อความบน Label และปรับขนาด ตำแหน่ง และฟอนต์ได้บน Canvas
- Aunt Dialogue Text: reference ของข้อความที่กำลังแสดง ส่วนต้นฉบับทุกประโยคอยู่บน TMP ของตัวเองใน AuntDialogueUI
- Return Duration: เวลาคืนกล้องเมื่อเหตุการณ์ถัดไปเรียก FinishDialogue หลังเลือกไม่รับเงิน ไม่คืนกล้องอัตโนมัติระหว่างคุยหรือเลือกช้อยส์
- Look Angle / Look Distance: เงื่อนไขมองลิ้นชัก เริ่มต้น 40 องศาต่อด้านและ 4 เมตร

แก้ข้อความใน Canvas ที่ SeatingCanvas > DrawerObjective, DrawerSearchPrompt, DrawerSearching > PromptText โดยระบบไม่เขียนทับข้อความที่ปรับแล้ว
Capsule ใช้การเคลื่อนตำแหน่งตรงไปยังจุดหมาย ยังไม่มีแอนิเมชันเดินหรือการหลบสิ่งกีดขวาง จัดตำแหน่งเริ่มกับจุดหยุดให้เส้นทางไม่ผ่านโต๊ะ
ระบบพักการมองระหว่างค้น/ตกใจและบทสนทนา กล้องค้างที่หน้าป้าจนคลิกซ้ายผ่านประโยคขอบคุณ คืน FOV แต่ยังคงมุมหันมองป้า จนถึงเฟดลุกจากเก้าอี้ ใช้ onSequenceFinished เมื่อเฟดกลับและคืนการเดินเรียบร้อย

### ป้าขอบคุณและเดินออก

แก้คำขอบคุณที่ SeatingCanvas > AuntDialogueUI > 08_AuntThankYou > PromptText โดยตรง
Main Door จะถูกผูกให้โดยชื่อใน Scene และสร้าง LawyerRoomStoryPoints > AuntExitPoint ใกล้ประตู ปรับจุดนี้ให้ตรงกับทางออกและระดับความสูงของ Capsule
DrawerSearchScareSequence > Exit Walk Speed กำหนดความเร็วเดินออก, Exit Turn Speed กำหนดความเร็วหมุน ป้าจะซ่อนตอนเฟดดำ ไม่ต้องรอถึงประตู
Automatic Stand-up Fade: Aunt Walk Before Stand เริ่มต้น 2 วินาที, Stand Fade Out Duration / Stand Fade In Duration เริ่มต้น 0.7 วินาที และ Stand Black Hold เริ่มต้น 0.15 วินาที
Stand Up Point ใส่จุดยืนใหม่ข้างเก้าอี้ได้ ถ้าว่างจะใช้ตำแหน่งปลอดภัยที่ผู้เล่นยืนก่อนกดนั่ง ตอนจอดำระบบคืนระดับกล้องยืนและปิดการควบคุมไว้จนเฟดกลับครบ
แผงเฟดอยู่ที่ SeatingCanvas > MealAndSearchUI > StandUpFade ใช้ CanvasGroup Alpha และ Canvas ชั้นสูงเพื่อคลุมทั้งจอ ถ้ายกเลิกจะเคลียร์จอดำและคืนการควบคุม
หากเส้นตรงผ่านเฟอร์นิเจอร์ ให้สร้างจุดกลางทาง แล้วลากใส่ Aunt Exit Waypoints ตามลำดับ ระบบจะไปครบก่อนถึง AuntExitPoint; Capsule ยังใช้การเคลื่อนตำแหน่ง ไม่มีแอนิเมชันเดินจริง

PlayerSeatController.State มี Exploring, SittingDown, Seated ใช้ onSeated เชื่อมการแสดงคำสั่งกินมาม่าใน Inspector ได้
ระบบพัก FirstPersonController, CharacterController และ CinemachineBrain ระหว่างนั่ง แล้วควบคุม MainCamera โดยตรง สถานะเปิด/ปิดเดิมจะถูกคืนเมื่อ CancelSeating หรือปิด component
CancelSeating คืนผู้เล่นไปยังตำแหน่งก่อนนั่ง ใช้สำหรับยกเลิกหรือ debug ไม่ใช่แอนิเมชันลุก
ไม่มีการแก้ prefab Player ส่วนกลางหรือสคริปต์ Starter Assets

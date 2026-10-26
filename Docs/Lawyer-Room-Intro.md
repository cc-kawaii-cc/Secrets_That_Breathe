# Lawyer Room Intro

เปิด `Assets/Tonpalm/Lawyer Room Intro.unity` แล้วกด Play เพื่อทดสอบหน้า Intro พร้อมโหลด Lawyer Room เบื้องหลัง Scene นี้ถูกเพิ่มเป็น Scene แรกที่เปิดใช้งานใน Build Settings แล้ว

ใน Lawyer Room ไม่มี Canvas Intro วางค้างไว้แล้ว เมื่อ Play จาก Lawyer Room โดยตรง ระบบจะสร้าง Canvas จาก Resources Prefab เฉพาะตอนเล่น แต่ Scene โหลดเสร็จก่อนแสดง Intro ถ้าเข้าผ่าน Scene Intro จะใช้ Canvas ที่กำลังแสดงอยู่ต่อไปโดยไม่สร้างซ้ำ

## แก้ข้อความและสี

เปิด Prefab `Assets/Resources/LawyerRoomIntroCanvas.prefab`

- `IntroTop`: ข้อความชุดบน
- `IntroMiddle`: ข้อความชุดกลาง
- `IntroBottom`: ข้อความชุดล่าง
- `PressAnyKey`: ข้อความกดเริ่มเกมด้านล่าง

แก้ Text Input, Vertex Color, Font Size และ Rect Transform ของ TextMeshPro ได้โดยตรง สีข้อความสามชุดเริ่มที่ C0C000 และข้อความกดเริ่มเป็นสีขาว ใช้ Kanit Medium ขนาด 30 เหมือนข้อความเดิม

Canvas Group ของข้อความเริ่มที่ Alpha 0 ถ้าต้องการดูตำแหน่งใน Prefab Mode ให้ปรับ Alpha เป็น 1 ชั่วคราว ตัว Intro จะตั้ง Alpha กลับเป็น 0 เองเมื่อเริ่มเกม

## แก้เวลา

เลือก root ของ Prefab แล้วดู Lawyer Room Intro component:

- Text Fade Seconds: เฟดข้อความแต่ละชุดเข้า เริ่มที่ 1 วินาที
- Paragraph Hold Seconds: เวลารอหลังข้อความขึ้นครบ ก่อนเริ่มชุดถัดไป เริ่มที่ 3 วินาที
- Prompt Fade Seconds: เฟด Press any key เข้า เริ่มที่ 1 วินาที
- Fade To Black Seconds: เฟดตัวอักษรออกหลังผู้เล่นกด เริ่มที่ 1 วินาที
- Black Hold Seconds: ค้างภาพดำก่อนเปิดเข้าเกม เริ่มที่ 0.25 วินาที
- Reveal Game Seconds: เฟดจากดำเข้าเกม เริ่มที่ 2 วินาที

ข้อความแต่ละชุดจะค้างสะสมจนผู้เล่นกดเริ่มเกม การกดก่อน Prompt พร้อมหรือกดค้างไว้ก่อนหน้านั้นจะไม่ข้าม Intro

Prefab ตั้ง Load Gameplay Scene เป็นปิดไว้สำหรับการ Play จาก Lawyer Room โดยตรง Scene Lawyer Room Intro มี override เปิดค่านี้เพื่อโหลดเกมจริง ไม่ต้องเปลี่ยนค่า default ของ Prefab

## การทำงานและการตรวจ

หน้าโหลดใช้ LoadSceneAsync และรอ Scene activation กับ texture streaming ที่ค้างอยู่ก่อนขึ้น Prompt ช่วง Intro ใช้เวลาจริงในการเฟด ส่วนเวลา gameplay ถูกพักไว้ที่ timeScale 0 พร้อมปิดคอนโทรลเดิน มอง และไฟฉายที่ตรวจพบ คืนค่าคอนโทรลที่เดิมเปิดใช้งานเมื่อเฟดเข้าเกมเสร็จเท่านั้น

ไม่ได้เปลี่ยนค่าไฟ กล้อง ตำแหน่ง UI หรือ dialogue เดิม เอา Prefab Intro ที่เคยวางเป็น root ใน Lawyer Room ออกแล้ว และสร้างเฉพาะตอนเล่น

ตรวจคอมไพล์ runtime/editor และตรวจ reference ของ Prefab/หน้าโหลด รวมถึงเทียบออบเจ็กต์ Scene เดิมกับสำเนาก่อนแก้แล้ว ยังไม่ได้ทดสอบ Play Mode หรือ build จริง

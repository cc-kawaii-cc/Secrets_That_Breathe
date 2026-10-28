# Lawyer Room Intro (ซีนเดียว)

เปิด `Assets/MainScenes/From_PM/Lawyer Room.unity` แล้วกด Play หรือ Build ตามปกติ ไม่ใช้ Scene Intro แยกอีกแล้ว

Canvas อยู่ใน Hierarchy ที่ `SeatingCanvas/LawyerRoomIntroCanvas` โดยปิด GameObject ไว้ตอนแก้ Scene และระบบเปิดให้อัตโนมัติตอน Play

## แก้ข้อความและสี

เปิด Scene Lawyer Room แล้วขยาย `SeatingCanvas > LawyerRoomIntroCanvas` เลือก Text ที่ต้องการแก้ได้ใน Scene นี้โดยตรง

- `IntroTop`: ข้อความชุดบน
- `IntroMiddle`: ข้อความชุดกลาง
- `IntroBottom`: ข้อความชุดล่าง
- `PressAnyKey`: ข้อความกดเริ่มเกมด้านล่าง
- `Intro2Text`: ข้อความสีขาวขนาด 40 กลางจอ หลัง Intro 1 เฟดดำ

แก้ Text Input, Vertex Color, Font Size และ Rect Transform ของ TextMeshPro ได้โดยตรง สีข้อความสามชุดเริ่มที่ DDDD00 และข้อความกดเริ่มเป็นสีขาว ใช้ Kanit Medium ขนาด 30 เหมือนข้อความเดิม

ถ้าต้องการดูตัวอย่าง ให้ติ๊กเปิด GameObject `LawyerRoomIntroCanvas` ชั่วคราว แล้วปิดกลับหลังแก้เสร็จเพื่อไม่ให้บัง Scene ตัว Intro จะตั้ง Alpha ของข้อความเริ่มที่ 0 เองตอน Play

## แก้เวลา

เลือก `LawyerRoomIntroCanvas` แล้วดู Lawyer Room Intro component:

- First Text Delay Seconds: รอจอดำก่อนข้อความชุดแรกเริ่มเฟดขึ้น เริ่มที่ 1.5 วินาที ปรับเป็น 0 เพื่อไม่รอ ไม่กระทบเวลารอของชุดถัดไป
- Text Fade Seconds: เฟดข้อความแต่ละชุดเข้า เริ่มที่ 1 วินาที
- Paragraph Hold Seconds: เวลารอหลังข้อความขึ้นครบ ก่อนเริ่มชุดถัดไป เริ่มที่ 3 วินาที
- Prompt Fade Seconds: เฟด Press any key เข้า เริ่มที่ 1 วินาที
- Fade To Black Seconds: เฟดตัวอักษรออกหลังผู้เล่นกด เริ่มที่ 1 วินาที
- Black Hold Seconds: ค้างภาพดำก่อนเปิดเข้าเกม เริ่มที่ 0.25 วินาที
- Reveal Game Seconds: เฟดจากดำเข้าเกม เริ่มที่ 2 วินาที

ข้อความแต่ละชุดจะค้างสะสมจนผู้เล่นกดเริ่มเกม การกดก่อน Prompt พร้อมหรือกดค้างไว้ก่อนหน้านั้นจะไม่ข้าม Intro

## การทำงานและการตรวจ

Intro แสดงบน Lawyer Room ซีนเดียว ขณะฉากเริ่มทำงานและแสดงเฟรมแรก ระบบรอ texture streaming ทั้ง pending และ loading ให้เป็นศูนย์ต่อเนื่อง 3 เฟรมก่อนแสดง Prompt ช่วง Intro ใช้เวลาจริงในการเฟด ส่วนเวลา gameplay ถูกพักไว้ที่ timeScale 0 พร้อมปิดคอนโทรลเดิน มอง และไฟฉายที่ตรวจพบ คืนค่าคอนโทรลเมื่อเฟดเข้าเกมเสร็จ พร้อมล็อกและซ่อนเคอร์เซอร์

ข้อจำกัด: การอ่านไฟล์ Scene และทรัพยากรที่จำเป็นต่อการเปิด Scene ของ Unity เกิดก่อน Canvas ปรากฏ วิธีนี้รอการเตรียมฉากและ texture streaming หลังเปิด Scene ไม่ได้โหลด Scene เดิมซ้ำเบื้องหลัง และไม่ได้ตรวจงาน async ที่เขียนขึ้นเองในระบบอื่น

ไม่ได้เปลี่ยนค่าไฟ กล้อง ตำแหน่ง UI หรือ dialogue เดิม Intro ถูกเก็บไว้ใต้ SeatingCanvas และเปิดเฉพาะตอนเล่น

ตรวจคอมไพล์ runtime/editor และตรวจ reference ของ Intro Canvas รวมถึงเทียบออบเจ็กต์ Scene เดิมกับสำเนาก่อนแก้แล้ว ยังไม่ได้ทดสอบ Play Mode หรือ build จริง

## Intro 2

แก้ข้อความที่ `SeatingCanvas/LawyerRoomIntroCanvas/Intro2Text` ค่าเริ่มต้นคือ “ณ วันธรรมดาวันหนึ่งที่ออฟฟิศทนายเข้ม...” สีขาว ขนาด 40 จัดกลางจอ

หลัง Intro 1 เฟดเป็นดำ ระบบเริ่มแสดง Intro 2 ด้วยความทึบ 100% และพิมพ์ทีละตัว ไม่มีเฟดเข้าและไม่ต้องกดปุ่มเพิ่มเติม พิมพ์จบค้าง 1 วินาที แล้วเฟดข้อความออก 1 วินาที ก่อนเฟดเข้าเกมตามเดิม คอนโทรลยังถูกล็อกจนภาพเกมเฟดเสร็จ

ปรับบน component Lawyer Room Intro: Second Intro Seconds Per Character (0.05), Second Intro Hold Seconds (1), Second Intro Fade Out Seconds (1)

## เสียงตอนเปิดเข้าเกม

RoomAmbience และ AirConditionerAudio ทั้งสองตัวถูกปิด Play On Awake และผูกใน Gameplay Audio Sources ของ Lawyer Room Intro จึงยังไม่เล่นระหว่าง Intro 1/2 เริ่มเล่นที่ความดัง 0 เมื่อเริ่มเปิดภาพเกม แล้วเฟดภาพและความดังด้วยความคืบหน้า Linear เดียวกัน ระยะเวลาใช้ Reveal Game Seconds ทั้งคู่

100% ของเสียงหมายถึง Volume ที่ตั้งไว้บน AudioSource แต่ละตัวก่อนเริ่ม Intro เช่น Volume 0.4 จะค่อยขึ้นจาก 0 ถึง 0.4 โดยไม่เปลี่ยนค่าความดังเดิมให้เป็น 1 ทั้งหมด เปลี่ยนระดับปลายทางได้ที่ AudioSource ตามปกติ หากเพิ่มเสียงบรรยากาศใหม่ ให้ปิด Play On Awake และเพิ่มลงใน Gameplay Audio Sources

## เสียงพิมพ์ Intro 2

ใช้ Keyboard Typing By Nemo ระหว่างพิมพ์ข้อความ Intro 2 เท่านั้น เริ่มพร้อม typewriter และหยุดเมื่ออักษรสุดท้ายขึ้น รวมถึงหยุดเมื่อ Intro ถูกปิด ใช้ AudioSource ที่ `SeatingCanvas/LawyerRoomIntroCanvas/Intro2TypingAudio` ปรับ Volume ได้ตรงนี้ ค่าเริ่มต้น 0.35, 2D, Loop เปิด และ Play On Awake ปิด

ไฟล์ต้นฉบับ `Assets/Keyboard Typing By Nemo.m4a` ยังคงอยู่ แปลงสำเนาที่ใช้กับ Unity เป็น `Assets/SecretsThatBreathe/Audio/Keyboard Typing By Nemo.wav` (PCM 16-bit stereo 44100 Hz) เพราะต้นฉบับถูก import เป็น DefaultImporter

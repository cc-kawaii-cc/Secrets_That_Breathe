# ทางออก Lawyer Room

หลังจบ sequence ลุงป้องและข้อความเป้าหมาย “ไปที่ประตูทางออก” แสดงขึ้น ประตู OfficeGlassDoor จึงเริ่มให้โต้ตอบได้ ผู้เล่นต้องอยู่ใน InteractionZone และมอง LookTarget เพื่อแสดง `[E] ออก` ใช้ E หรือปุ่มล่างของ gamepad

เมื่อกด ระบบล็อกคอนโทรล เฟดดำ 1.5 วินาที และเริ่มโหลด `Assets/MainScenes/From_PM/MainScene_2_Evening.unity` หลังภาพดำเต็มจอ กันกดซ้ำขณะเปลี่ยนฉาก

แก้ป้ายได้ใน Lawyer Room ที่ `SeatingCanvas/OfficeExitUI/ExitPrompt/PromptText` แก้เวลาเฟดและซีนปลายทางที่ component Office Door Exit Interaction บน `OfficeGlassDoor` ส่วนพื้นดำอยู่ที่ `OfficeExitUI/ExitFade`

ใช้ระบบ Box Collider เหมือนตู้กดน้ำ โดยเพิ่ม child `OfficeGlassDoor/InteractionZone` และ `OfficeGlassDoor/LookTarget` เป็น trigger แยกกัน ปรับขนาด/ตำแหน่งด้วย Edit Collider ได้โดยตรง ไม่เปลี่ยน geometry ของ prefab ประตูหรือการเดินของ NPC ซีนปลายทางเปิดใช้งานใน Build Settings แล้ว

ตรวจคอมไพล์และ reference ใน Scene แล้ว ยืนยันว่า geometry และ transform เดิมของประตูไม่เปลี่ยน ยังไม่ได้ทดสอบ Play Mode หรือ Build จริง

## ข้ามเพื่อทดสอบ

กด F4 ระหว่าง gameplay เพื่อข้ามไป checkpoint “ไปที่ประตูทางออก” ระบบปิดโทรศัพท์/เอกสาร/บทสนทนา/ตัวเลือกที่ค้าง คืนการควบคุมการเดิน ซ่อนลุงป้อง และแสดงเป้าหมายประตูทันที จากนั้นยังต้องเดินเข้า InteractionZone มอง LookTarget และกด E ตามปกติ ไม่ย้ายผู้เล่นไปประตูอัตโนมัติ

เปิด/ปิดปุ่มลัด F1–F4 ทั้งหมดด้วย Enable Testing Shortcuts บน Document Search Sequence ที่ SeatingCanvas/DocumentSearchUI ปุ่มนี้ไม่ทำงานขณะ Intro หรือช่วงที่ timeScale เป็น 0

## เปิด/ปิดป้ายปุ่มลัด

ปิด GameObject `SeatingCanvas/TestingShortcutHints` เพื่อซ่อนป้าย F1–F4 ทั้งชุด โดยปุ่มลัดยังทำงานตาม Enable Testing Shortcuts ปิดแต่ละ child TestSkip…Hint เพื่อซ่อนเฉพาะปุ่มนั้นได้ โค้ดจะไม่เปิด UI กลับเองอีก

## ความคิดหลังคุยลุงป้อง

หลังภาพลุกจากเก้าอี้สว่างและคืนคอนโทรล รอ Exit Objective Delay (3 วิ) แล้วแสดงทีละประโยค: “ลุงเขาน่าสงสารจัง” → “ได้ช่วยคนอีกแล้วสินะเรา” → “แต่ลุงแม่งมาไม่ให้สุ้มให้เสียง ตกใจหมด” ก่อนขึ้นเป้าหมาย “กลับไปที่ประตูทางออก”

แก้ข้อความได้ที่ `SeatingCanvas/MealAndSearchUI/AfterUncleThought1` ถึง `AfterUncleThought3` ใน PromptText รูปแบบและตำแหน่งใช้จาก OfficeIntroLine1 โดยตรง ปรับ After Uncle Thought Duration (3 วิ) และ After Uncle Thought Gap (2 วิ รวมเว้นก่อนเป้าหมาย) ที่ Drawer Search Scare Sequence ของ Player

F4 ยังคงข้ามตรงไปยังเป้าหมายประตูสำหรับทดสอบทางออก

ตัวเปิด Intro ตรวจชื่อ Scene และทางออกใช้ชื่อ `MainScene_2_Evening` แทนพาธ เพื่อรองรับการย้ายโฟลเดอร์ Scene ปลายทางต้องเปิดใน Build Settings และมีชื่อนี้เพียง Scene เดียว

# Evening Sky

Skybox Material โทนเย็น อ้างอิง MainScene_2_Evening: ฟ้าน้ำเงินหม่น ขอบฟ้าอุ่น เมฆส้มแดง และดวงอาทิตย์ต่ำ เป็นท้องฟ้าคงที่ที่สร้างด้วย Shader ไม่ต้องใช้ Tenkoku หรือภาพ Texture

## ใช้งานใน Scene ที่เปิดอยู่

เลือก Tools > Secrets That Breathe > Sky > Apply Evening Sky To Active Scene แล้วบันทึก Scene คำสั่งนี้รองรับ Undo และไม่บันทึกทับงานที่ยังไม่ได้เซฟโดยอัตโนมัติ

คำสั่งจะตั้ง EveningSky.mat เป็น Skybox และปิดตัวควบคุม สคริปต์ กล้อง และ Renderer ภายใต้ Tenkoku DynamicSky เพื่อไม่ให้ท้องฟ้าเดิมซ้อนหรือเขียน Material กลับ ค่าไฟ ตำแหน่งไฟ Fog และ Ambient Lighting เดิมจะถูกเก็บไว้ ระบบเวลาและสภาพอากาศของ Tenkoku จะหยุดทำงานใน Scene ที่ใช้วิธีนี้

## ปรับ Material

- Sky Brightness: ความสว่างของท้องฟ้ารวม ไม่ใช่ความแรง Directional Light
- Cloud Coverage / Cloud Scale / Sunset Cloud Color: ปริมาณ ขนาด และสีของเมฆ
- Sun Direction / Sun Height / Sun Brightness: ตำแหน่งและความสว่างของดวงอาทิตย์ที่วาดในท้องฟ้า ไม่เปลี่ยนทิศทางไฟจริง
- Rotation: หมุนท้องฟ้าทั้งหมด
- Use Scene Fog Color: ใช้สี Fog จาก Lighting; ปิดเพื่อเลือก Manual Fog Color เอง
- Horizon Fade To Fog / Horizon Fade Height: ทำให้ขอบฟ้ากลืนกับสี Fog
- Whole Sky Fade To Fog: 0 แสดงท้องฟ้าปกติ, 1 ให้ท้องฟ้าทั้งหมดรวมเมฆและดวงอาทิตย์เป็นสี Fog ถ้าต้องการดำสนิทให้ตั้ง Fog สีดำ

Fog ของพื้นยังควบคุมผ่าน Lighting > Environment > Other Settings > Fog ตามเดิม ส่วน Skybox ไม่มีระยะทางจริง จึงใช้การกลืนสีที่ขอบฟ้าและตัวปรับ Whole Sky Fade To Fog แทน

หาก Scene เปิดอยู่และมีงานที่ยังไม่บันทึก ให้ใช้เมนู Apply ด้านบนเพื่อใช้กับงานปัจจุบัน แทนการปิดเปิด Scene จากไฟล์บนดิสก์

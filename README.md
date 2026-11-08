# Arena Strike (Godot 4)

เกมยิงทีม 1v1 / 2v2 / 4v4 สไตล์รอบ (first to 5 rounds) มีล็อบบี้ 3D, ห้อง, จับคู่เร็ว, ล็อกอิน/สมัครด้วยชื่อ+รหัส, 3 แมพ, ปืน 4 กระบอก, ระเบิดมือ

## วิธีรัน
1. เปิด Godot → Import → เลือก `project.godot`
2. **เปิด Server** (เครื่องเดียวสำหรับทุกคน):
   `/Applications/Godot.app/Contents/MacOS/Godot --headless --path /path/to/arena_strike -- --server`
   (macOS ถ้าไฟล์ Godot ชื่ออื่น ให้ปรับ path) พอร์ต UDP 24565 — ถ้าเล่นผ่านเน็ตต้องเปิดพอร์ตนี้
3. เปิดเกม (F5) หลายหน้าต่าง → ใส่ IP server → สมัครบัญชี → Quick Match หรือสร้างห้อง / ใส่รหัสห้อง
   (ทดสอบคนเดียว: Debug > Customize Run Instances เปิด 2 instance)

## ปุ่ม
WASD เดิน, Space กระโดด, Shift เดินช้า, คลิกซ้ายยิง, คลิกขวา (Sniper) ซูม, 1-4 เปลี่ยนปืน, R รีโหลด, G ระเบิด, Tab คะแนน, Esc ปล่อยเมาส์, L ออกจากแมตช์

## ข้อจำกัด / สิ่งที่ต้องทำต่อ
- **Gmail Login**: ยังเป็นปุ่มเปล่า ต้องมี Google OAuth Client ID + ให้ server ตรวจ id_token (เช่นผ่าน HTTPRequest ไป tokeninfo) แล้วค่อยสร้าง session
- รหัสผ่านเก็บเป็น salted SHA-256 บน server (`user://accounts.json`) แต่ส่งผ่าน ENet แบบไม่เข้ารหัส → ก่อนปล่อยจริงควรใช้ DTLS/TLS
- ระบบตัดสินการยิง (hit) เชื่อใจ client — ยังไม่กัน cheat
- ไม่มีเสียง/โมเดลตัวละคร (ใช้ capsule), ไม่มี bot

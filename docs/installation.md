# การติดตั้ง (Installation)

## ผู้ใช้ทั่วไป (exe แบบพกพา)

1. ดาวน์โหลด `NoMoreThaiNums.exe` จากหน้า release (หรือ build เอง — ดู
   [building.md](building.md))
2. คัดลอกไปวางในโฟลเดอร์ที่ต้องการให้อยู่ถาวร เช่น `C:\Tools\NoMoreThaiNums\`
   แล้วเก็บไว้ที่นั่น: ถ้าคุณเปิดใช้ *Start with Windows*
   ค่าใน registry จะชี้มาที่พาธนี้ตรง ๆ หากย้าย exe ทีหลัง ให้ปิดตัวเลือกแล้ว
   เปิดใหม่อีกครั้ง
3. ดับเบิลคลิกรัน ไอคอนจะปรากฏในถาด (อาจอยู่ใต้ปุ่ม `^` ไอคอนที่ถูกซ่อน
   ใกล้นาฬิกา) แอปเริ่มแบบซ่อนอยู่ในถาด
4. ไม่บังคับ: เปิด **Start with Windows** จากเมนูถาด

ไม่ต้องใช้ตัวติดตั้ง ไม่ต้องใช้สิทธิ์ Administrator แอปเขียนไฟล์เฉพาะ
ค่าตั้งค่า (และ log กรณีเกิดข้อผิดพลาด) ใต้ `%LocalAppData%\NoMoreThaiNums\`

## ถอนการติดตั้ง (Uninstall)

1. เมนูถาด → **Exit**
2. ลบไฟล์ exe (และโฟลเดอร์ที่วางไว้)
3. ไม่บังคับ: ลบ `%LocalAppData%\NoMoreThaiNums\` เพื่อลบค่าตั้งค่าและ log
   ถ้าเคยเปิด *Start with Windows* ไว้ ให้ปิดตัวเลือกก่อน (หรือลบ exe แล้ว
   รัน/exit แอปหนึ่งครั้ง) จะเป็นการลบค่าใน registry ด้วย ค่านั้นอยู่ที่
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` ชื่อ `NoMoreThaiNums`

## ความต้องการระบบ (Requirements)

- Windows 10 หรือ Windows 11, x64
- ต้องมีเลย์เอาต์คีย์บอร์ดไทย (Thai Kedmanee หรือ Thai Pattachote)
  ติดตั้งอยู่ การแปลงจึงมีอะไรให้แปลง แต่แอปรันได้ปกติแม้ไม่มี

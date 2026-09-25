# สถาปัตยกรรม (Architecture)

## ภาพรวม

No more Thai Nums เป็นแอป WinForms บน .NET 8 (Windows x64) ที่ทำงานใน System
Tray โดยดักจับอินพุตคีย์บอร์ดทั้งระบบด้วย low-level keyboard hook แปลงเลขไทย
เป็นเลขอารบิกโดยกลืนคีย์สโตรกเดิมแล้วฉีดเลขเข้าไปแทน และเปิดให้ตั้งค่า
enable/disable กับ auto-start ผ่านเมนูถาดและหน้าต่างตั้งค่าขนาดเล็ก

```text
┌────────────────────────── UI (UI thread) ──────────────────────────┐
│  TrayManager (NotifyIcon + menu)      MainForm (settings window)   │
│            └──────────── ApplicationManager (coordinator) ──────┘  │
├───────────────────── Keyboard (hook thread) ───────────────────────┤
│  KeyboardHook → KeyboardProcessor → KeyInjector                    │
│        └── NativeInterop (P/Invoke)                                │
├──────────────────────── Shared services ───────────────────────────┤
│  ThaiDigitConverter   SettingsManager/AppSettings                  │
│  StartupManager (HKCU Run)   SingleInstanceManager (mutex)         │
│  ErrorLog (privacy-safe, error-only)                               │
└────────────────────────────────────────────────────────────────────┘
```

## โมเดลเธรด (Threading model)

| เธรด | หน้าที่ |
|---|---|
| UI thread (`Application.Run`) | ไอคอนถาด, เมนูคลิกขวา, หน้าต่างตั้งค่า, MessageBox |
| Hook thread | ติดตั้ง `WH_KEYBOARD_LL`, ปั๊ม message, ทำการตัดสินใจรายปุ่ม + การฉีด ไม่แตะ UI เด็ดขาด |
| Listener thread | รอ event "show-window" จากการเปิดตัวซ้ำ; ส่งงานไป UI thread ผ่าน `BeginInvoke` |

Callback ของ hook คือจุดที่ไวต่อเวลา (ระบบจะถอด hook ที่บล็อกนานออกเอง)
จึงไม่มีการจัดสรรหน่วยความจำบน hot path, ไม่มี I/O, ไม่มี logging, ไม่มีการเรียก UI
ทั้งการตัดสินใจใช้เพียงการเปรียบเทียบไม่กี่ครั้ง บวก `ToUnicodeEx` อย่างมากหนึ่งครั้ง

## ขั้นตอนต่อหนึ่งคีย์สโตรก (Per-keystroke flow)

1. `KeyboardHook.HookCallback` รับ event ดิบ
2. Event ที่ถูกฉีด (`LLKHF_INJECTED`) ผ่านทันที — ทำให้ recursion เป็นไปไม่ได้
3. สำหรับ key-down ปกติเท่านั้น hook จะหาว่าเลย์เอาต์**ที่ใช้งานอยู่**ผลิตตัวอักษร
   อะไร: อ่านเลย์เอาต์ของเธรดเจ้าของหน้าต่าง foreground แล้วเรียก `ToUnicodeEx`
   ด้วยสถานะ Shift ที่สังเคราะห์ขึ้น
4. `KeyboardProcessor.Process` ตัดสินใจ:
   - ยังไม่เปิดใช้, เป็น key-up, Ctrl/Alt/Win ถูกกดอยู่, หรือตัวอักษรที่ได้
     ไม่ใช่เลขไทย (`U+0E50`–`U+0E59`) → **ผ่านไป (pass through)**
   - ไม่เช่นนั้น → **แทนที่**: กลืน event และฉีดเลขอารบิก
5. `KeyInjector.SendUnicodeChar` ส่งเลขเป็น `KEYEVENTF_UNICODE` down+up
   พร้อม marker ใน `dwExtraInfo` แล้ว hook คืนค่า `1` (กลืน)

### ทำไมต้องตรวจจับแบบอิงเลย์เอาต์

บน Thai Kedmanee เลขไทยอยู่บน*ชั้น Shift* (๑ = Shift+2, ๒ = Shift+3,
๓ = Shift+4, ๔ = Shift+5, ๕ = Shift+8, ๖ = Shift+9, ๗ = Shift+0,
๘ = Shift+`-`, ๙ = Shift+`=`, ๐ = Shift+Q) ส่วนแถวเลขแบบไม่กด Shift จะได้
พยัญชนะ/เครื่องหมายไทย (`จ`, `/`, `-`, `ภ`, …) ใน Thai Pattachote เลขไทยไม่
ต้องกด Shift แต่อยู่บนปุ่มคนละปุ่ม (๑ = `-`, ๖ = `=`, ๐ = `0`) ตาราง
"แถวเลข" แบบคงที่จะผิดกับเลย์เอาต์ใดเลย์เอาต์หนึ่งและทำลายการพิมพ์ไทยปกติ
การถามเลย์เอาต์ที่ใช้งานอยู่ (`ToUnicodeEx`) คือสิ่งที่ทำให้ยูทิลิตี้ถูกต้อง
ทั้งสองเลย์เอาต์ ไม่สะดุดกับการสลับเลย์เอาต์ และปลอดภัย: จะมีการแปลงเฉพาะ
ตัวอักษร*ที่เป็นเลขไทยจริง ๆ* เท่านั้น

### ทำไมใช้การฉีดแบบ Unicode

เลขทดแทนถูกส่งเป็นอักขระ Unicode แทน scancode เพื่อให้ลงเป็น `0`–`9`
ไม่ว่าขณะนั้นเลย์เอาต์ใดจะทำงานอยู่ และไม่มี side effect จาก modifier

## สถานะและการตั้งค่า (State and settings)

`ApplicationManager` เป็นเจ้าของ `AppSettings` ตัวจริง และ push ไปทุกพื้นผิว
(ธงของ processor, เครื่องหมายถูกในเมนูถาด, tooltip, คอนโทรลในหน้าต่าง)
ทุกครั้งที่เปลี่ยน แล้วบันทึกด้วย `SettingsManager` (เขียนแบบ atomic:
ไฟล์ชั่วคราว + replace) ความล้มเหลวในการโหลดทุกชนิด fallback ไปค่าเริ่มต้น

## เส้นทางการปิดโปรแกรม (Shutdown path)

ถาด → Exit → `Application.Exit` → `Application.Run` คืนค่า →
`KeyboardHook.Dispose` (WM_QUIT → join เธรด → unhook) → ไอคอนถาดถูก dispose →
บันทึกค่าตั้งค่า ไม่มีเธรดพื้นหลังหรือ hook หลงเหลืออยู่หลังโปรเซสจบ

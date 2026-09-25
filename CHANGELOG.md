# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-09-25

### Changed

- โปรแกรมเปลี่ยนชื่อเป็น **No more Thai Nums**: ไฟล์โปรแกรมเป็น
  `NoMoreThaiNums.exe`, namespace/identifier ใหม่เป็น `NoMoreThaiNums`
  และค่าตั้งค่าย้ายไป `%LocalAppData%\NoMoreThaiNums\`
- ไอคอนใหม่แบบทันสมัย: จัตุรัสมุมโค้งพร้อมเกรเดียนต์ indigo→cyan,
  เลข 9 สีขาวขนาดใหญ่ และลูกศรเขียวระบุทิศทางการแปลง (๙ → 9)

### Fixed

- แก้บั๊ก `SendInput` P/Invoke (return type ผิด) ที่ทำให้การฉีดเลขล้มเหลว
  ทุกครั้งและคีย์ที่พิมพ์หายไปทั้งหมด — เมื่อการฉีดล้มเหลว hook จะปล่อย
  เลขไทยเดิมผ่านไปแทนการกลืนทิ้ง

### Added

- Migration อัตโนมัติ: ค่าตั้งค่าจากโฟลเดอร์เดิม `%LocalAppData%\ThaiNumberFix`
  และ registry Run key ชื่อเดิม `ThaiNumberFix` จะถูกย้าย/เคลียร์ให้เอง

## [0.1.0] - 2026-09-23

รุ่นต้นแบบ / Alpha

### Added

- Global low-level keyboard hook ทำงานบนเธรด message-pump เฉพาะ
- การตรวจจับเลขไทยแบบอิงเลย์เอาต์ที่ใช้งานอยู่ (รองรับ Thai Kedmanee และ
  Thai Pattachote, ตามการสลับภาษา)
- การแปลงเลขไทย `๐-๙` (U+0E50-U+0E59) เป็นเลขอารบิก `0-9`
  (U+0030-U+0039) ด้วยการฉีดอักขระจริง (`SendInput`)
- เปิด/ปิดการทำงานได้ทันทีจาก System Tray โดยไม่ต้องรีสตาร์ท
- ไอคอน System Tray พร้อมเมนู Enable / Start with Windows / Settings /
  About / Exit และ tooltip แสดงสถานะ
- หน้าต่างตั้งค่าแบบซ่อนไปที่ถาด (ปิดด้วย `X` ไม่ทำให้แอปจบ)
- รองรับ Start-with-Windows ผ่านค่า `HKCU` Run key ต่อผู้ใช้
- บันทึกค่าตั้งค่าเป็น JSON พร้อม fallback ปลอดภัยเมื่อไฟล์หาย/เสียหาย
- บังคับ single-instance พร้อมส่งสัญญาณดึงหน้าต่างตั้งค่าขึ้นมา
- Error logging เท่านั้น (ไม่บันทึกเนื้อหาคีย์สโตรก ตามแบบ)
- เทสต์อัตโนมัติ: การแปลงเลข, ตรรกะตัดสินใจของคีย์บอร์ด, การตั้งค่า,
  พฤติกรรม single-instance

[0.1.1]: https://example.com/releases/tag/v0.1.1
[0.1.0]: https://example.com/releases/tag/v0.1.0

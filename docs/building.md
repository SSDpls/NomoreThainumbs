# การ build (Building)

## สิ่งที่ต้องมี (Prerequisites)

- Windows 10 หรือ 11, x64
- .NET SDK 8.0 ขึ้นไป (ตรวจด้วย `dotnet --list-sdks`)

## Build

```bash
dotnet build NoMoreThaiNums.slnx -c Release
```

ผลลัพธ์: `src/NoMoreThaiNums/bin/Release/net8.0-windows/win-x64/`

## รันเทสต์

```bash
dotnet test NoMoreThaiNums.slnx
```

## Publish เป็น exe แบบพกพาไฟล์เดียว

```bash
dotnet publish src/NoMoreThaiNums -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o publish
```

ได้ผลลัพธ์: `publish/NoMoreThaiNums.exe` — self-contained (เครื่องปลายทางไม่ต้อง
ติดตั้ง .NET runtime), ไฟล์เดียว, x64 ไฟล์ `NoMoreThaiNums.pdb` ข้าง ๆ คือ
debug information ซึ่งไม่จำเป็นต้องใช้ตอนรัน

## สร้างไอคอนแอป/ถาดใหม่

ไฟล์ `src/NoMoreThaiNums/app.ico` สร้างจาก `scripts/make-icon.ps1`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/make-icon.ps1
```

## ตรวจสอบเลย์เอาต์คีย์บอร์ดที่ติดตั้ง (เครื่องมือสำหรับนักพัฒนา)

`scripts/probe-layouts.ps1` จะพิมพ์ว่าแต่ละปุ่มผลิตตัวอักษรอะไร (กดและไม่กด
Shift) สำหรับเลย์เอาต์ไทยทุกตัวที่ติดตั้งไว้ โดยใช้เทคนิค `ToUnicodeEx` แบบเดียว
กับที่ hook ใช้ มีประโยชน์ตอนตรวจสอบพฤติกรรมเลย์เอาต์หรือเพิ่มการรองรับ
เลย์เอาต์อื่น:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/probe-layouts.ps1
```

## รูปแบบ solution

โซลูชันใช้ฟอร์แมต `.slnx` แบบใหม่ Visual Studio 2022 17.13+,
Rider 2024.3+ และ .NET CLI รองรับโดยตรง

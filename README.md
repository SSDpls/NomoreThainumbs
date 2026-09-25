# No more Thai nums

**No more Thai nums** is a lightweight Windows utility that automatically converts Thai numerals into Western digits while typing.

```text
๐๑๒๓๔๕๖๗๘๙
↓
0123456789
```

It is designed for users who want to keep using a Thai keyboard normally without accidentally typing Thai numerals.

## Features

* Converts Thai numerals `๐-๙` to `0-9`
* Works system-wide on Windows
* Enable or disable conversion from the System Tray
* Optional Start with Windows
* Hide to System Tray
* Remembers settings
* Lightweight and offline
* No telemetry
* No keystroke logging
* No account or cloud services

## Example

```text
Before:
จำนวน ๑๒๓ ชิ้น

After:
จำนวน 123 ชิ้น
```

Thai letters, vowels, tone marks, and other characters are not modified.

## Supported Platform

* Windows 10
* Windows 11
* x64

## Installation

Download the latest version from **GitHub Releases**, extract the ZIP file, and run:

```text
NoMoreThaiNums.exe
```

## Usage

The application runs from the Windows System Tray.

```text
No more Thai nums
──────────────────
✓ Enable
□ Start with Windows
Settings
Exit
```

Closing the settings window hides the application to the tray. Use **Exit** from the tray menu to fully close it.

## Privacy

No more Thai nums processes keyboard input locally.

It does not:

* Record or store typed text
* Store keystrokes
* Send keyboard data over the internet
* Use telemetry or analytics

## Known Limitation

Conversion may not work inside applications running with elevated Administrator privileges when No more Thai nums is running normally.

## Build from Source

Clone the repository and open:

```text
NoMoreThaiNums.sln
```

Recommended environment:

* Visual Studio 2022+
* .NET SDK required by the project

## License

Released under the **MIT License**.

Bug reports and contributions are welcome through GitHub Issues and Pull Requests.

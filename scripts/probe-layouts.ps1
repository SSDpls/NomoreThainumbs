# Probes installed Thai keyboard layouts on this machine and prints what each
# key produces with and without Shift, using ToUnicodeEx.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class LayoutProbe
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint uiFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool UnloadKeyboardLayout(IntPtr hkl);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);
}
'@

$virtualKeys = @(0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,
                 0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,0xC0,0xDB,0xDC,0xDD,0xDE,0xDF,0xE2,0x51,0x57,0x45,0x52,0x54)

$klids = @('0000041E','0001041E','0002041E','0003041E')
Write-Output ("Current thread HKL: 0x{0:X}" -f [LayoutProbe]::GetKeyboardLayout(0))

foreach ($klid in $klids) {
    $hkl = [LayoutProbe]::LoadKeyboardLayout($klid, 0)
    if ($hkl -eq [IntPtr]::Zero) {
        Write-Output "KLID $klid : NOT INSTALLED"
        continue
    }
    Write-Output "KLID $klid :"
    foreach ($vk in $virtualKeys) {
        foreach ($shift in 0, 1) {
            $state = New-Object byte[] 256
            if ($shift -eq 1) { $state[0x10] = 0x80; $state[0xA0] = 0x80 }
            $sb = New-Object System.Text.StringBuilder 8
            $n = [LayoutProbe]::ToUnicodeEx([uint32]$vk, 0, $state, $sb, 8, 0, $hkl)
            if ($n -gt 0) {
                $ch = $sb.ToString()[0]
                $code = [int][char]$ch
                Write-Output ("  vk=0x{0:X2} shift={1} -> U+{2:X4} '{3}'" -f $vk, $shift, $code, $ch)
            }
        }
    }
    [void][LayoutProbe]::UnloadKeyboardLayout($hkl)
}

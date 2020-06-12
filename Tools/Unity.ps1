Function Set-ScreenResolution { 
 
    <# 
        .Synopsis 
            Sets the Screen Resolution of the primary monitor 
        .Description 
            Uses Pinvoke and ChangeDisplaySettings Win32API to make the change 
        .Example 
            Set-ScreenResolution -Width 1024 -Height 768         
        #> 
    param ( 
        [Parameter(Mandatory = $true, 
            Position = 0)] 
        [int] 
        $Width, 
     
        [Parameter(Mandatory = $true, 
            Position = 1)] 
        [int] 
        $Height 
    ) 
     
    $pinvokeCode = @" 
     
    using System; 
    using System.Runtime.InteropServices; 
     
    namespace Resolution 
    { 
     
        [StructLayout(LayoutKind.Sequential)] 
        public struct DEVMODE1 
        { 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] 
            public string dmDeviceName; 
            public short dmSpecVersion; 
            public short dmDriverVersion; 
            public short dmSize; 
            public short dmDriverExtra; 
            public int dmFields; 
     
            public short dmOrientation; 
            public short dmPaperSize; 
            public short dmPaperLength; 
            public short dmPaperWidth; 
     
            public short dmScale; 
            public short dmCopies; 
            public short dmDefaultSource; 
            public short dmPrintQuality; 
            public short dmColor; 
            public short dmDuplex; 
            public short dmYResolution; 
            public short dmTTOption; 
            public short dmCollate; 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] 
            public string dmFormName; 
            public short dmLogPixels; 
            public short dmBitsPerPel; 
            public int dmPelsWidth; 
            public int dmPelsHeight; 
     
            public int dmDisplayFlags; 
            public int dmDisplayFrequency; 
     
            public int dmICMMethod; 
            public int dmICMIntent; 
            public int dmMediaType; 
            public int dmDitherType; 
            public int dmReserved1; 
            public int dmReserved2; 
     
            public int dmPanningWidth; 
            public int dmPanningHeight; 
        }; 
     
     
     
        class User_32 
        { 
            [DllImport("user32.dll")] 
            public static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE1 devMode); 
            [DllImport("user32.dll")] 
            public static extern int ChangeDisplaySettings(ref DEVMODE1 devMode, int flags); 
     
            public const int ENUM_CURRENT_SETTINGS = -1; 
            public const int CDS_UPDATEREGISTRY = 0x01; 
            public const int CDS_TEST = 0x02; 
            public const int DISP_CHANGE_SUCCESSFUL = 0; 
            public const int DISP_CHANGE_RESTART = 1; 
            public const int DISP_CHANGE_FAILED = -1; 
        } 
     
     
     
        public class PrmaryScreenResolution 
        { 
            static public string ChangeResolution(int width, int height) 
            { 
     
                DEVMODE1 dm = GetDevMode1(); 
     
                if (0 != User_32.EnumDisplaySettings(null, User_32.ENUM_CURRENT_SETTINGS, ref dm)) 
                { 
     
                    dm.dmPelsWidth = width; 
                    dm.dmPelsHeight = height; 
     
                    int iRet = User_32.ChangeDisplaySettings(ref dm, User_32.CDS_TEST); 
     
                    if (iRet == User_32.DISP_CHANGE_FAILED) 
                    { 
                        return "Unable To Process Your Request. Sorry For This Inconvenience."; 
                    } 
                    else 
                    { 
                        iRet = User_32.ChangeDisplaySettings(ref dm, User_32.CDS_UPDATEREGISTRY); 
                        switch (iRet) 
                        { 
                            case User_32.DISP_CHANGE_SUCCESSFUL: 
                                { 
                                    return "Success"; 
                                } 
                            case User_32.DISP_CHANGE_RESTART: 
                                { 
                                    return "You Need To Reboot For The Change To Happen.\n If You Feel Any Problem After Rebooting Your Machine\nThen Try To Change Resolution In Safe Mode."; 
                                } 
                            default: 
                                { 
                                    return "Failed To Change The Resolution"; 
                                } 
                        } 
     
                    } 
     
     
                } 
                else 
                { 
                    return "Failed To Change The Resolution."; 
                } 
            } 
     
            private static DEVMODE1 GetDevMode1() 
            { 
                DEVMODE1 dm = new DEVMODE1(); 
                dm.dmDeviceName = new String(new char[32]); 
                dm.dmFormName = new String(new char[32]); 
                dm.dmSize = (short)Marshal.SizeOf(dm); 
                return dm; 
            } 
        } 
    } 
     
"@ 
     
    Add-Type $pinvokeCode -ErrorAction SilentlyContinue 
    [Resolution.PrmaryScreenResolution]::ChangeResolution($width, $height) 
} 

Function Cmd-RunInteractive {
    param(
  
        [string]$Command,
        [switch]$Admin = $false,
        [switch]$PowerShellPrompt = $false,
        [switch]$CommandPrompt = $false
  
    )

    $RunCMD = $Command
    $level = 'LIMITED'
	
    if ($CommandPrompt -eq $true) {
        $RunCMD = "C:\Windows\System32\cmd.exe /c $command"
    }
  
    if ($PowerShellPrompt -eq $true) {
        $RunCMD = "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -command $Command"
    }
  
    if ($Admin -eq $true) {
        $level = 'HIGHEST'
    }
  
    Write-Host -Object "Launching $RunCMD with $level rights"
    $null = schtasks.exe /Delete /TN 'GUI Command' /F
    $null = schtasks.exe /Create /SC ONCE /TR "$RunCMD" /IT /ST 00:00 /SD 01/01/9999 /TN 'GUI Command' /RL $level /RU bokken
    $null = schtasks.exe /Run /TN 'GUI Command'
    $null = schtasks.exe /Delete /TN "GUI Command" /F 
}
# Test of Csharp-VVVF
Generating vvvf sound on PC using C#

# Usage
C# console app on VisualStudio.

# Output
## Audio
This application will output a sound wave form in wav extension.<br>
Default is 192 kHz sampling data.<br>

## Video
This application will output a wave form view video in avi extention.<br>
It is required to use OpenCV dependencies on visualstudio and also openH264 to generate video<br>

### openH264 file
Its file name will like `openh264-1.8.0-win64.dll.bz2` <br>
You can download openH264 file from here https://github.com/cisco/openh264/releases <br>
After you've download it, you just extract bz2 file and you will find `openh264-1.8.0-win64.dll`.<br>
This file should be placed same directory of console app exe file.

# Parent Project
This program was ported from : <br>
https://github.com/JOTAN-0655/RPi-Zero-VVVF

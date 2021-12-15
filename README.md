# C# VVVF Simulator
Simulating VVVF sound on PC.

# Description
This program is for C# console app on VisualStudio.<br>

# Dependencies
## Generating Video
・OpenCV - You can get from NuGet<br>
・System.Drawing.Common - You can get from NuGet<br>
・OpenH264 - You can get from Internet<br>

### About openH264
You can download from this link.<br>
https://github.com/cisco/openh264/releases<br>
The version which this application uses is `1.8.0`<br>
File name is `openh264-1.8.0-win64.dll.bz2`<br>
After you've download it, extract it.And you will have `openh264-1.8.0-win64.dll`. Put it on same directory as execute file.<br>

## Generating Audio
・There is no dependencies.

## Realtime Audio Generation
・NAudio - You can get from NuGet.

# Functions
## VVVF Audio Generation
This application will export simulated vvvf sound in `.wav` extension.<br>
The sampling frequency will be 192kHz.<br>

## Waveform Video Generation
This application will export video in `.avi` extension.

## Control stat Video Generation
This application can export video of control stat.<br>
Output extension is `.avi`<br>

## Realtime Audio Generation
You can play around with it<br>
Key Binds<br>
```
W - Big Change
S - Middle Change
X - Small Change
B - Brake ON/OFF
M - Mascon ON/OFF
```

# Parent Project
This program was ported from RPi-Zero-VVVF
https://github.com/JOTAN-0655/RPi-Zero-VVVF

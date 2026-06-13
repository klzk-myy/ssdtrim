@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo ERROR: C# compiler not found at %CSC%
    exit /b 1
)

"%CSC%" /target:exe /out:SsdTrim.exe /optimize+ /platform:x64 Program.cs

if %ERRORLEVEL% EQU 0 (
    echo Build successful: SsdTrim.exe
) else (
    echo Build failed
    exit /b 1
)

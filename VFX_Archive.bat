@echo off
hg id -i > id.txt
set /p rev=<id.txt
del id.txt
set filename=VFX_Extra_%rev%
set folderName=%temp%\%filename%
hg archive %folderName% -X ".hg*"
robocopy Assets\Editor\GraphView\ %folderName%\Assets\Editor\GraphView\ /E /xd .git
del %filename%.7z
"C:/Program Files/7-Zip/7z.exe" a -t7z -mx9 %filename%.7z %folderName%/*
rmdir %folderName% /s /q
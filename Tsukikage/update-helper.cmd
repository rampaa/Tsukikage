@ECHO OFF
@SETLOCAL enableextensions
@CD /D "%~dp0"
TASKKILL /F /PID %1
DEL /Q /F ".\tmp\Tsukikage.ini"
ROBOCOPY ".\tmp\ " . /E /Z /R:30 /W:1 /MOVE
RMDIR /Q /S  "%CD%\tmp\"
START "" "%CD%\Tsukikage.exe"
EXIT

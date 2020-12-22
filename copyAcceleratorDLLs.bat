@echo off
REM Batch file to copy SDK Sample DLLs including accelerator DLLs in C#

IF "%GO_SDK_4%" == "" GOTO NOPATH
   :YESPATH
   @ECHO The GO_SDK environment variable was detected.

   xcopy "%GO_SDK_4%\bin\win32\*.dll" "%CD%\bin\x86\Release\"
   xcopy "%GO_SDK_4%\bin\win32d\*.dll" "%CD%\bin\x86\Debug\"
   xcopy "%GO_SDK_4%\bin\win64\*.dll" "%CD%\bin\x64\Release\"
   xcopy "%GO_SDK_4%\bin\win64d\*.dll" "%CD%\bin\x64\Debug\"

   GOTO END
   :NOPATH
   @ECHO The GO_SDK_4 environment variable was NOT detected.Please set the ENV Variable GO_SDK_4 to point to the Go SDK Directory.
   GOTO END
   :END

pause
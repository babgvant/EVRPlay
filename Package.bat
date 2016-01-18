@ECHO OFF
CLS

SET ZIPNAME=EVRPlaySource.zip
SET ZIPNAME1=EVRPlay.zip
GOTO WORK

:WORK
"%ProgramFiles%\WinZip\wzzip" -a -P -r -x*.snk -x*.bat -x*.dll -x*.ax -x*.scc -x*.vbs -x*.exe -x*.fwjob -x*.pdb -x*.bat -x*.zip -x*.xml -x*.dcc -x*.dpc -x*.bin -x*.msi -x*.ini -x*.h -x*.c -x*.dictionary -x*.htm -x*.css -x*.log -x*.user -xfilestoremove.txt -xchanges*.txt -xreadme.txt -xSASDK_Documentation.txt -xSA_Documentation.txt -x*.url -x"New Text Document.txt" -xMode.status -xblank.txt -x*.resources -x*.cache -xManualInstall\* %ZIPNAME% *.*

"%ProgramFiles%\WinZip\wzzip" -a -x*.scc %ZIPNAME1% -x*.log -x*.pdb -x*.vshost.* bin\Release\*.* license.txt readme.txt



:CACLS.EXE %ZIPNAME% /E /G USERS:F
:CACLS.EXE %ZIPNAME1% /E /G USERS:F

REM CD\Tools
rem PAUSE
GOTO BYE

:NOPARM


:BYE

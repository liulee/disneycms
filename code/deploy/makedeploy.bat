@echo off
set Config=Debug
set zip=%cd%\7z64.exe
set src=src_%DATE:~0,4%%DATE:~5,2%%DATE:~8,2%.zip

echo Deploying CMS of '%Config%'...

IF EXIST cms.zip del cms.zip
IF EXIST %src% del %src%

IF EXIST cms (
 del cms /s /q > nul
 rmdir cms /s /q > nul
)
mkdir cms
mkdir cms\server
mkdir cms\client

IF EXIST src (
 del src /s /q > nul
 rmdir src /s /q > nul
)
mkdir src
mkdir src\server
mkdir src\server\res
mkdir src\client


echo Server...
copy ..\server\bin\%Config%\DisneyCMS.exe cms\server\ > nul
copy ..\server\bin\%Config%\cms.xml cms\server\cms_sample.xml > nul
copy ..\server\bin\%Config%\*.dll cms\server\ > nul
copy ..\server\bin\%Config%\log*.config cms\server\ > nul

echo Client...
copy ..\client\bin\%Config%\client.exe cms\client\ > nul
copy ..\client\bin\%Config%\*.dll cms\client\ > nul
copy ..\client\bin\%Config%\log*.config cms\client\ > nul

echo Source...
xcopy  /s /y /exclude:nosrc.txt  ..\server src\server
xcopy  /s /y /exclude:nosrc.txt  ..\client src\client

echo Zipping...
cd cms
%zip% a ..\cms.zip * > nul
cd ..

cd src
%zip% a ..\%src% * > nul
cd ..

echo Done!
pause

@echo off
IF %1.==. GOTO No1
@echo on
docker build --rm -t dibkregistrylinux.azurecr.io/pdfgenerator:%1 .
docker login dibkregistrylinux.azurecr.io -u %PDFGenerator-Azure-User% -p %PDFGenerator-Azure-Password%
docker push dibkregistrylinux.azurecr.io/pdfgenerator:%1
GOTO EndScript

:No1
  ECHO ERROR 
  ECHO No param for version 
  ECHO Usage: %0 v^<yyyymmdd-n^>

:EndScript
pause
IF %1.==. GOTO No1
docker build --rm -t dibkregistrylinux.azurecr.io/pdfgenerator:%1 .
docker login dibkregistrylinux.azurecr.io -u %PDFGenerator-Azure-User% -p %PDFGenerator-Azure-Password%
docker push dibkregistrylinux.azurecr.io/pdfgenerator:%1
GOTO EndScript

:No1
  ECHO No param 1

:EndScript
pause
## Getting Linux base image for .NET 5.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build

WORKDIR /
COPY *.sln .
COPY PDFGenerator/. ./PDFGenerator/

RUN dotnet build -c Release -o /app_output
RUN dotnet publish -c Release -o /app_output

## Getting Linux base image for .NET 5.0 runtime 
FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS final

EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app_output .

## Updating packages
RUN echo "deb http://ftp.debian.org/debian buster main contrib" >> /etc/apt/sources.list
RUN apt update 

## Installing Chromium to be run in headless mode
RUN apt install -y chromium 

## Installing tool for process management
RUN apt install -y supervisor

## Setting Supervisor base configuration
ADD supervisor.conf /etc/supervisor.conf

## Installing fonts
# Andale Mono, Arial Black, Arial (Bold, Italic, Bold Italic), Comic Sans MS (Bold), Courier New (Bold, Italic, Bold Italic), 
# Georgia (Bold, Italic, Bold Italic), Impact, Times New Roman (Bold, Italic, Bold Italic), Trebuchet (Bold, Italic, Bold Italic), 
# Verdana (Bold, Italic, Bold Italic), Webdings
RUN apt install -y ttf-mscorefonts-installer fonts-crosextra-carlito fonts-crosextra-caladea

# Launching Supervisor as the default command
CMD ["supervisord", "-c", "/etc/supervisor.conf"]

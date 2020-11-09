#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

## Getting Linux base image for .Net Core runtime 
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

## Installing Chromium to be run in headless mode
RUN apt-get update && apt-get install -y chromium

## Installing tool for process management
RUN apt-get install -y supervisor
# Setting supervisor base configuration
ADD supervisor.conf /etc/supervisor.conf

## Installing fonts
# Andale Mono, Arial Black, Arial (Bold, Italic, Bold Italic), Comic Sans MS (Bold), Courier New (Bold, Italic, Bold Italic), 
# Georgia (Bold, Italic, Bold Italic), Impact, Times New Roman (Bold, Italic, Bold Italic), Trebuchet (Bold, Italic, Bold Italic), 
# Verdana (Bold, Italic, Bold Italic), Webdings
#RUN printf "deb http://httpredir.debian.org/debian jessie-backports main non-free\ndeb-src http://httpredir.debian.org/debian jessie-backports main non-free" >> /etc/apt/sources.list
RUN printf "deb http://ftp.us.debian.org/debian jessie main contrib" >> /etc/apt/sources.list
RUN apt-get update && apt-get -y install ttf-mscorefonts-installer
RUN apt-get -y install fonts-crosextra-carlito fonts-crosextra-caladea

## Getting Linux base image for .Net Core SDK
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

## Copy- and build project for .Net Core Rest service
WORKDIR /src
COPY ["PDFGenerator/PDFGenerator.csproj", "PDFGenerator/"]
RUN dotnet restore "PDFGenerator/PDFGenerator.csproj"
COPY . .
WORKDIR "/src/PDFGenerator"
RUN dotnet build "PDFGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PDFGenerator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


# Launching Supervisor as the default command
CMD ["supervisord", "-c", "/etc/supervisor.conf"]

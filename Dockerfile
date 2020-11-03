#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

##cleanup
#RUN add-apt-repository -r ppa:webupd8team/java
#RUN apt-get update && apt-get install -y apache2 && apt-get clean && rm -rf /var/lib/apt/lists/*
#RUN \
#  sed -i 's/# \(.*multiverse$\)/\1/g' /etc/apt/sources.list && \
#apt-get update && \
#apt-get -y upgrade

# supervisor installation &&
# create directory for child images to store configuration in
#RUN apt-get -y install supervisor && \
#  mkdir -p /var/log/supervisor && \
#  mkdir -p /etc/supervisor/conf.d
RUN apt-get update && apt-get install -y chromium


## Installing required tools
RUN apt-get install -y supervisor
# supervisor base configuration
ADD supervisor.conf /etc/supervisor.conf

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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


# default command
CMD ["supervisord", "-c", "/etc/supervisor.conf"]


#ENTRYPOINT ["dotnet", "PDFGenerator.dll"]
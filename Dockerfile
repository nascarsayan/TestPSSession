#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /usr/lib/x86_64-linux-gnu
RUN apt -y update &&\
  apt -y install libssl-dev &&\
  [ ! -f libssl.so.1.0.0 ] && ln -s libssl.so libssl.so.1.0.0 &&\
  [ ! -f libcrypto.so.1.0.0 ] && ln -s libcrypto.so libcrypto.so.1.0.0 &&\
  apt -y install gss-ntlmssp
WORKDIR /app
# EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src/TestPSSession
COPY TestPSSession.csproj TestPSSession.csproj
RUN dotnet restore TestPSSession.csproj
COPY . .
RUN dotnet publish "TestPSSession.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_preventHostingStartup=true
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TestPSSession.dll"]

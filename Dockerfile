#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG REPO=mcr.microsoft.com/dotnet

FROM centos:7 AS base
RUN aspnetcore_version=6.0.6 \
  && curl -fSL --output aspnetcore.tar.gz https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/$aspnetcore_version/aspnetcore-runtime-$aspnetcore_version-linux-x64.tar.gz \
  && aspnetcore_sha512='1a5c0f85820f0eb589700df94de6dbff45fe4089a37f1cd5b1fac33476a2cbd8d5c6f129e55b3716f5a7a2616f1a5a720c52238f21b28a510a3e5c8bcb8c516c' \
  && echo "$aspnetcore_sha512  aspnetcore.tar.gz" | sha512sum -c - \
  && tar -oxzf aspnetcore.tar.gz ./shared/Microsoft.AspNetCore.App \
  && rm -y aspnetcore.tar.gz
WORKDIR /usr/lib/x86_64-linux-gnu
RUN apt -y update &&\
  apt -y install libssl-dev &&\
  [ ! -f libssl.so.1.0.0 ] && ln -s libssl.so libssl.so.1.0.0 &&\
  [ ! -f libcrypto.so.1.0.0 ] && ln -s libcrypto.so libcrypto.so.1.0.0 &&\
  apt -y install gss-ntlmssp
WORKDIR /app
# EXPOSE 80

FROM $REPO/sdk:6.0 AS build
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

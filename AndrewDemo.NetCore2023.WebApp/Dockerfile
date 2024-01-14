#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AndrewDemo.NetCore2023.WebApp/AndrewDemo.NetCore2023.WebApp.csproj", "AndrewDemo.NetCore2023.WebApp/"]
RUN dotnet restore "./AndrewDemo.NetCore2023.WebApp/./AndrewDemo.NetCore2023.WebApp.csproj"
COPY . .
WORKDIR "/src/AndrewDemo.NetCore2023.WebApp"
RUN dotnet build "./AndrewDemo.NetCore2023.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AndrewDemo.NetCore2023.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetCore2023.WebApp.dll"]
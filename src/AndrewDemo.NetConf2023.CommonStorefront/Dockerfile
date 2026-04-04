FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY . .
WORKDIR /src/src/AndrewDemo.NetConf2023.CommonStorefront
RUN dotnet publish AndrewDemo.NetConf2023.CommonStorefront.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetConf2023.CommonStorefront.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY . .
WORKDIR /src/src/AndrewDemo.NetConf2023.PetShop.DatabaseInit
RUN dotnet publish AndrewDemo.NetConf2023.PetShop.DatabaseInit.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetConf2023.PetShop.DatabaseInit.dll"]

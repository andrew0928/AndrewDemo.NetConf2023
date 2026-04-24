FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["src/AndrewDemo.NetConf2023.PetShop.API/AndrewDemo.NetConf2023.PetShop.API.csproj", "src/AndrewDemo.NetConf2023.PetShop.API/"]
COPY ["src/AndrewDemo.NetConf2023.PetShop.Extension/AndrewDemo.NetConf2023.PetShop.Extension.csproj", "src/AndrewDemo.NetConf2023.PetShop.Extension/"]
COPY ["src/AndrewDemo.NetConf2023.Core/AndrewDemo.NetConf2023.Core.csproj", "src/AndrewDemo.NetConf2023.Core/"]
COPY ["src/AndrewDemo.NetConf2023.Abstract/AndrewDemo.NetConf2023.Abstract.csproj", "src/AndrewDemo.NetConf2023.Abstract/"]
RUN dotnet restore "src/AndrewDemo.NetConf2023.PetShop.API/AndrewDemo.NetConf2023.PetShop.API.csproj"
COPY . .
WORKDIR "/src/src/AndrewDemo.NetConf2023.PetShop.API"
RUN dotnet publish "AndrewDemo.NetConf2023.PetShop.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetConf2023.PetShop.API.dll"]

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["src/Directory.Build.props", "src/"]
COPY ["src/AndrewDemo.NetConf2023.PetShop.API/AndrewDemo.NetConf2023.PetShop.API.csproj", "src/AndrewDemo.NetConf2023.PetShop.API/"]
COPY ["src/AndrewDemo.NetConf2023.PetShop.Extension/AndrewDemo.NetConf2023.PetShop.Extension.csproj", "src/AndrewDemo.NetConf2023.PetShop.Extension/"]
COPY ["src/AndrewDemo.NetConf2023.Core/AndrewDemo.NetConf2023.Core.csproj", "src/AndrewDemo.NetConf2023.Core/"]
COPY ["src/AndrewDemo.NetConf2023.Abstract/AndrewDemo.NetConf2023.Abstract.csproj", "src/AndrewDemo.NetConf2023.Abstract/"]
RUN target_arch="${TARGETARCH:-$(uname -m)}"; \
    case "$target_arch" in \
        amd64|x86_64) rid="linux-x64" ;; \
        arm64|aarch64) rid="linux-arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${target_arch}" >&2; exit 1 ;; \
    esac; \
    dotnet restore "src/AndrewDemo.NetConf2023.PetShop.API/AndrewDemo.NetConf2023.PetShop.API.csproj" -r "$rid"
COPY . .
WORKDIR "/src/src/AndrewDemo.NetConf2023.PetShop.API"
RUN target_arch="${TARGETARCH:-$(uname -m)}"; \
    case "$target_arch" in \
        amd64|x86_64) rid="linux-x64" ;; \
        arm64|aarch64) rid="linux-arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${target_arch}" >&2; exit 1 ;; \
    esac; \
    dotnet publish "AndrewDemo.NetConf2023.PetShop.API.csproj" -c Release -r "$rid" --self-contained false /p:UseAppHost=false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetConf2023.PetShop.API.dll"]

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY . .
WORKDIR /src/src/AndrewDemo.NetConf2023.CommonStorefront
RUN target_arch="${TARGETARCH:-$(uname -m)}"; \
    case "$target_arch" in \
        amd64|x86_64) rid="linux-x64" ;; \
        arm64|aarch64) rid="linux-arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${target_arch}" >&2; exit 1 ;; \
    esac; \
    dotnet publish AndrewDemo.NetConf2023.CommonStorefront.csproj -c Release -r "$rid" --self-contained false /p:UseAppHost=false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AndrewDemo.NetConf2023.CommonStorefront.dll"]

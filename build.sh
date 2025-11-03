#!/bin/bash

# Parse command line arguments
PUSH_IMAGES=false

show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Build and optionally push Docker images for AndrewShop.ApiDemo"
    echo ""
    echo "Options:"
    echo "  --push    Push images to registry after building"
    echo "  --help    Show this help message"
    echo ""
    echo "Note: Docker engine must enable containerd image store"
    echo "Edit /etc/docker/daemon.json to add:"
    echo '{"features": {"containerd-snapshotter": true}}'
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --push)
            PUSH_IMAGES=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# build solution
dotnet publish src/AndrewDemo.NetConf2023.sln -c Release /t:PublishContainer -m

# build init container (seed) - multi-arch for Azure Container Apps
if [ "$PUSH_IMAGES" = true ]; then
    # Build and push multi-arch image directly to registry
    docker buildx build --platform linux/amd64,linux/arm64 \
        -t andrew0928.azurecr.io/andrewdemo-shop-seed:develop \
        -t andrew0928.azurecr.io/andrewdemo-shop-seed:develop$(date +%Y%m%d) \
        -t andrew0928.azurecr.io/andrewdemo-shop-seed:$(date +%Y%m%d) \
        --push \
        src/seed/
else
    # Local build only (native arch)
    docker build -t andrewdemo-netconf2023-seed:develop \
        -t andrewdemo-netconf2023-seed:$(date +%Y%m%d) \
        src/seed/
fi

# tag images (only for API, seed is handled above)
docker tag andrewdemo-netconf2023-api:develop         andrew0928.azurecr.io/andrewdemo-shop-api:develop
docker tag andrewdemo-netconf2023-api:develop         andrew0928.azurecr.io/andrewdemo-shop-api:develop$(date +%Y%m%d)
docker tag andrewdemo-netconf2023-api:$(date +%Y%m%d) andrew0928.azurecr.io/andrewdemo-shop-api:$(date +%Y%m%d)

# Seed tags only needed for local development
if [ "$PUSH_IMAGES" = false ]; then
    docker tag andrewdemo-netconf2023-seed:develop         andrew0928.azurecr.io/andrewdemo-shop-seed:develop
    docker tag andrewdemo-netconf2023-seed:develop         andrew0928.azurecr.io/andrewdemo-shop-seed:develop$(date +%Y%m%d)
    docker tag andrewdemo-netconf2023-seed:$(date +%Y%m%d) andrew0928.azurecr.io/andrewdemo-shop-seed:$(date +%Y%m%d)
fi

# push images
if [ "$PUSH_IMAGES" = true ]; then
    docker push andrew0928.azurecr.io/andrewdemo-shop-api:$(date +%Y%m%d)
    # seed image already pushed during buildx build above
fi
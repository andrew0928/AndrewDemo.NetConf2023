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

# tag images
docker tag andrewdemo-netconf2023-api:develop         andrew0928.azurecr.io/andrewdemo-netconf2023-api:develop
docker tag andrewdemo-netconf2023-api:$(date +%Y%m%d) andrew0928.azurecr.io/andrewdemo-netconf2023-api:$(date +%Y%m%d)

# push images
if [ "$PUSH_IMAGES" = true ]; then
    docker push andrew0928.azurecr.io/andrewdemo-netconf2023-api:$(date +%Y%m%d)
fi
#!/usr/bin/env bash
set -euo pipefail

REGISTRY="${REGISTRY:-andrew0928.azurecr.io}"
PLATFORMS="${PLATFORMS:-linux/amd64,linux/arm64}"
DATE_TAG="${DATE_TAG:-$(date +%Y%m%d)}"
BUILD_CONTEXT="${BUILD_CONTEXT:-.}"
PUSH_IMAGES=false

IMAGES=(
    "andrewdemo-shop-api|src/AndrewDemo.NetConf2023.API/Dockerfile"
    "andrewdemo-shop-applebts-seed|src/AndrewDemo.NetConf2023.AppleBTS.DatabaseInit/Dockerfile"
    "andrewdemo-shop-applebts-btsapi|src/AndrewDemo.NetConf2023.AppleBTS.API/Dockerfile"
    "andrewdemo-shop-applebts-storefront|src/AndrewDemo.NetConf2023.AppleBTS.Storefront/Dockerfile"
    "andrewdemo-shop-petshop-seed|src/AndrewDemo.NetConf2023.PetShop.DatabaseInit/Dockerfile"
    "andrewdemo-shop-petshop-reservationapi|src/AndrewDemo.NetConf2023.PetShop.API/Dockerfile"
    "andrewdemo-shop-petshop-storefront|src/AndrewDemo.NetConf2023.PetShop.Storefront/Dockerfile"
)

show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Build and optionally push release Docker images for AndrewShop.ApiDemo."
    echo ""
    echo "Options:"
    echo "  --push    Build multi-arch images and push them to the registry"
    echo "  --help    Show this help message"
    echo ""
    echo "Environment variables:"
    echo "  REGISTRY       Target registry. Default: andrew0928.azurecr.io"
    echo "  PLATFORMS      buildx platforms for --push. Default: linux/amd64,linux/arm64"
    echo "  DATE_TAG       Date tag. Default: current date in yyyyMMdd"
    echo "  BUILD_CONTEXT  Docker build context. Default: ."
    echo ""
    echo "Tags:"
    echo "  develop"
    echo "  develop${DATE_TAG}"
    echo "  ${DATE_TAG}"
    echo ""
    echo "Note:"
    echo "  nginx is intentionally excluded. Production edge routing is handled by Azure Front Door."
    echo "  The standard API image is built once; AppleBTS/PetShop behavior is selected by runtime environment variables."
}

while [[ $# -gt 0 ]]; do
    case "$1" in
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

build_image() {
    local repository="$1"
    local dockerfile="$2"
    local image="${REGISTRY}/${repository}"

    echo ""
    echo "==> Building ${image}"
    echo "    Dockerfile: ${dockerfile}"

    if [ "$PUSH_IMAGES" = true ]; then
        docker buildx build \
            --platform "$PLATFORMS" \
            -f "$dockerfile" \
            -t "${image}:develop" \
            -t "${image}:develop${DATE_TAG}" \
            -t "${image}:${DATE_TAG}" \
            --push \
            "$BUILD_CONTEXT"
    else
        docker build \
            -f "$dockerfile" \
            -t "${repository}:develop" \
            -t "${repository}:${DATE_TAG}" \
            -t "${image}:develop" \
            -t "${image}:develop${DATE_TAG}" \
            -t "${image}:${DATE_TAG}" \
            "$BUILD_CONTEXT"
    fi
}

echo "Registry: ${REGISTRY}"
echo "Date tag: ${DATE_TAG}"

if [ "$PUSH_IMAGES" = true ]; then
    echo "Mode: buildx multi-arch push"
    echo "Platforms: ${PLATFORMS}"
else
    echo "Mode: local build only"
    echo "Platforms: Docker native platform"
fi

for image_spec in "${IMAGES[@]}"; do
    IFS='|' read -r repository dockerfile <<< "$image_spec"
    build_image "$repository" "$dockerfile"
done

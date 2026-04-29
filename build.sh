#!/usr/bin/env bash
set -euo pipefail

REGISTRY="${REGISTRY:-andrew0928.azurecr.io}"
PLATFORMS="${PLATFORMS:-linux/amd64,linux/arm64}"
DATE_TAG="${DATE_TAG:-$(date +%Y%m%d)}"
BUILD_CONTEXT="${BUILD_CONTEXT:-}"
PUSH_IMAGES=false
SELECTED_IMAGES=()

IMAGES=(
    "andrewdemo-shop-common-api|src/AndrewDemo.NetConf2023.API/Dockerfile|."
    "andrewdemo-shop-common-seed|src/seed/Dockerfile|src/seed"
    "andrewdemo-shop-common-storefront|src/AndrewDemo.NetConf2023.CommonStorefront/Dockerfile|."
    "andrewdemo-shop-applebts-seed|src/AndrewDemo.NetConf2023.AppleBTS.DatabaseInit/Dockerfile|."
    "andrewdemo-shop-applebts-btsapi|src/AndrewDemo.NetConf2023.AppleBTS.API/Dockerfile|."
    "andrewdemo-shop-applebts-storefront|src/AndrewDemo.NetConf2023.AppleBTS.Storefront/Dockerfile|."
    "andrewdemo-shop-petshop-seed|src/AndrewDemo.NetConf2023.PetShop.DatabaseInit/Dockerfile|."
    "andrewdemo-shop-petshop-reservationapi|src/AndrewDemo.NetConf2023.PetShop.API/Dockerfile|."
    "andrewdemo-shop-petshop-storefront|src/AndrewDemo.NetConf2023.PetShop.Storefront/Dockerfile|."
)

show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Build and optionally push release Docker images for AndrewShop.ApiDemo."
    echo ""
    echo "Options:"
    echo "  --push          Build multi-arch images and push them to the registry"
    echo "  --image NAME    Build only the named image. Can be repeated."
    echo "                  NAME can be the repository or short name, e.g. common-storefront."
    echo "  --help          Show this help message"
    echo ""
    echo "Environment variables:"
    echo "  REGISTRY       Target registry. Default: andrew0928.azurecr.io"
    echo "  PLATFORMS      buildx platforms for --push. Default: linux/amd64,linux/arm64"
    echo "  DATE_TAG       Date tag. Default: current date in yyyyMMdd"
    echo "  BUILD_CONTEXT  Override Docker build context for all images. Default: image-specific context."
    echo ""
    echo "Tags:"
    echo "  develop"
    echo "  develop${DATE_TAG}"
    echo "  ${DATE_TAG}"
    echo ""
    echo "Note:"
    echo "  nginx is intentionally excluded. Production edge routing is handled by Azure Front Door."
    echo "  The common API image is built once; AppleBTS/PetShop behavior is selected by runtime environment variables."
    echo ""
    echo "Images:"
    for image_spec in "${IMAGES[@]}"; do
        IFS='|' read -r repository dockerfile build_context <<< "$image_spec"
        echo "  ${repository#andrewdemo-shop-} (${repository})"
    done
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --push)
            PUSH_IMAGES=true
            shift
            ;;
        --image)
            if [[ $# -lt 2 || "$2" == --* ]]; then
                echo "Missing value for --image"
                show_help
                exit 1
            fi
            SELECTED_IMAGES+=("$2")
            shift 2
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

image_matches_name() {
    local repository="$1"
    local selected="$2"
    local short_name="${repository#andrewdemo-shop-}"

    [[ "$selected" == "$repository" || "$selected" == "$short_name" ]]
}

image_is_selected() {
    local repository="$1"

    if [[ "${#SELECTED_IMAGES[@]}" -eq 0 ]]; then
        return 0
    fi

    for selected in "${SELECTED_IMAGES[@]}"; do
        if image_matches_name "$repository" "$selected"; then
            return 0
        fi
    done

    return 1
}

validate_selected_images() {
    local selected found repository dockerfile build_context image_spec

    if [[ "${#SELECTED_IMAGES[@]}" -eq 0 ]]; then
        return 0
    fi

    for selected in "${SELECTED_IMAGES[@]}"; do
        found=false
        for image_spec in "${IMAGES[@]}"; do
            IFS='|' read -r repository dockerfile build_context <<< "$image_spec"
            if image_matches_name "$repository" "$selected"; then
                found=true
                break
            fi
        done

        if [[ "$found" == false ]]; then
            echo "Unknown image: $selected"
            echo "Available images:"
            for image_spec in "${IMAGES[@]}"; do
                IFS='|' read -r repository dockerfile build_context <<< "$image_spec"
                echo "  ${repository#andrewdemo-shop-} (${repository})"
            done
            exit 1
        fi
    done
}

build_image() {
    local repository="$1"
    local dockerfile="$2"
    local build_context="$3"
    local image="${REGISTRY}/${repository}"

    echo ""
    echo "==> Building ${image}"
    echo "    Dockerfile: ${dockerfile}"
    echo "    Context: ${build_context}"

    if [ "$PUSH_IMAGES" = true ]; then
        docker buildx build \
            --platform "$PLATFORMS" \
            -f "$dockerfile" \
            -t "${image}:develop" \
            -t "${image}:develop${DATE_TAG}" \
            -t "${image}:${DATE_TAG}" \
            --push \
            "$build_context"
    else
        docker build \
            -f "$dockerfile" \
            -t "${repository}:develop" \
            -t "${repository}:${DATE_TAG}" \
            -t "${image}:develop" \
            -t "${image}:develop${DATE_TAG}" \
            -t "${image}:${DATE_TAG}" \
            "$build_context"
    fi
}

echo "Registry: ${REGISTRY}"
echo "Date tag: ${DATE_TAG}"
validate_selected_images

if [[ "${#SELECTED_IMAGES[@]}" -gt 0 ]]; then
    echo "Selected images: ${SELECTED_IMAGES[*]}"
fi

if [ "$PUSH_IMAGES" = true ]; then
    echo "Mode: buildx multi-arch push"
    echo "Platforms: ${PLATFORMS}"
else
    echo "Mode: local build only"
    echo "Platforms: Docker native platform"
fi

for image_spec in "${IMAGES[@]}"; do
    IFS='|' read -r repository dockerfile image_build_context <<< "$image_spec"
    build_context="${BUILD_CONTEXT:-$image_build_context}"
    if image_is_selected "$repository"; then
        build_image "$repository" "$dockerfile" "$build_context"
    fi
done

#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

RESOURCE_GROUP="${RESOURCE_GROUP:-}"
CONTAINERAPPS_ENV="${CONTAINERAPPS_ENV:-}"
LOCATION="${LOCATION:-}"
ACR_NAME="${ACR_NAME:-andrew0928}"
ACR_RESOURCE_GROUP="${ACR_RESOURCE_GROUP:-}"
SITES="${SITES:-common applebts petshop}"
IMAGE_TAG="${IMAGE_TAG:-develop}"

usage() {
    cat <<'USAGE'
Usage:
  RESOURCE_GROUP=<resource-group> CONTAINERAPPS_ENV=<aca-env> [LOCATION=<region>] [SITES="common applebts petshop"] bash deploy/aca/deploy-site-prod.sh

Required:
  RESOURCE_GROUP       Azure resource group for Container Apps.
  CONTAINERAPPS_ENV    Azure Container Apps environment name.

Optional:
  LOCATION             Required only when the resource group or Container Apps environment must be created.
  ACR_NAME             Azure Container Registry name. Default: andrew0928
  ACR_RESOURCE_GROUP   ACR resource group. Default: let Azure CLI resolve by ACR name.
  SITES                Space-separated sites to deploy: common applebts petshop.
  IMAGE_TAG            Required image tag in ACR. Default: develop
USAGE
}

if [[ -z "$RESOURCE_GROUP" || -z "$CONTAINERAPPS_ENV" ]]; then
    usage
    exit 1
fi

app_name_for_site() {
    case "$1" in
        common) echo "andrewshop-common-site" ;;
        applebts) echo "andrewshop-applebts-site" ;;
        petshop) echo "andrewshop-petshop-site" ;;
        *)
            echo "Unknown site: $1" >&2
            exit 1
            ;;
    esac
}

yaml_file_for_site() {
    case "$1" in
        common) echo "${SCRIPT_DIR}/common.site-prod.aca.yaml" ;;
        applebts) echo "${SCRIPT_DIR}/applebts.site-prod.aca.yaml" ;;
        petshop) echo "${SCRIPT_DIR}/petshop.site-prod.aca.yaml" ;;
        *)
            echo "Unknown site: $1" >&2
            exit 1
            ;;
    esac
}

repositories_for_site() {
    case "$1" in
        common)
            echo "andrewdemo-shop-common-seed"
            echo "andrewdemo-shop-common-api"
            echo "andrewdemo-shop-common-storefront"
            ;;
        applebts)
            echo "andrewdemo-shop-applebts-seed"
            echo "andrewdemo-shop-common-api"
            echo "andrewdemo-shop-applebts-btsapi"
            echo "andrewdemo-shop-applebts-storefront"
            ;;
        petshop)
            echo "andrewdemo-shop-petshop-seed"
            echo "andrewdemo-shop-common-api"
            echo "andrewdemo-shop-petshop-reservationapi"
            echo "andrewdemo-shop-petshop-storefront"
            ;;
        *)
            echo "Unknown site: $1" >&2
            exit 1
            ;;
    esac
}

acr_id() {
    if [[ -n "$ACR_RESOURCE_GROUP" ]]; then
        az acr show --name "$ACR_NAME" --resource-group "$ACR_RESOURCE_GROUP" --query id -o tsv
    else
        az acr show --name "$ACR_NAME" --query id -o tsv
    fi
}

acr_show_tags() {
    local repository="$1"

    az acr repository show-tags \
        --name "$ACR_NAME" \
        --repository "$repository" \
        -o tsv
}

ensure_required_images() {
    local site="$1"
    local repository
    local missing=false
    local tags

    for repository in $(repositories_for_site "$site"); do
        if ! tags="$(acr_show_tags "$repository" 2>/dev/null)"; then
            echo "Missing image repository in ACR: ${ACR_NAME}.azurecr.io/${repository}" >&2
            missing=true
            continue
        fi

        if ! grep -qx "$IMAGE_TAG" <<< "$tags"; then
            echo "Missing image tag in ACR: ${ACR_NAME}.azurecr.io/${repository}:${IMAGE_TAG}" >&2
            missing=true
        fi
    done

    if [[ "$missing" == true ]]; then
        echo "Build and push required images first, for example:" >&2
        case "$site" in
            common) echo "  ./build.sh --push --image common-seed --image common-api --image common-storefront" >&2 ;;
            applebts) echo "  ./build.sh --push --image applebts-seed --image common-api --image applebts-btsapi --image applebts-storefront" >&2 ;;
            petshop) echo "  ./build.sh --push --image petshop-seed --image common-api --image petshop-reservationapi --image petshop-storefront" >&2 ;;
        esac
        echo "or push the full set:" >&2
        echo "  ./build.sh --push" >&2
        exit 1
    fi
}

ensure_providers() {
    az provider register --namespace Microsoft.App >/dev/null
    az provider register --namespace Microsoft.OperationalInsights >/dev/null
}

ensure_resource_group() {
    if az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
        return 0
    fi

    if [[ -z "$LOCATION" ]]; then
        echo "Resource group ${RESOURCE_GROUP} does not exist. Set LOCATION to create it." >&2
        exit 1
    fi

    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null
}

ensure_environment() {
    if az containerapp env show --name "$CONTAINERAPPS_ENV" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
        return 0
    fi

    if [[ -z "$LOCATION" ]]; then
        echo "Container Apps environment ${CONTAINERAPPS_ENV} does not exist. Set LOCATION to create it." >&2
        exit 1
    fi

    az containerapp env create \
        --name "$CONTAINERAPPS_ENV" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" >/dev/null
}

ensure_bootstrap_app() {
    local app_name="$1"

    if az containerapp show --name "$app_name" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
        return 0
    fi

    az containerapp create \
        --name "$app_name" \
        --resource-group "$RESOURCE_GROUP" \
        --environment "$CONTAINERAPPS_ENV" \
        --image nginx:1.27-alpine \
        --ingress external \
        --target-port 80 \
        --min-replicas 1 \
        --max-replicas 1 >/dev/null
}

ensure_acr_pull_role() {
    local app_name="$1"
    local principal_id
    local registry_id
    local attempt
    local output

    az containerapp identity assign \
        --name "$app_name" \
        --resource-group "$RESOURCE_GROUP" \
        --system-assigned >/dev/null

    principal_id="$(az containerapp identity show \
        --name "$app_name" \
        --resource-group "$RESOURCE_GROUP" \
        --query principalId \
        -o tsv)"

    registry_id="$(acr_id)"

    for attempt in 1 2 3 4 5; do
        if output="$(az role assignment create \
            --assignee-object-id "$principal_id" \
            --assignee-principal-type ServicePrincipal \
            --role AcrPull \
            --scope "$registry_id" 2>&1)"; then
            return 0
        fi

        if grep -qi "RoleAssignmentExists" <<< "$output"; then
            return 0
        fi

        echo "ACR role assignment not ready for ${app_name}; retrying (${attempt}/5)..." >&2
        echo "$output" >&2
        sleep 15
    done

    echo "Unable to assign AcrPull to ${app_name}." >&2
    return 1
}

deploy_yaml() {
    local app_name="$1"
    local yaml_file="$2"
    local attempt

    for attempt in 1 2 3 4; do
        if az containerapp update \
            --name "$app_name" \
            --resource-group "$RESOURCE_GROUP" \
            --yaml "$yaml_file"; then
            return 0
        fi

        echo "Deployment failed for ${app_name}; retrying after ACR role propagation (${attempt}/4)..." >&2
        sleep 20
    done

    az containerapp update \
        --name "$app_name" \
        --resource-group "$RESOURCE_GROUP" \
        --yaml "$yaml_file"
}

ensure_providers
ensure_resource_group
ensure_environment

for site in $SITES; do
    app_name="$(app_name_for_site "$site")"
    yaml_file="$(yaml_file_for_site "$site")"

    echo "==> Deploying ${site}: ${app_name}"
    ensure_required_images "$site"
    ensure_bootstrap_app "$app_name"
    ensure_acr_pull_role "$app_name"
    deploy_yaml "$app_name" "$yaml_file"
done

echo "Done."

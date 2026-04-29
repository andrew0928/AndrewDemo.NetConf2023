#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-}"
APP_NAME="${APP_NAME:-andrewshop-common-site}"
CONTAINER="${CONTAINER:-}"
REVISION="${REVISION:-}"
ACR_NAME="${ACR_NAME:-andrew0928}"
IMAGE_TAG="${IMAGE_TAG:-develop}"

usage() {
    cat <<'USAGE'
Usage:
  RESOURCE_GROUP=<resource-group> [APP_NAME=andrewshop-common-site] bash deploy/aca/diagnose-site-prod.sh

Optional:
  CONTAINER=<container-name>   Show console logs for one container.
  REVISION=<revision-name>     Use one specific revision for replica/log commands.
  ACR_NAME=andrew0928
  IMAGE_TAG=develop
USAGE
}

if [[ -z "$RESOURCE_GROUP" ]]; then
    usage
    exit 1
fi

site_for_app() {
    case "$APP_NAME" in
        andrewshop-common-site) echo "common" ;;
        andrewshop-applebts-site) echo "applebts" ;;
        andrewshop-petshop-site) echo "petshop" ;;
        *) echo "unknown" ;;
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
            return 0
            ;;
    esac
}

show_required_images() {
    local site="$1"
    local repository
    local tags

    if [[ "$site" == unknown ]]; then
        return 0
    fi

    echo
    echo "==> ACR image tag check (${ACR_NAME}.azurecr.io, tag: ${IMAGE_TAG})"
    for repository in $(repositories_for_site "$site"); do
        if ! tags="$(az acr repository show-tags --name "$ACR_NAME" --repository "$repository" -o tsv 2>/dev/null)"; then
            echo "MISSING repository: ${repository}"
            continue
        fi

        if grep -qx "$IMAGE_TAG" <<< "$tags"; then
            echo "OK ${repository}:${IMAGE_TAG}"
        else
            echo "MISSING tag: ${repository}:${IMAGE_TAG}"
        fi
    done
}

show_revision_summary() {
    echo
    echo "==> Revisions"
    az containerapp revision list \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "[].{name:name,active:properties.active,traffic:properties.trafficWeight,created:properties.createdTime,running:properties.runningState}" \
        -o table
}

resolve_revision() {
    if [[ -n "$REVISION" ]]; then
        echo "$REVISION"
        return 0
    fi

    az containerapp revision list \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "[?properties.trafficWeight > \`0\`].name | [0]" \
        -o tsv
}

show_replicas() {
    local revision="$1"

    if [[ -z "$revision" ]]; then
        return 0
    fi

    echo
    echo "==> Replicas for ${revision}"
    az containerapp replica list \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --revision "$revision" \
        --query "[].{name:name,containers:properties.containers[].name,initContainers:properties.initContainers[].name}" \
        -o table || true
}

show_logs() {
    local revision="$1"

    echo
    echo "==> System logs"
    az containerapp logs show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --revision "$revision" \
        --type system \
        --tail 80 || true

    if [[ -n "$CONTAINER" ]]; then
        echo
        echo "==> Console logs (${CONTAINER})"
        az containerapp logs show \
            --name "$APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision "$revision" \
            --container "$CONTAINER" \
            --type console \
            --tail 120 || true
    fi
}

site="$(site_for_app)"
revision="$(resolve_revision)"

show_required_images "$site"
show_revision_summary
show_replicas "$revision"

if [[ -n "$revision" ]]; then
    show_logs "$revision"
fi

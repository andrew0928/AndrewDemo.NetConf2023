#!/usr/bin/env bash
set -euo pipefail

log() { echo "[seed][$(date -u +%Y-%m-%dT%H:%M:%SZ)] $*"; }

DEST_DIR="${SEED_DEST:-/data}"
SRC_DIR="/seed-src"
FLAG_FILE="${DEST_DIR}/.seed_done"

log "Start seeding to ${DEST_DIR}"

# 確保目標目錄存在
mkdir -p "${DEST_DIR}"

# 複製所有檔案
log "Copying all files from ${SRC_DIR} to ${DEST_DIR}"
cp -a "${SRC_DIR}"/* "${DEST_DIR}/"

# 設定權限
log "Setting permissions on ${DEST_DIR}"
chmod -R 777 "${DEST_DIR}" || true

# 權限調整 (如果需要特定 UID/GID)
if [[ -n "${SEED_CHOWN_UID:-}" && -n "${SEED_CHOWN_GID:-}" ]]; then
  log "chown -R ${SEED_CHOWN_UID}:${SEED_CHOWN_GID} ${DEST_DIR}"
  chown -R "${SEED_CHOWN_UID}:${SEED_CHOWN_GID}" "${DEST_DIR}" || true
fi

# 建立完成旗標
echo "seeded $(date -u +%Y-%m-%dT%H:%M:%SZ)" > "${FLAG_FILE}"

log "Seeding completed. Created ${FLAG_FILE}"
exit 0

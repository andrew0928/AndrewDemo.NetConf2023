#!/usr/bin/env bash
set -euo pipefail

query_repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ $# -gt 2 ]]; then
    echo "用法：bash scripts/query-litedb.sh [SELECT查詢] [資料庫路徑]" >&2
    exit 2
fi
query_sql="${1:-SELECT \$ FROM members;}"
query_database="${2:-$query_repo_root/.local/data/common-site-dev/shop-database.db}"
exec dotnet run --project "$query_repo_root/src/AndrewDemo.NetConf2023.DatabaseQuery" --verbosity quiet -- "$query_database" "$query_sql"

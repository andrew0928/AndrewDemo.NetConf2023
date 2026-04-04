#!/usr/bin/env bash

set -uo pipefail

API_HOST="${API_HOST:-http://localhost:5108}"
BTS_API_HOST="${BTS_API_HOST:-http://localhost:5118}"
REDIRECT_URI="${REDIRECT_URI:-https://localhost/callback}"

PASS_COUNT=0
FAIL_COUNT=0
SKIP_COUNT=0

require_bin() {
  local name="$1"
  if ! command -v "$name" >/dev/null 2>&1; then
    echo "[fatal] command not found: $name" >&2
    exit 1
  fi
}

require_bin curl
require_bin jq

json_post() {
  local url="$1"
  local token="$2"
  local body="$3"
  if [[ -n "$token" ]]; then
    curl -sS --fail-with-body -X POST "$url" \
      -H "Authorization: Bearer $token" \
      -H "Content-Type: application/json" \
      -d "$body"
  else
    curl -sS --fail-with-body -X POST "$url" \
      -H "Content-Type: application/json" \
      -d "$body"
  fi
}

json_get() {
  local url="$1"
  local token="${2:-}"
  if [[ -n "$token" ]]; then
    curl -sS --fail-with-body "$url" -H "Authorization: Bearer $token"
  else
    curl -sS --fail-with-body "$url"
  fi
}

assert_eq() {
  local actual="$1"
  local expected="$2"
  local message="$3"
  if [[ "$actual" != "$expected" ]]; then
    echo "  [assert] $message: expected '$expected', actual '$actual'" >&2
    return 1
  fi
}

assert_contains() {
  local haystack="$1"
  local needle="$2"
  local message="$3"
  if [[ "$haystack" != *"$needle"* ]]; then
    echo "  [assert] $message: expected contains '$needle', actual '$haystack'" >&2
    return 1
  fi
}

login() {
  local name="$1"
  local headers_file
  headers_file="$(mktemp)"

  curl -sS -D "$headers_file" -o /dev/null -X POST "${API_HOST}/api/login/authorize" \
    --data-urlencode "name=${name}" \
    --data-urlencode "password=ignored" \
    --data-urlencode "client_id=applebts-script" \
    --data-urlencode "redirect_uri=${REDIRECT_URI}" \
    --data-urlencode "state=applebts" || {
      rm -f "$headers_file"
      return 1
    }

  local location
  location="$(awk 'BEGIN{IGNORECASE=1} /^Location:/ {print $2}' "$headers_file" | tr -d '\r')"
  rm -f "$headers_file"

  local code
  code="$(printf '%s' "$location" | sed -n 's/.*code=\([^&]*\).*/\1/p')"
  if [[ -z "$code" ]]; then
    echo "[fatal] oauth code not found for user $name" >&2
    return 1
  fi

  curl -sS --fail-with-body -X POST "${API_HOST}/api/login/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "code=${code}" \
    | jq -r '.access_token'
}

create_cart() {
  local token="$1"
  json_post "${API_HOST}/api/carts/create" "$token" '{}' | jq -r '.id'
}

add_item() {
  local token="$1"
  local cart_id="$2"
  local product_id="$3"
  local qty="$4"
  local parent_line_id="${5:-}"

  if [[ -n "$parent_line_id" ]]; then
    json_post "${API_HOST}/api/carts/${cart_id}/items" "$token" "$(jq -cn --arg productId "$product_id" --argjson qty "$qty" --arg parentLineId "$parent_line_id" '{productId:$productId, qty:$qty, parentLineId:$parentLineId}')"
  else
    json_post "${API_HOST}/api/carts/${cart_id}/items" "$token" "$(jq -cn --arg productId "$product_id" --argjson qty "$qty" '{productId:$productId, qty:$qty}')"
  fi
}

estimate_cart() {
  local token="$1"
  local cart_id="$2"
  json_post "${API_HOST}/api/carts/${cart_id}/estimate" "$token" '{}'
}

verify_email() {
  local token="$1"
  local email="$2"
  json_post "${BTS_API_HOST}/bts-api/qualification/verify" "$token" "$(jq -cn --arg email "$email" '{email:$email}')"
}

run_case() {
  local case_id="$1"
  shift
  if "$@"; then
    PASS_COUNT=$((PASS_COUNT + 1))
    echo "[PASS] ${case_id}"
  else
    FAIL_COUNT=$((FAIL_COUNT + 1))
    echo "[FAIL] ${case_id}" >&2
  fi
}

skip_case() {
  local case_id="$1"
  local reason="$2"
  SKIP_COUNT=$((SKIP_COUNT + 1))
  echo "[SKIP] ${case_id} - ${reason}"
}

case_m01_p01() {
  local token cart_id main_line_id cart_json estimate_json total amount kind
  token="$(login "m01-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1
  json_get "${BTS_API_HOST}/bts-api/catalog/macbook-air" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  cart_json="$(add_item "$token" "$cart_id" "macbook-air" 1)" || return 1
  main_line_id="$(printf '%s' "$cart_json" | jq -r '.lineItems[-1].lineId')"
  add_item "$token" "$cart_id" "airpods-4" 1 "$main_line_id" >/dev/null || return 1

  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1
  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  amount="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].amount')"
  kind="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].kind')"

  assert_eq "$total" "31400" "P-01 total" || return 1
  assert_eq "$amount" "-10490" "P-01 discount amount" || return 1
  assert_eq "$kind" "0" "P-01 record kind" || return 1
}

case_m02() {
  local token cart_id estimate_json total kind description
  token="$(login "m02-user")" || return 1
  verify_email "$token" "student@gmail.com" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  add_item "$token" "$cart_id" "macbook-air" 1 >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  kind="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].kind')"
  description="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].description')"

  assert_eq "$total" "35900" "M-02 total" || return 1
  assert_eq "$kind" "1" "M-02 hint kind" || return 1
  assert_contains "$description" "教育驗證尚未通過" "M-02 hint description" || return 1
}

case_m04() {
  local token cart_id estimate_json total discount_count
  token="$(login "m04-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  add_item "$token" "$cart_id" "iphone-16" 1 >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  discount_count="$(printf '%s' "$estimate_json" | jq -r '.discounts | length')"

  assert_eq "$total" "25900" "M-04 total" || return 1
  assert_eq "$discount_count" "0" "M-04 discount count" || return 1
}

case_g03() {
  local token cart_id estimate_json total
  token="$(login "g03-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  add_item "$token" "$cart_id" "macbook-air" 1 >/dev/null || return 1
  add_item "$token" "$cart_id" "airpods-4" 1 >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  assert_eq "$total" "37390" "G-03 total" || return 1
}

case_g04() {
  local token cart_id main_line_id cart_json estimate_json total
  token="$(login "g04-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  cart_json="$(add_item "$token" "$cart_id" "macbook-air" 1)" || return 1
  main_line_id="$(printf '%s' "$cart_json" | jq -r '.lineItems[-1].lineId')"
  add_item "$token" "$cart_id" "apple-pencil-pro" 1 "$main_line_id" >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  assert_eq "$total" "35900" "G-04 total" || return 1
}

case_p03() {
  local token cart_id main_line_id cart_json estimate_json total
  token="$(login "p03-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  cart_json="$(add_item "$token" "$cart_id" "macbook-air" 1)" || return 1
  main_line_id="$(printf '%s' "$cart_json" | jq -r '.lineItems[-1].lineId')"
  add_item "$token" "$cart_id" "airpods-pro-3" 1 "$main_line_id" >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  assert_eq "$total" "33400" "P-03 total" || return 1
}

case_p04() {
  local token cart_id estimate_json total amount
  token="$(login "p04-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  add_item "$token" "$cart_id" "macbook-air" 1 >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  amount="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].amount')"
  assert_eq "$total" "31400" "P-04 total" || return 1
  assert_eq "$amount" "-4500" "P-04 main-only discount" || return 1
}

case_c04() {
  local token cart_id estimate_json total description
  token="$(login "bts-expired-user")" || return 1

  cart_id="$(create_cart "$token")" || return 1
  add_item "$token" "$cart_id" "macbook-air" 1 >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  description="$(printf '%s' "$estimate_json" | jq -r '.discounts[0].description')"
  assert_eq "$total" "35900" "C-04 total" || return 1
  assert_contains "$description" "教育資格已過期" "C-04 hint description" || return 1
}

case_c05() {
  local token cart_id main_line_id cart_json estimate_json hint_kind total
  token="$(login "c05-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  cart_json="$(add_item "$token" "$cart_id" "macbook-air" 1)" || return 1
  main_line_id="$(printf '%s' "$cart_json" | jq -r '.lineItems[-1].lineId')"
  add_item "$token" "$cart_id" "airpods-4" 1 "$main_line_id" >/dev/null || return 1
  add_item "$token" "$cart_id" "magic-trackpad" 1 "$main_line_id" >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  hint_kind="$(printf '%s' "$estimate_json" | jq -r '.discounts[] | select(.kind == 1) | .kind' | head -n 1)"
  assert_eq "$total" "41890" "C-05 total" || return 1
  assert_eq "$hint_kind" "1" "C-05 hint exists" || return 1
}

case_c06() {
  local token cart_id main_line_id cart_json estimate_json total
  token="$(login "c06-user")" || return 1
  verify_email "$token" "student@campus.edu.tw" >/dev/null || return 1

  cart_id="$(create_cart "$token")" || return 1
  cart_json="$(add_item "$token" "$cart_id" "mac-mini" 1)" || return 1
  main_line_id="$(printf '%s' "$cart_json" | jq -r '.lineItems[-1].lineId')"
  add_item "$token" "$cart_id" "airpods-4" 1 "$main_line_id" >/dev/null || return 1
  estimate_json="$(estimate_cart "$token" "$cart_id")" || return 1

  total="$(printf '%s' "$estimate_json" | jq -r '.totalPrice')"
  assert_eq "$total" "24390" "C-06 total" || return 1
}

echo "[info] AppleBTS decision table API verification"
echo "[info] API_HOST=${API_HOST}"
echo "[info] BTS_API_HOST=${BTS_API_HOST}"

run_case "M-01/P-01" case_m01_p01
run_case "M-02" case_m02
skip_case "M-03" "time mock pending"
run_case "M-04" case_m04
run_case "G-03" case_g03
run_case "G-04" case_g04
run_case "P-03" case_p03
run_case "P-04" case_p04
skip_case "C-03" "time mock pending"
run_case "C-04" case_c04
run_case "C-05" case_c05
run_case "C-06" case_c06

echo
echo "[summary] pass=${PASS_COUNT} fail=${FAIL_COUNT} skip=${SKIP_COUNT}"

if [[ "$FAIL_COUNT" -gt 0 ]]; then
  exit 1
fi

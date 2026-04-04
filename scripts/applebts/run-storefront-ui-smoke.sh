#!/bin/zsh

set -euo pipefail

base_url="${1:-http://localhost:5138}"

cookie_jar="$(mktemp)"
auth_headers="$(mktemp)"
authorize_post_headers="$(mktemp)"
qualification_page="$(mktemp)"
confirm_page="$(mktemp)"
checkout_page="$(mktemp)"

cleanup() {
    rm -f "$cookie_jar" "$auth_headers" "$authorize_post_headers" "$qualification_page" "$confirm_page" "$checkout_page"
}

trap cleanup EXIT

extract_location() {
    local header_file="$1"
    awk 'BEGIN { IGNORECASE = 1 } /^Location:/ { print $2 }' "$header_file" | tr -d '\r'
}

extract_query_value() {
    local url="$1"
    local key="$2"
    printf "%s" "$url" | sed -n "s/.*[?&]$key=\\([^&]*\\).*/\\1/p"
}

extract_antiforgery_token() {
    local html_file="$1"
    sed -n 's/.*name="__RequestVerificationToken" type="hidden" value="\([^"]*\)".*/\1/p' "$html_file" | tail -n 1
}

echo "[1] 檢查匿名 BTS 型錄"
catalog_html="$(curl -sS --fail-with-body "$base_url/bts")"
printf "%s" "$catalog_html" | grep -q "Apple BTS 專區"
printf "%s" "$catalog_html" | grep -q "MacBook Air"
printf "%s" "$catalog_html" | grep -q "原價 NT\\$"
printf "%s" "$catalog_html" | grep -q "最高可折 NT\\$"

echo "[2] 完成 storefront login flow"
client_id="andrewshop-applebts-storefront"
redirect_uri="${base_url}/auth/callback"
state="applebts-smoke"

curl -sS -D "$authorize_post_headers" -o /dev/null -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/api/login/authorize" \
    --data-urlencode "name=bts-storefront-user" \
    --data-urlencode "client_id=$client_id" \
    --data-urlencode "redirect_uri=$redirect_uri" \
    --data-urlencode "response_type=code" \
    --data-urlencode "scope=openid" \
    --data-urlencode "state=$state" \
    --data-urlencode "password=x"
callback_url="$(extract_location "$authorize_post_headers")"

curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" -L "$callback_url" >/dev/null

echo "[3] 驗證教育資格"
curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    "$base_url/bts/qualification" > "$qualification_page"
qualification_token="$(extract_antiforgery_token "$qualification_page")"
[ -n "$qualification_token" ]

qualification_response="$(curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/bts/qualification" \
    --data-urlencode "__RequestVerificationToken=$qualification_token" \
    --data-urlencode "Email=student@campus.edu.tw")"
printf "%s" "$qualification_response" | grep -q "已通過驗證"

echo "[3A] 會員頁顯示 Apple BTS 教育資格摘要"
member_html="$(curl -sS --fail-with-body -b "$cookie_jar" "$base_url/member")"
printf "%s" "$member_html" | grep -q "Apple BTS 教育資格"
printf "%s" "$member_html" | grep -q "已通過驗證"
printf "%s" "$member_html" | grep -q "student@campus.edu.tw"

echo "[4] 確認 gift 加入前會顯示確認區塊"
curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    "$base_url/bts/products/macbook-air?confirmGiftProductId=airpods-4" > "$confirm_page"
grep -q "確認加入 BTS 組合" "$confirm_page"
grep -q "AirPods 4" "$confirm_page"
confirm_token="$(extract_antiforgery_token "$confirm_page")"
[ -n "$confirm_token" ]

echo "[5] 同意加入主商品與 gift"
curl -sS -D /dev/null -o /dev/null -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/bts/products/macbook-air?handler=AddBundle" \
    --data-urlencode "__RequestVerificationToken=$confirm_token" \
    --data-urlencode "giftProductId=airpods-4" >/dev/null

echo "[6] 檢查購物車"
cart_html="$(curl -sS --fail-with-body -b "$cookie_jar" "$base_url/cart")"
printf "%s" "$cart_html" | grep -q "MacBook Air"
printf "%s" "$cart_html" | grep -q "AirPods 4"
printf "%s" "$cart_html" | grep -q "BTS"
printf "%s" "$cart_html" | grep -q "31400"

echo "[7] 完成結帳並檢查訂單折扣明細"
curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    "$base_url/checkout" > "$checkout_page"
checkout_token="$(extract_antiforgery_token "$checkout_page")"
[ -n "$checkout_token" ]

curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/checkout" \
    --data-urlencode "__RequestVerificationToken=$checkout_token" >/dev/null

orders_html="$(curl -sS --fail-with-body -b "$cookie_jar" "$base_url/member/orders")"
printf "%s" "$orders_html" | grep -q "折扣明細"
printf "%s" "$orders_html" | grep -q "BTS 優惠"
printf "%s" "$orders_html" | grep -q "主商品套用 BTS 價格"

echo "[8] 重新加入組合後，刪除主商品 line，gift 應一併移除"
curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    "$base_url/bts/products/macbook-air?confirmGiftProductId=airpods-4" > "$confirm_page"
confirm_token="$(extract_antiforgery_token "$confirm_page")"
[ -n "$confirm_token" ]

curl -sS -D /dev/null -o /dev/null -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/bts/products/macbook-air?handler=AddBundle" \
    --data-urlencode "__RequestVerificationToken=$confirm_token" \
    --data-urlencode "giftProductId=airpods-4" >/dev/null

cart_html="$(curl -sS --fail-with-body -b "$cookie_jar" "$base_url/cart")"
line_id="$(printf "%s" "$cart_html" | sed -n 's/.*name="lineId" value="\([^"]*\)".*/\1/p' | head -n 1)"
page_token="$(printf "%s" "$cart_html" | sed -n 's/.*name="__RequestVerificationToken" type="hidden" value="\([^"]*\)".*/\1/p' | tail -n 1)"
[ -n "$line_id" ]
[ -n "$page_token" ]

curl -sS --fail-with-body -b "$cookie_jar" -c "$cookie_jar" \
    -X POST "$base_url/cart?handler=RemoveLine" \
    --data-urlencode "__RequestVerificationToken=$page_token" \
    --data-urlencode "lineId=$line_id" >/dev/null

updated_cart_html="$(curl -sS --fail-with-body -b "$cookie_jar" "$base_url/cart")"
printf "%s" "$updated_cart_html" | grep -q "目前購物車是空的。"

echo "SMOKE_OK"

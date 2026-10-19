#!/usr/bin/env python3
"""使用全新 temporary DB 執行標準 API / AdminCLI smoke；--keep-running 留給瀏覽器驗收。"""
import argparse
import json
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import tempfile
import time
import urllib.request
import urllib.parse
import urllib.error

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "AndrewDemo.NetConf2023."


def binary(name, folder="src", assembly=None):
    return ROOT / folder / (PREFIX + name) / "bin/Debug/net10.0" / ((assembly or PREFIX + name) + ".dll")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api-port", type=int, default=5187)
    parser.add_argument("--ui-port", type=int, default=5188)
    parser.add_argument("--keep-running", action="store_true")
    options = parser.parse_args()
    for name in ["API", "DatabaseInit", "CommonStorefront", "AdminCLI"]:
        if not binary(name, assembly="shop-admin" if name == "AdminCLI" else None).exists():
            raise SystemExit("請先串行建置 src/AndrewDemo.NetConf2023.slnx。")
    directory = Path(tempfile.mkdtemp(prefix="tip-phase3-smoke-"))
    seed = directory / "seed"
    shutil.copytree(binary("DatabaseInit").parent, seed, ignore=shutil.ignore_patterns("*.db"))
    api = f"http://127.0.0.1:{options.api_port}"
    ui = f"http://127.0.0.1:{options.ui_port}"
    env = dict(os.environ, SHOP_DATABASE_FILEPATH=str(seed / "shop-database.db"),
               SHOP_ADMIN_ACCESS_TOKEN="admin_" + secrets.token_hex(20), SHOP_ADMIN_BASE_URL=api,
               SHOP_ID="default", ASPNETCORE_ENVIRONMENT="Development")
    processes = []
    logs = []

    def start(name, url, additions=None):
        log = (directory / (name + ".log")).open("w")
        logs.append(log)
        process = subprocess.Popen(["dotnet", str(binary(name)), "--urls", url,
                                    "--contentRoot", str(ROOT / "src" / (PREFIX + name))],
                                   cwd=directory, env=dict(env, **(additions or {})), stdout=log, stderr=log)
        processes.append(process)
        return process

    def request(path, token=None, data=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = urllib.request.Request(api + path, headers=headers,
                                     data=None if data is None else json.dumps(data).encode())
        with urllib.request.urlopen(req, timeout=20) as response:
            return json.load(response), response.headers

    try:
        # destructive seed 只在剛建立的 temporary 複本執行。
        subprocess.run(["dotnet", str(seed / (PREFIX + "DatabaseInit.dll"))], cwd=directory,
                       check=True, stdout=subprocess.DEVNULL)
        backend = start("API", api)
        for _ in range(100):
            if backend.poll() is not None:
                raise RuntimeError(f"API 啟動失敗，請檢查 {directory / 'API.log'}")
            try:
                request("/api/products")
                break
            except OSError:
                time.sleep(.1)
        else:
            raise RuntimeError("API 啟動逾時")
        results = []
        for name in ["phase3-normal", "test"]:
            # OAuth 的正式入口不會輸出 access token。
            form = urllib.parse.urlencode({"name": name, "client_id": "smoke", "redirect_uri": api + "/unused"}).encode()
            class NoRedirect(urllib.request.HTTPRedirectHandler):
                def redirect_request(self, *args):
                    return None
            opener = urllib.request.build_opener(NoRedirect)
            try:
                response = opener.open(urllib.request.Request(api + "/oauth/authorize", data=form))
            except urllib.error.HTTPError as response_error:
                if response_error.code != 302:
                    raise
                response = response_error
            code = response.headers["X-OAuth-Code"]
            with urllib.request.urlopen(urllib.request.Request(api + "/oauth/token", data=urllib.parse.urlencode({"code": code}).encode())) as response:
                token = json.load(response)["access_token"]
            for payment in [0, 990001]:
                cart, _ = request("/api/carts/create", token, {})
                request(f"/api/carts/{cart['id']}/items", token, {"productId": "2", "qty": 1})
                created, _ = request("/api/checkout/create", token, {"cartId": cart["id"], "paymentId": payment})
                completed, headers = request("/api/checkout/complete", token, {"transactionId": created["transactionId"]})
                test = name == "test" or payment == 990001
                assert completed["orderDetail"]["isTest"] == test
                assert headers["X-Shop-Behavior"] == ("test" if test else "normal")
                assert headers.get("X-Shop-Skipped-Effects") == ("inventory.deduct" if test else None)
                results.append({"member": name, "payment": payment, "checkout": created["transactionId"],
                                "order": completed["orderDetail"]["id"], "isTest": test})
            history, headers = request("/api/member/orders", token)
            assert len(history["orders"]) == 2
            assert "X-Shop-Transaction-Id" not in headers
        for purpose in ["normal", "test", "all"]:
            command = subprocess.run(["dotnet", str(binary("AdminCLI", assembly="shop-admin")), "list", "--source", "order",
                                      "--type", purpose, "--all-pages", "--format", "json"], cwd=directory,
                                     env=env, check=True, capture_output=True, text=True)
            (directory / f"cli-{purpose}.json").write_text(command.stdout)
            cli = json.loads(command.stdout)
            expected_count = {"normal": 1, "test": 3, "all": 4}[purpose]
            assert len(cli["rows"]) == expected_count
            assert cli["observations"][0]["totalAmount"] == 18 * expected_count
            admin, _ = request("/api/admin/orders?purpose=" + purpose, env["SHOP_ADMIN_ACCESS_TOKEN"])
            assert sorted(row["id"] for row in cli["rows"]) == sorted(row["id"] for row in admin["items"])
        (directory / "api-cases.json").write_text(json.dumps(results, indent=2))
        start("CommonStorefront", ui, {"Storefront__CoreApi__BaseUrl": api, "Storefront__CoreApi__PublicOAuthBaseUrl": api,
                                      "Storefront__Session__CookieName": ".AndrewShop.Phase3Smoke"})
        print(json.dumps({"evidence": str(directory), "api": api, "ui": ui, "apiCases": results}, ensure_ascii=False), flush=True)
        if options.keep_running:
            print("隔離環境已就緒；Ctrl-C 停止本次 hosts，保留 temporary 證據。", flush=True)
            while True:
                time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        for process in reversed(processes):
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
        for log in logs:
            log.close()
        inspection = subprocess.run(["dotnet", str(binary("StorageProbe", "tests")), "--inspect", str(seed / "shop-database.db")],
                                    cwd=directory, capture_output=True, text=True)
        if inspection.returncode == 0:
            (directory / "raw-readback.json").write_text(inspection.stdout)
            print("raw read-back：" + str(directory / "raw-readback.json"), flush=True)


if __name__ == "__main__":
    main()

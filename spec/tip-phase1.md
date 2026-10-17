# TiP Phase 1 定案基準

## 狀態與範圍

- 日期：2026-10-17。
- Phase 1：完成，規格與 Abstract 合約定案。
- Phase 2：尚未交付；用途判定、效果控制與 host 整合仍待實作。
- 依使用者指示，本輪先修改 code，再同步 spec／testcases；既有 Core 基礎實作納入本次交付。
- 適用標準 Core／API／CommonStorefront／CLI。AppleBTS、PetShop 領域政策排除；共用合約的呼叫參數同步不代表擴充模組已支援 TiP。

後續 Phase 2 不任意改動本清單中的正式規格與 `.Abstract`；若發現需要改變對外語意或契約，先停下來由使用者明確重新開啟規格階段。Core 內部類別、資料庫 ID 配發與 scope 建立方式可依既定規則實作。

## 正式文件與案例

| 規格 | 測試案例 | 定案內容 |
|---|---|---|
| [Core 物件](tip-core-object-definitions.md) | [物件案例](testcases/tip-core-object-definitions.md) | IsTest、PaymentId、nullable Order 關聯、獨立封存區與舊資料預設值。 |
| [白名單](tip-test-allowlist.md) | [白名單案例](testcases/tip-test-allowlist.md) | Core 唯讀清單；預設 MemberId 900001、PaymentId 990001；只在建立時分類。 |
| [Context](tip-context-contracts.md) | [Context 案例](testcases/tip-context-contracts.md) | 獨立 Identity／Transaction、不可變快照、nullable transaction、scoped 邊界。 |
| [標準流程](tip-standard-flow.md) | [流程案例](testcases/tip-standard-flow.md) | 註冊／Token／Checkout／Order、原子封存、效果控制、事件、舊交易 409。 |
| [HTTP 與操作結果](tip-http-and-operation-results.md) | [出口案例](testcases/tip-http-and-operation-results.md) | Endpoint 解析、Items、header、結果代碼、略過明細與 log。 |

既有 [Checkout 正確性](checkout-correctness-fixes.md)、[庫存規格](core-product-sku-and-inventory.md) 與 [CommonStorefront](common-storefront-baseline.md) 的 TiP 補充已同步；歷史搬移規格保留當時範圍，現行 TiP 合約以本清單為準。

## Contract 交付

| 位置 | 合約 |
|---|---|
| Abstract/Contexts | IIdentityContext、IdentitySnapshot、IdentityKind；ITransactionContext、TransactionSnapshot、TransactionKind。 |
| Abstract/Operations | OperationResult、OperationOutcome、SkippedEffect；用途、成功與否、實際略過分別表達。 |
| Abstract/Orders | IOrderEventDispatcher 簽章不變，補上每次事件獨立 scope、從 Order 恢復用途的行為契約。 |
| Core/Checkouts | Create 明確要求 PaymentId 與 RequestMember；Complete 明確要求 RequestMember、不再接受 PaymentId；增加 RecreateRequired 結果。 |
| API／Storefront.Shared | Create request 必填 paymentId；Complete request 移除；缺少付款資料的舊 Checkout 回 409／checkout_recreate_required。 |

沒有合併的 IShopExecutionContext。Product 查詢、折扣、ShopManifest 介面簽章不變。Member、Order、Checkout command 與 database context 維持 Core 所有，不為此次改版額外搬入 Abstract。

## AC 對照

下表為規格覆蓋，不表示完整執行流程已通過測試。案例前綴為 TC-TIP-FLOW／TC-TIP-HTTP。

| 接受條件 | 正式驗收案例 |
|---|---|
| AC-TIP-001 一般測試交易 | FLOW-202、307、308；HTTP-010。不存在的金流／出貨／統計能力標示不適用。 |
| AC-TIP-002 測試 PaymentId | FLOW-102、203。 |
| AC-TIP-003 正常業務失敗 | FLOW-204；HTTP-005。 |
| AC-TIP-004 用途不可變 | FLOW-003、107、401、402；物件與白名單案例。 |
| AC-TIP-005／006 商品與 SKU 保護 | FLOW-303；HTTP-004。 |
| AC-TIP-009 正式／test 並行 | FLOW-301；HTTP-011。 |
| AC-TIP-010 Cart 正常異動 | FLOW-302。 |
| AC-TIP-011 買方驗證 | FLOW-205；HTTP-008。 |
| AC-TIP-013 事件與重試延續用途 | FLOW-304、305、307；HTTP-013。 |
| AC-TIP-014 用途不明為 BUG | FLOW-005、306；HTTP-009。 |
| AC-TIP-007／008／012／015 | PetShop 範圍，本輪不驗收，保留原接受條件。 |

## 已完成與 Phase 2 邊界

| 項目 | 本次交付 | Phase 2 工作 |
|---|---|---|
| 模型／持久化 | init 欄位、舊 BSON 預設值、獨立封存入口 | 服務端用途不可變保證、實際封存編排與 ID 唯一性。 |
| 白名單／Context | 唯讀查詢、初始化／Bind／Clear | 註冊與 Token 繼承、Checkout 四種用途組合、DI 與 host 接入。 |
| Checkout 合約 | 付款前移、保存／回傳值、舊紀錄 409 | Order 繼承、庫存略過、原子封存、重複與並行完成保護。 |
| 結果／出口 | Abstract 結果模型、HTTP 與 log 規格 | 結果收集、middleware、headers、CLI／事件結果與 UI 顯示。 |
| 事件 | scope／用途承接契約 | 新 scope 載入 Order、效果控制、context BUG 與隔離驗證。 |

舊 Checkout 缺少或 null PaymentId 時，保留紀錄並要求重新建立，不補值、不建立相容 complete 入口。已保存的 0 是明確值，不以模型預設值猜測欄位是否存在。

## 驗證紀錄

2026-10-17：全套 107 項測試通過，0 failed、0 skipped。

| 測試專案 | 通過數 |
|---|---:|
| Core.Tests | 58 |
| WebContract.Tests | 9 |
| AppleBTS.Extension.Tests | 19 |
| PetShop.Extension.Tests | 13 |
| PetShop.API.Tests | 8 |

新增的自動化證據涵蓋物件保存、白名單、Context、不可變結果模型、付款輸入搬移與舊交易 409。WebContract 為序列化／client／controller 測試，沒有宣稱 middleware 或完整 HTTP TiP 流程已通過。擴充模組測試為共用合約的回歸檢查。

驗證命令：

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo
```

本地 Docker 尚未重建此批變更；目前不能用既有 Web UI 驗證完整 TiP。Phase 2 完成後須重建標準環境，依上述流程／出口案例驗證正式與測試交易。

## Phase 2 建議順序

1. 註冊與 Token 繼承、Core 用途更新保護，建立可重複使用的正式／測試資料。
2. Checkout 建立分類、Order 繼承與關聯、交易 ID 配發、原子封存、正式扣庫存／test 略過。
3. Scoped DI、HTTP／CLI 操作範圍、事件獨立 scope 與用途恢復；補齊跨範圍及併發測試。
4. Product／SKU 受保護寫入、OperationResult 收集、middleware headers／log 與錯誤映射。
5. CommonStorefront 明確選用測試付款來源與結果呈現，重建本地環境，完成標準流程整合與 UI 驗收。

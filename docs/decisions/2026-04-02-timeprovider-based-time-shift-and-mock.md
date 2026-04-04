# TimeProvider 化的時間平移與 Time Mock 遷移方向

## 狀態

- proposed
- 日期：2026-04-02

## 背景

目前系統內仍有多處直接呼叫 `DateTime.Now` 與 `DateTime.UtcNow`。這會帶來兩個問題：

- 測試與 demo 無法穩定重現特定時間情境
- 後續若要驗證 campaign 過期、token 過期、跨日等案例，無法在不改系統時間的前提下精準控制時間

本輪也已確認新的需求方向：

- 在 `appsettings` 指定「期待的啟動時間」
- host 啟動時計算「期待時間」與「真實啟動時間」的固定差值
- 後續所有時間讀取都套用這個固定差值，讓時間仍隨真實世界流動，但整體時間軸被平移
- 必須嚴格檢查 production code 是否仍直接存取 `DateTime.Now` / `DateTime.UtcNow`

使用者另提供 [從 DateTime 的 Mock 技巧談 PoC 的應用](https://columns.chicken-house.net/2022/05/29/datetime-mock/) 作為參考，希望確認 .NET 10 的 `TimeProvider` 是否足以承接該文章想解決的問題。

## 決策

- 後續時間抽象的 canonical 基底採用 `.NET` 內建 `TimeProvider`
- 不另做全域 singleton 版的 `DateTimeUtil`
- 為了支援「期待啟動時間」與「整體時間差」需求，需在 `TimeProvider` 之上建立自訂 `ShiftedTimeProvider`
- `ShiftedTimeProvider` 的責任為：
  - 啟動時讀取設定的期待時間與時區
  - 以真實啟動時間計算固定 offset
  - 後續所有 `GetUtcNow()` / `GetLocalNow()` 都以 `system time + offset` 回傳
- runtime 使用 `ShiftedTimeProvider`
- unit test / integration test 使用 `FakeTimeProvider`
- production code 不再允許直接呼叫：
  - `DateTime.Now`
  - `DateTime.UtcNow`
  - `DateTime.Today`
  - `DateTimeOffset.Now`
- 所有業務與 host code 應改為依賴 `TimeProvider` 或其 thin wrapper 取得現在時間
- 這次只先凍結設計方向與盤點待修清單，不在本輪直接重構

建議設定模型如下：

```json
{
  "Time": {
    "Mode": "Shifted",
    "ExpectedStartupLocal": "2026-04-01T09:00:00",
    "TimeZoneId": "Asia/Taipei"
  }
}
```

建議啟動流程如下：

1. 讀取 `Time:ExpectedStartupLocal`
2. 讀取 `Time:TimeZoneId`
3. 以 `TimeProvider.System.GetUtcNow()` 取得真實啟動時間
4. 將期待的 local time 轉成 UTC
5. 計算 `offset = expectedStartupUtc - actualStartupUtc`
6. 建立 `ShiftedTimeProvider(offset, timezone)`
7. 將該 provider 註冊進 DI，作為後續唯一的時間來源

## 影響

- 需要全面掃描並替換 production code 的直接時間存取
- API、`.Core`、Console、DatabaseInit、AppleBTS.API、AppleBTS.DatabaseInit 都會受影響
- 後續可用同一套機制支撐：
  - token 過期測試
  - campaign 有效期測試
  - checkout / cart / qualification 的時間判定
  - 本機 demo 與 API script 的穩定重現
- `TimeProvider` 也能順便支援後續若出現的 `Task.Delay(..., timeProvider)` 或 timer 類需求

## 替代方案

### 1. 沿用自刻 `DateTimeUtil` singleton

不建議。雖然概念上可行，但 `.NET` 已提供標準 `TimeProvider` 與 `FakeTimeProvider`，繼續自建 singleton 容易與現代 DI / testing 模式分岐。

### 2. 只在 unit test 使用 `FakeTimeProvider`，runtime 不做時間平移

不採用。這無法滿足「appsettings 設定期待啟動時間、整體時間軸偏移」的需求。

### 3. 維持 `DateTime.Now`，用 wrapper 或 helper 零星補洞

不採用。這會讓時間來源長期雙軌，無法嚴格保證所有判定都用同一套時間基準。

## 後續工作

- 補一份 `/docs` 說明文件，記錄目前 repo 的直接時間存取點位
- 後續另開獨立修正輪次，實作：
  - `ShiftedTimeProvider`
  - `TimeOptions`
  - 全域 DI 註冊
  - production code 全面替換
  - 禁止直接呼叫 `DateTime.Now` / `DateTime.UtcNow` 的檢查
- time mock refactor 完成前，`M-03` / `C-03` 等需要控制活動時間窗的 API 驗證情境仍可維持 pending

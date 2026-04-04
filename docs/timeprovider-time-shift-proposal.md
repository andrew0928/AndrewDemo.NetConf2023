# TimeProvider 與時間平移方案盤點

這份文件只做設計盤點，不包含實作。

## 目標

本輪要解決的是兩件事：

- 用可測試、可注入的方式統一系統時間來源
- 支援「設定期待的啟動時間，讓整個系統時間軸平移」的 PoC / demo 需求

## 與參考文章的對應

使用者提供的文章重點有兩個：

- `Now` 不應直接綁死在 `DateTime.Now`
- 時間不只要能固定，還要能隨真實時間流動，甚至在特定情境快轉

對照 `.NET 10` 內建能力：

- `TimeProvider` 已提供標準的時間抽象
- `FakeTimeProvider` 已提供可控制的測試時間
- `Task.Delay(..., timeProvider)` 與 timer 也能跟 provider 一起被控制

所以：

- 若目標只是「可 mock / 可測試 / 可注入」，`TimeProvider` 已足夠
- 若目標還包含「啟動時依設定做固定 offset，後續時間持續流動」，則需額外補一個 `ShiftedTimeProvider`

結論：

- `TimeProvider` 可以取代文章想解決的核心問題
- 但無法自動接管既有的 `DateTime.Now`
- 因此仍必須做一次 codebase migration

## 建議架構

### Runtime

- `TimeProvider.System`
  - 真實時間來源
- `ShiftedTimeProvider`
  - 封裝固定 offset
  - 作為系統 runtime 的實際 provider

### Test

- `FakeTimeProvider`
  - 單元測試與 integration test 直接注入

### 設定

```json
{
  "Time": {
    "Mode": "Shifted",
    "ExpectedStartupLocal": "2026-04-01T09:00:00",
    "TimeZoneId": "Asia/Taipei"
  }
}
```

## 建議演算法

啟動時計算一次固定 offset：

```text
actualStartupUtc   = TimeProvider.System.GetUtcNow()
expectedStartupUtc = convert(ExpectedStartupLocal, TimeZoneId) -> UTC
offset             = expectedStartupUtc - actualStartupUtc
```

之後：

```text
ShiftedTimeProvider.GetUtcNow() = TimeProvider.System.GetUtcNow() + offset
ShiftedTimeProvider.GetLocalNow() = 依 LocalTimeZone 將 shifted utc 轉 local
```

這樣可保證：

- 時間仍會流動
- 不需改系統時間
- 同一個 process 內的時間基準一致

## repo 目前待替換點位

### `.Core`

- [Checkout.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Checkout.cs)
- [Cart.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Cart.cs)
- [CartContextFactory.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Carts/CartContextFactory.cs)
- [CheckoutService.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Checkouts/CheckoutService.cs)

### 標準 API

- [MemberController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/MemberController.cs)
- [CartsController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CartsController.cs)
- [CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs)

### AppleBTS API

- [CatalogController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.AppleBTS.API/Controllers/CatalogController.cs)
- [QualificationController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.AppleBTS.API/Controllers/QualificationController.cs)

### Init / Console

- [Program.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.DatabaseInit/Program.cs)
- [Program.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.AppleBTS.DatabaseInit/Program.cs)
- [Program_HelperFunctions.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.ConsoleUI/Program_HelperFunctions.cs)

## 目前盤點數量

production `src` 內直接時間存取總數：

- `DateTime.Now`: 10
- `DateTime.UtcNow`: 15
- 合計：25

## 遷移原則

### 1. 不保留雙軌時間來源

一旦開始遷移，就不應長期同時混用：

- `DateTime.Now`
- `DateTime.UtcNow`
- `TimeProvider`

### 2. UTC 為內部基準

- domain 與 persistence 優先以 UTC 記錄
- local time 僅在顯示、輸入設定與業務規則需要時轉換

### 3. 嚴格檢查 production code

後續實作時應補一個檢查：

- 掃描 `src`
- 若發現 `DateTime.Now` / `DateTime.UtcNow` / `DateTime.Today` / `DateTimeOffset.Now`
- 直接讓檢查失敗

### 4. 先不做文章中的日切事件

目前需求只需要：

- 一致的現在時間
- 可控的時間偏移
- 可測試的 delay / timer 基礎

文章中的跨日事件 (`RaiseDayPassEvent`) 不是這一輪的必要需求。若未來真的要做 scheduler / recurring job，再另外設計。

## 之後建議的實作順序

1. 新增 `TimeOptions`
2. 新增 `ShiftedTimeProvider`
3. DI 註冊 `TimeProvider`
4. 先替換 `.Core`
5. 再替換 API / AppleBTS.API / seed / console
6. 補檢查腳本或 architecture test，禁止直接讀 `DateTime.Now`

## 參考來源

- [TimeProvider Class](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider?view=net-10.0)
- [Testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing)
- [從 DateTime 的 Mock 技巧談 PoC 的應用](https://columns.chicken-house.net/2022/05/29/datetime-mock/)

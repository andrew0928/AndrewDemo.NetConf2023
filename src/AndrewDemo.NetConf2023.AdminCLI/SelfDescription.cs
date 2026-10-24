using System.Reflection;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal static class SelfDescription
{
    public static readonly string[] Topics = ["configure", "query-stats", "query-details", "export", "tip-verify", "troubleshoot"];
    public static string? Read(string[] args)
    {
        if (args.SequenceEqual(["--version"])) return "shop-admin " + typeof(SelfDescription).Assembly.GetName().Version + "\n";
        if (args.SequenceEqual(["--help"]) || args.Length == 2 && args[0] is "stats" or "list" && args[1] == "--help") return Help;
        if (args.Length is 1 or 2 && args[0] == "--skill")
        {
            var topic = args.Length == 1 ? "index" : args[1];
            if (args.Length == 2 && !Topics.Contains(topic))
                throw new CliException(2, "未知 topic；可用 ID：" + string.Join(", ", Topics));
            using var stream = typeof(SelfDescription).Assembly.GetManifestResourceStream($"skills.{topic}.md")
                ?? throw new InvalidOperationException("缺少內嵌指引。");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        return null;
    }

    private const string Help = """
        shop-admin：唯讀管理查核（會員、結帳、訂單）；不產生交易。
        用法：
          shop-admin stats --source <member|checkout|order> [選項]
          shop-admin list  --source <member|checkout|order> [選項]
          shop-admin [--help | --version | --skill [topic-id]]
          shop-admin <stats|list> --help
        查詢：
          --type all|normal|test       預設 normal；all 合併正式與測試
          --member-id 正整數           三種來源皆可用
          --name 名稱                  僅 member；精確名稱，不將數字名稱當 ID
          --from / --to ISO8601        訂單／結帳；秒與時區必填，含起點、不含終點
                                      會員不接受日期；雙側須 from < to
          --state all|unknown|created|completed|cancelled  僅 checkout，預設 all
          --time-field createdAt|closedAt  僅 checkout，預設 createdAt
        分頁（僅 list）：
          --page 正整數                預設 1
          --page-size 1..100           預設 50
          --all-pages                  讀完全部頁；不可搭配 --page，非原子快照
        輸出：
          --format txt|csv|json        預設 txt；JSON 保留完整查詢證據
          --output 路徑                預設 stdout；父目錄須存在，不覆寫既有檔案
                                      副檔名 .txt/.csv/.json 須符合 format
        組態：
          SHOP_ADMIN_BASE_URL          預設 http://localhost:5128，遠端須 HTTPS
          SHOP_ADMIN_ACCESS_TOKEN      必填 admin_ 加小寫英數字；不接受 token 參數
          --env-file 路徑              預設 working directory 的 .env；缺預設檔可略過
                                      明確指定的檔案必須可讀；程序環境 > dotenv > 預設
        範例：
          shop-admin stats --source order --type normal
          shop-admin list --source member --type all --name test
          shop-admin list --source order --type test --all-pages --format csv --output test-orders.csv
          shop-admin stats --source order --from 2026-10-01T00:00:00+08:00 --to 2026-11-01T00:00:00+08:00
        訂單統計為保存金額，非實收；未知金額不當成 0。stats 使用 API 全部符合條件統計。
        CSV 使用 BOM／CRLF，文字公式前置單引號保護；無損原值與分頁時間證據請用 JSON。
        --help／--version／--skill 離線可用，不讀 .env；不可混用查詢／輸出選項。
        Agent 指引：--skill 列索引；--skill configure|query-stats|query-details|export|tip-verify|troubleshoot
        退出碼：0 成功、2 用法、3 組態、4 傳輸／30 秒逾時、5 HTTP、6 回應／跨頁漂移、7 輸出、130 取消。
        成功查詢不代表所有 TiP 驗收條件通過。

        """;
}

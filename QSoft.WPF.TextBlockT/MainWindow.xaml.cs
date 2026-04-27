using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QSoft.WPF.TextBlockT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            this.DataContext = new MainUI
            {
                TotalSums =
                [
                    new() {
                        Sum = "C# 型別選擇指南",
                        Items =
                        [
                            "class 是參考型別，實例配置於受 GC 管理的 Heap 記憶體中。它支援繼承、多型與介面實作，適合具有可變狀態、複雜行為或需要共享參考語意的物件模型，例如服務類別、ViewMode 或領域實體。",
                            "struct 是實值型別，通常配置於 Stack 上，賦值時會進行完整的位元複製。它不支援繼承，適合封裝小型且語意上不可分割的資料，例如 Point、Color 或 DateTime，能有效避免 Heap 配置與 GC 壓力。",
                            "record 是 C# 9 引入的型別，預設具備不可變性（immutable），編譯器會自動合成結構相等性比較（Equals、==）、GetHashCode、ToString 以及非破壞性複製的 with 運算式，非常適合用於資料傳輸物件（DTO）、事件酬載或任何以值語意為主的資料容器。"
                        ]
                    },
                    new() {
                        Sum = "訂單總覽",
                        Items =
                        [
                            "訂單編號：ORD-20240001，客戶：王小明，下單日期：2024/04/01，商品：無線滑鼠×2、機械鍵盤×1，小計金額：$3,200，付款方式：信用卡，目前狀態：已出貨，預計到貨日為 2024/04/05，請客戶留意簡訊通知。",
                            "訂單編號：ORD-20240002，客戶：李美華，下單日期：2024/04/02，商品：27吋螢幕×1，小計金額：$8,500，付款方式：ATM 轉帳，目前狀態：待確認付款，系統將於收款後 24 小時內安排出貨，如有疑問請聯繫客服。",
                            "訂單編號：ORD-20240003，客戶：陳大偉，下單日期：2024/04/03，商品：USB 集線器×3、Type-C 線×5，小計金額：$1,250，付款方式：超商代碼，目前狀態：處理中，預計於 2024/04/06 完成包裝並交付物流。",
                        ]
                    },
                    new() {
                        Sum = "庫存警示",
                        Items =
                        [
                            "商品名稱：無線滑鼠 MX300（料號：MS-MX300-BK），目前剩餘庫存：3 件，安全庫存設定為 10 件，已觸發低庫存警示。建議儘速向供應商送出補貨申請，預估前置作業時間約 5 個工作天，請採購部門優先處理。",
                            "商品名稱：機械鍵盤 KB-500（料號：KB-500-TKL），目前剩餘庫存：0 件，已完全缺貨。近 7 日平均出貨量為 4 件/天，缺貨損失風險極高，請立即聯繫原廠確認最快到貨時程，並評估是否需要臨時調貨支應。",
                        ]
                    },
                    new() {
                        Sum = "系統公告",
                        Items =
                        [
                            "系統維護公告：本系統將於 2024/05/01（三）凌晨 02:00 至 04:00 進行例行性基礎架構維護作業，維護期間所有功能暫停服務。請各單位於前一日完成重要作業存檔，避免資料遺失，如有緊急需求請聯絡資訊部值班人員。",
                            "功能上線通知：新版報表模組 v2.3 已正式上線，新增支援 Excel 批次匯出、動態圖表顯示及跨期間比較分析等功能。請各部門管理員登入後台，至「系統設定 → 模組管理」頁面啟用新功能，首次使用建議參閱線上操作手冊。",
                            "教育訓練報名截止提醒：本季員工數位技能提升課程報名截止日為 2024/04/30 下午 17:00，課程涵蓋 AI 工具應用、資料分析基礎及資安意識三大主題。請各部門主管確認所屬成員報名狀況，未達報名人數門檻之場次將予以取消。",
                            "資安政策更新公告：依據公司最新資訊安全規範（版本 3.1），即日起所有系統帳號密碼須符合以下條件：長度不得少於 12 個字元、須包含大寫英文、小寫英文、數字及特殊符號各至少一個，且每 90 天強制更換一次，舊密碼不得重複使用。",
                        ]
                    },
                ]
            };
         }
    }

    public class MainUI
    {
        public ObservableCollection<TotalSum> TotalSums { set; get; } = [];
    }

    public class TotalSum
    {
        public string Sum { set; get; }
        public List<string> Items { set; get; }
    }
}
# CFG、Strong Naming 與 SafeSEH 判斷流程

## 1. 先確認檢查對象

CFG 是 Windows PE native image 的控制流程保護。判斷時應檢查最後要部署的 `.exe` 或 native `.dll`，不要只檢查 C# 編譯產生的 managed `.dll`。

- 一般 .NET managed DLL 主要包含 IL，不是 CFG 的主要檢查對象。
- NativeAOT 的輸出是 native PE，應檢查 publish 目錄中的最終 `.exe` 或 `.dll`。
- 應用程式載入的其他 native DLL 也要逐一檢查；只檢查主程式不足以代表整個程序的所有模組都受 CFG 保護。

## 2. 確認 NativeAOT 建置設定

`ConsoleApp_CFG.csproj` 應包含：

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <ControlFlowGuard>Guard</ControlFlowGuard>
</PropertyGroup>
```

修改設定後，重新 publish，避免檢查到舊的輸出檔：

```powershell
dotnet publish .\ConsoleApp_CFG.csproj -c Release -r win-x64
```

本範例的檢查檔案為：

```text
.\ConsoleApp_CFG\bin\Release\net10.0\publish\win-x64\ConsoleApp_CFG.exe
```

## 3. 使用 `dumpbin` 檢查 PE metadata

請在 **Visual Studio Developer PowerShell** 執行：

```powershell
dumpbin /headers /loadconfig `
  .\ConsoleApp_CFG\bin\Release\net10.0\publish\win-x64\ConsoleApp_CFG.exe
```

也可以直接指定 `dumpbin.exe` 的完整路徑。`/headers` 檢查 PE header，`/loadconfig` 檢查 CFG 的 Load Configuration metadata。

## 4. 判斷必要欄位

完整 CFG-enabled image 至少應看到以下資訊：

```text
DLL characteristics
    Control Flow Guard

Guard Flags
    CF instrumented
    FID table present

Guard CF function table
Guard CF function count
```

另外應看到：

```text
Dynamic base
NX compatible
```

其中 `Guard CF function count` 應為非零值。`CF instrumented` 單獨出現並不足以判定 CFG 完整啟用；若缺少 `Control Flow Guard` 或 `FID table present`，應判定為不完整。

## 5. 判定結果

| 檢查結果 | 判定 |
| --- | --- |
| 有 `Control Flow Guard`、`CF instrumented`、`FID table present`，且 function count 非零 | CFG 已啟用 |
| 只有 `CF instrumented`，但缺少 `Control Flow Guard` 或 `FID table present` | CFG metadata 不完整，不通過 |
| 沒有 CFG 相關 Load Configuration metadata | 不支援或未啟用 CFG |

本範例目前的 `ConsoleApp_CFG.exe` 輸出包含：

```text
Control Flow Guard
CF instrumented
FID table present
Guard CF function count: 8F5
```

因此目前判定為 **CFG 已啟用**。

## 6. 檢查執行中的程序設定

PE metadata 是靜態檢查；若要確認 Windows 對執行中程序套用的 mitigation policy，可使用：

```powershell
Get-ProcessMitigation -Name "C:\path\ConsoleApp_CFG.exe"
Get-ProcessMitigation -Name "C:\path\ConsoleApp_CFG.exe" -RunningProcesses
```

重點查看：

- `EnableControlFlowGuard`：程序是否啟用 CFG。
- `StrictMode`：載入的 DLL 是否也必須全部支援 CFG。

`StrictMode` 開啟時，任何不具 CFG metadata 的 executable DLL 都可能無法載入，因此部署時應檢查主程式及所有 native DLL。

## 7. C/C++ native 專案的對照設定

若檢查的是 C/C++ native 專案，需同時在 compiler 與 linker 啟用：

```text
/guard:cf
/GUARD:CF
/DYNAMICBASE
```

C# 一般 managed 專案不能直接以 C/C++ 的 `/guard:cf` 取代 NativeAOT 設定；NativeAOT 應使用 `<ControlFlowGuard>Guard</ControlFlowGuard>`。

## 8. Strong Naming 的適用範圍

Strong Naming 是 managed assembly 的身分識別機制，主要由 assembly name、version、public key、public key token 與簽章組成。它不是 Windows 發行者憑證，也不應作為 .NET 10 的防竄改或信任邊界。

### 8.1 哪些檔案需要檢查

| 檔案或專案 | Strong Naming 是否有意義 |
| --- | --- |
| 一般 .NET 10 managed `.dll` | 可以檢查，但通常不是必要的安全措施 |
| .NET Framework `net472` library | 有意義；可能涉及 GAC、強名稱相依與版本並存 |
| 同時支援 `net472` 與現代 .NET 的 library | 若要支援強名稱相依，所有 target framework 應使用同一把 key |
| NativeAOT 最終 native `.exe/.dll` | 不應以 Strong Name 判定；應使用 Authenticode |

`ConsoleApp_CFG` 的 NativeAOT 最終輸出是 native PE，不是要用 `sn.exe` 驗證的 managed assembly。若要檢查其發行者與檔案完整性，應使用 Authenticode；若要檢查 AOT 前的 managed assembly，才檢查該 assembly。

### 8.2 建置設定

SDK-style C# project 可使用：

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>keys\QSoft.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

也可以在 Visual Studio 中設定：

```text
Project Properties
→ Build
→ Strong naming
→ Sign the assembly
```

同一個 library 的所有 target framework 應使用同一把 Strong Name key。不要在每次建置時重新產生 `.snk`，否則 `PublicKeyToken` 會改變，可能破壞既有相依性、GAC 安裝與 assembly identity。

### 8.3 Strong Name 驗證

請在 **Visual Studio Developer PowerShell** 執行：

```powershell
# 驗證 Strong Name 簽章
sn -vf .\bin\Release\net8.0\MyLibrary.dll

# 查看 Public Key Token；此命令只查看 token，不驗證簽章
sn -T .\bin\Release\net8.0\MyLibrary.dll
```

判定方式：

- `sn -vf` 顯示 assembly valid：Strong Name 簽章有效。
- 顯示 not a strongly named assembly：檔案沒有 Strong Name。
- 驗證失敗：檔案可能被修改、簽章無效或只是 delay-signed。
- `sn -T` 的 token 應與預期的固定 token 一致，但不能取代 `sn -vf`。

程式內可用 `AssemblyName` 檢查 metadata 是否有 public key，但這只能判定是否包含 Strong Name identity，不能取代簽章驗證：

```csharp
using System.Reflection;

var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
var hasStrongName =
    assemblyName.GetPublicKey() is { Length: > 0 } &&
    assemblyName.GetPublicKeyToken() is { Length: > 0 };
```

### 8.4 CI/CD 使用方式

`.snk` 通常在 Release pipeline 中被取用，但 key pair 應只產生一次並固定保存：

1. 產生並保留固定的 key pair。
2. 將私鑰放在 CI/CD Secret Store、Secure File 或受控的 key storage。
3. 建置時暫時提供給 MSBuild，不要將私鑰輸出到 log。
4. 建置後使用 `sn -vf` 驗證，並檢查 `PublicKeyToken` 是否符合預期。
5. 完成後清理 workspace 中的暫存私鑰。

例如：

```powershell
dotnet build .\QSoft.WPF.TreeListView\QSoft.WPF.TreeListView.csproj `
  -c Release `
  -p:SignAssembly=true `
  -p:AssemblyOriginatorKeyFile="$env:SNK_PATH"

sn -vf .\QSoft.WPF.TreeListView\bin\Release\net8.0\QSoft.WPF.TreeListView.dll
```

Strong Name 私鑰與 Authenticode 憑證不是同一種金鑰：

- `.snk`：managed assembly identity。
- Authenticode certificate：Windows 檔案發行者與信任鏈。
- NuGet package signing certificate：NuGet 套件來源與完整性。

若目標是驗證 NativeAOT `.exe` 的發行者與檔案簽章，使用：

```powershell
Get-AuthenticodeSignature .\ConsoleApp_CFG.exe
signtool verify /pa /all /v .\ConsoleApp_CFG.exe
```

### 8.5 Authenticode 簽署與驗證

Authenticode 不需要修改 C# 程式碼或 `.csproj`，而是在 `dotnet publish` 完成後，對最後要交付的 PE 檔案簽署。`signtool.exe` 隨 Visual Studio 或 Windows SDK 提供，應在 **Developer PowerShell** 中執行。

正式環境應使用具有 **Code Signing** 用途的企業或公開 CA 憑證。私鑰應放在 CI/CD Secret Store、憑證存放區、HSM 或遠端簽署服務，不應提交到 repository。

使用目前使用者憑證存放區中的憑證：

```powershell
signtool sign `
  /sha1 $env:SIGN_CERT_THUMBPRINT `
  /fd SHA256 `
  /tr $env:TIMESTAMP_URL `
  /td SHA256 `
  /d "Company Application" `
  .\bin\Release\net10.0-windows\win-x64\publish\MyApp.exe
```

如果 CI/CD 使用受保護的 `.pfx`，可使用：

```powershell
signtool sign `
  /f .\signing\company-code-signing.pfx `
  /p $env:PFX_PASSWORD `
  /fd SHA256 `
  /tr $env:TIMESTAMP_URL `
  /td SHA256 `
  .\publish\MyApp.exe
```

`/fd SHA256` 是檔案簽章摘要演算法，`/tr` 是 RFC 3161 timestamp server，`/td SHA256` 是 timestamp 摘要演算法。應使用 timestamp，否則簽署憑證過期後，簽章可能無法通過長期驗證。

簽署時機與對象：

1. 先完成最後一次 build/publish。
2. 若要修改、壓縮或替換 PE 檔案，必須在修改完成後再簽署。
3. 簽署自己發布且可控制的 `.exe`、managed `.dll` 或 native `.dll`。
4. 不要重新簽署 Microsoft 或第三方已簽署的 runtime/dependency；應保留並驗證其原始簽章。
5. 如果使用 MSIX 或其他 installer/package，除了檔案簽章外，還要依該格式簽署 package 本身。

簽署後應同時使用 SignTool 與 PowerShell 驗證：

```powershell
signtool verify /pa /all /v .\publish\MyApp.exe
Get-AuthenticodeSignature .\publish\MyApp.exe
```

驗收時確認：

- `signtool` exit code 為 `0`。
- `Get-AuthenticodeSignature` 的 `Status` 為 `Valid`。
- `SignerCertificate` 的 Subject、憑證鏈與有效用途符合企業政策。
- Timestamp certificate 存在且 timestamp 驗證成功。

開發測試可使用 self-signed certificate，但必須先在測試電腦建立信任；self-signed certificate 不可視為正式產品的發行者信任。

## 9. SafeSEH 的適用範圍

SafeSEH 是 x86 native PE 的 Structured Exception Handling 保護。`/SAFESEH` 只能用於 x86；x64、ARM 與 ARM64 使用 PDATA/XDATA 記錄例外處理與 unwind 資訊，不應因為沒有 SafeSEH 就判定為不安全。

### 9.1 C#/.NET 10 的判定原則

| 檔案或平台 | SafeSEH 是否有意義 |
| --- | --- |
| 一般 C# managed `.dll/.exe` | 沒有意義；不檢查 C# `try/catch` 或 managed exception |
| NativeAOT `win-x64` | 不適用；檢查 PDATA/XDATA、CFG 與 CET |
| NativeAOT `win-arm64` | 不適用；檢查平台的 unwind metadata、CFG 與 CET |
| NativeAOT `win-x86` | 有意義；可檢查最終 native PE 的 SafeSEH table |
| P/Invoke 載入的 x86 native DLL | 有意義；應逐一檢查 |

SafeSEH 不是 CFG 的替代品，也不是 `<ControlFlowGuard>Guard</ControlFlowGuard>` 的必要條件。兩者保護的控制流程類型不同：

- SafeSEH：限制 x86 SEH handler 必須位於合法 handler table。
- CFG：限制間接呼叫只能跳至合法的 function target。

### 9.2 SafeSEH 驗證

針對 x86 NativeAOT 或其他 x86 native PE：

```powershell
dumpbin /loadconfig .\path\MyNativeX86.exe |
  Select-String "Safe Exception Handler"
```

完整輸出通常包含：

```text
Safe Exception Handler Table
Safe Exception Handler Count
```

SafeSEH 是 native linker 產生的資訊；C# managed 專案沒有通用的 `/SAFESEH` 設定。對 .NET 10 NativeAOT，應依 Runtime Identifier 判斷是否需要檢查，而不是對所有 `.NET` 輸出一律要求 SafeSEH。

## 10. 三種機制的驗收矩陣

| 檢查對象 | CFG | Strong Naming | SafeSEH | 發行者/檔案完整性 |
| --- | --- | --- | --- | --- |
| 一般 managed `.dll` | 不是主要判定對象 | 需要時使用 `sn -vf` | 不適用 | Authenticode（如有需求） |
| `net472` managed library | 不是主要判定對象 | 若有強名稱需求，使用固定 key 並驗證 | 不適用 | Authenticode（如有需求） |
| NativeAOT `win-x64` `.exe` | `dumpbin /headers /loadconfig` | 不適用於最終 native image | 不適用 | `signtool` / Authenticode |
| NativeAOT `win-x86` `.exe` | `dumpbin /headers /loadconfig` | 不適用於最終 native image | 檢查 SafeSEH table | `signtool` / Authenticode |
| P/Invoke native DLL | 依架構檢查 | 若是 managed assembly 才檢查 | x86 時檢查 | `signtool` / Authenticode |

## 11. 建議的完整驗收流程

1. 先確認檔案類型：managed assembly、NativeAOT native PE 或 P/Invoke native DLL。
2. 重新 build/publish，避免檢查到舊的 `bin`/`publish` 輸出。
3. 對 NativeAOT/native PE 使用 `dumpbin` 檢查 CFG；所有載入的 native DLL 也要檢查。
4. 只有 x86 native image 才將 SafeSEH 納入驗收；x64/ARM64 改確認 PDATA/XDATA。
5. 對 managed assembly 在確有需求時使用 `sn -vf`，並核對固定的 `PublicKeyToken`。
6. 在最後一次 publish 後，對自己發布的最終 `.exe/.dll` 使用 Authenticode 簽署。
7. 使用 `signtool verify /pa /all /v` 與 `Get-AuthenticodeSignature` 驗證簽署者、憑證鏈、timestamp 與檔案完整性。
8. 若使用 Windows Exploit Protection，另以 `Get-ProcessMitigation` 檢查程序層級 CFG 與 `StrictMode`。

## 12. 重要注意事項

- CFG、Strong Name、SafeSEH 解決的是不同問題，不能互相替代。
- `CF instrumented` 單獨存在不足以判定完整 CFG；仍需確認 `Control Flow Guard`、`FID table present` 與 function table。
- Strong Name 不等於 Authenticode，不能用來證明發行者身份。
- .NET 10 runtime 不應以 Strong Name 作為安全邊界；Strong Name 主要用於 assembly identity 與 .NET Framework 相容性。
- 不要在 CI/CD 每次重新產生 `.snk`，也不要任意更換既有 library 的 Strong Name key。
- `StrictMode` 可能要求所有載入的 native DLL 都具備 CFG；只驗證主程式不夠。
- 驗證應針對最後要部署的檔案，而不是只檢查中間產物或 managed input assembly。

## 13. Microsoft 參考文件

- [Control Flow Guard for platform security](https://learn.microsoft.com/windows/win32/secbp/control-flow-guard)
- [Native AOT security features](https://learn.microsoft.com/dotnet/core/deploying/native-aot/security)
- [Strong naming](https://learn.microsoft.com/dotnet/standard/library-guidance/strong-naming)
- [Sn.exe (Strong Name Tool)](https://learn.microsoft.com/dotnet/framework/tools/sn-exe-strong-name-tool)
- [`/SAFESEH` (Image has Safe Exception Handlers)](https://learn.microsoft.com/cpp/build/reference/safeseh-image-has-safe-exception-handlers?view=msvc-170)
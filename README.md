# AngArch-CAD-Addins

AutoCAD .NET 增益集工具，專為 AngArch 建築工作流程設計。

---

## REVEXPORT — 修改雲線漏標稽核匯出

掃描模型空間中指定圖層的修改雲線、版次三角形及文字標註，
逐筆匯出至 `桌面\RevisionExport.xlsx`，若雲線缺少三角形或說明文字則自動標記（漏標稽核）。

### 環境需求

| 項目 | 版本 |
|------|------|
| Windows | 10 / 11 |
| Visual Studio | 2022（任意版本） |
| .NET Framework | 4.8（Win 10+ 已內建） |
| AutoCAD | 2023 或以上（需使用 .NET Framework 模式） |
| NuGet | 建置時自動還原 |

### 建置步驟

1. Clone 此儲存庫
2. 以 Visual Studio 2022 開啟 `AngArch-CAD-Addins.sln`
3. 若 AutoCAD 未安裝於 `C:\Program Files\Autodesk\AutoCAD 2023`，
   請在 `Directory.Build.props` 旁新增 `Directory.Build.props.user`：
   ```xml
   <Project>
     <PropertyGroup>
       <AcadLibPath>C:\Program Files\Autodesk\AutoCAD 2026</AcadLibPath>
     </PropertyGroup>
   </Project>
   ```
   或直接修改 `Directory.Build.props`（請勿 commit 此變更）。
4. 按 `Ctrl+Shift+B` 建置，應無任何錯誤
5. DLL 輸出於 `src\bin\Debug\AngArch-CAD-Addins.dll`

### 載入 AutoCAD

```
指令: NETLOAD
```
瀏覽至 `src\bin\Debug\AngArch-CAD-Addins.dll` 並按開啟。

若要每次啟動 AutoCAD 時自動載入，可於 `OPTIONS` → 檔案 → 支援檔案搜尋路徑 新增啟動項目，
或使用啟動 LISP 檔案。

### 執行 REVEXPORT

1. 開啟含有修改雲線、版次三角形及文字標註的圖面（例如圖層名稱為 `REV`）
2. 輸入：
   ```
   指令: REVEXPORT
   Enter layer name to scan: REV
   ```
3. 開啟桌面上的 `RevisionExport.xlsx`

### Excel 欄位說明

| 欄位 | 說明 |
|------|------|
| No./編號 | 序號 |
| Cloud X/雲線X · Cloud Y/雲線Y | 修改雲線中心座標（圖面單位） |
| Revision/版次 | 最靠近版次三角形的文字內容 |
| Description/說明 | 最靠近雲線中心的說明文字（已排除版次文字） |
| Has △/有△ | Yes / No — 是否有對應的版次三角形 |
| Status/狀態 | `OK` · `漏△`（缺三角形）· `漏字`（缺說明）· `漏△漏字` |

狀態非 `OK` 的列會以黃色底色標示，方便快速找出漏標項目。

---

## 常見問題

**NETLOAD 成功但找不到 `REVEXPORT` 指令**
- 確認建置成功且 DLL 檔案大小不為 0
- 確認 `RevCloudExport.cs` 中有 `[assembly: ExtensionApplication(null)]`
- 再執行一次 NETLOAD；AutoCAD 第一次載入有時需要兩次

**建置錯誤：找不到 `accoremgd.dll`**
- 修改 `Directory.Build.props` 中的 `AcadLibPath`，指向你的 AutoCAD 安裝資料夾

**所有雲線都顯示 `漏△`**
- AutoCAD 的 `REVCLOUD` 在**物件**模式下轉換矩形或多邊形時，可能產生 `Polyline2d` 而非輕量型 `Polyline`。
  請改用**徒手**或**矩形**模式繪製雲線，或先對現有雲線執行 `CONVERTPOLY`。

**MText 說明欄位出現 RTF 格式碼（如 `{\fArial;text}`）**
- v1 建議使用單行文字（`TEXT` 指令）作為說明標註。
  MText 去除格式碼的功能將於後續版本加入。

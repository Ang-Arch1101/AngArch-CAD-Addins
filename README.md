# AngArch-CAD-Addins

AutoCAD .NET add-in tools for AngArch architectural workflows.

---

## REVEXPORT — Revision Cloud Audit Export

Scans a layer in Model Space for revision clouds, triangle version markers, and text labels.
Exports a row per cloud to `Desktop\RevisionExport.xlsx`, flagging clouds where the triangle
or description text is missing (漏標 audit).

### Prerequisites

| Requirement | Version |
|-------------|---------|
| Windows | 10 / 11 |
| Visual Studio | 2022 (any edition) |
| .NET Framework | 4.8 (pre-installed on Win 10+) |
| AutoCAD | 2023 (or later with .NET Framework mode) |
| NuGet | auto-restored on build |

### Build

1. Clone the repo
2. Open `AngArch-CAD-Addins.sln` in Visual Studio 2022
3. If your AutoCAD is not installed at `C:\Program Files\Autodesk\AutoCAD 2023`,
   create a `Directory.Build.props.user` file next to `Directory.Build.props` with:
   ```xml
   <Project>
     <PropertyGroup>
       <AcadLibPath>C:\Program Files\Autodesk\AutoCAD 2026</AcadLibPath>
     </PropertyGroup>
   </Project>
   ```
   Or just edit `Directory.Build.props` directly (don't commit that change).
4. Press `Ctrl+Shift+B` to build — zero errors expected
5. DLL is at `src\bin\Debug\AngArch-CAD-Addins.dll`

### Load in AutoCAD

```
Command: NETLOAD
```
Browse to `src\bin\Debug\AngArch-CAD-Addins.dll` and click Open.

To load automatically on every session, add a startup entry via the AutoCAD `OPTIONS` dialog →
Files → Support File Search Path, or use a startup LISP file.

### Run REVEXPORT

1. Open a drawing that has revision clouds, triangles, and text on a dedicated layer (e.g. `REV`)
2. Type:
   ```
   Command: REVEXPORT
   Enter layer name to scan: REV
   ```
3. Open `Desktop\RevisionExport.xlsx`

### Excel Output

| Column | Description |
|--------|-------------|
| No. | Sequential row number |
| Cloud X / Y | Centroid of the revision cloud (drawing units) |
| Revision | Text found nearest to the matched triangle |
| Description | Text found nearest to the cloud centroid (excluding Revision text) |
| Has △ | Yes / No — whether a triangle was matched |
| Status | `OK` · `漏△` (missing triangle) · `漏字` (missing description) · `漏△漏字` |

Rows with a status other than `OK` are highlighted yellow for quick review.

---

## Troubleshooting

**NETLOAD accepts the DLL but `REVEXPORT` command is not found**
- Confirm the build succeeded and the DLL is not 0 bytes
- Check that `[assembly: ExtensionApplication(null)]` is present in `RevCloudExport.cs`
- Run `NETLOAD` again; AutoCAD sometimes needs a second load on the first session

**Build error: cannot find `accoremgd.dll`**
- Set `AcadLibPath` in `Directory.Build.props` to your AutoCAD install folder

**All clouds show `漏△`**
- AutoCAD's `REVCLOUD` command on **object** mode converts a rectangle/polygon — those may use
  `Polyline2d` instead of lightweight `Polyline`. Use `REVCLOUD` in **freehand** or **rectangular**
  mode to ensure lightweight `Polyline` output, or run `CONVERTPOLY` on existing clouds first.

**MText description contains RTF codes (e.g. `{\fArial;text}`)**
- Use `DBText` (single-line `TEXT` command) for description labels in v1.
  MText stripping will be added in a future update.

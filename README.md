# Rons Place Patch Tool / Rons Place 补丁工具

**English** | [中文](#中文说明)

A tiny portable Windows tool that removes the **row save limit** and disables the **automatic update popup** for two Rons Place Software products:

- **Ron's Editor** — removes the 1000-row CSV save limit of the free/Lite version.
- **Ron's Data Edit** — removes the 2500-row save limit of the free/Lite version.

> ⚠️ Educational / research purposes only. Do not use this tool to violate the software's license agreement. You must own a legitimate copy of the software.

---

## English

### Supported products

| Product | Detect by | Row limit removed | Update popup disabled |
|---------|-----------|-------------------|----------------------|
| Ron's Editor (2020.7.23.1031) | `Editor.WinGUI.exe` | 1000 -> 2147483647 | yes |
| Ron's Data Edit (2026.9.x) | `DataEdit.WPFGUI.exe` | 2500 -> 2147483647 | yes |

### What it patches

**Ron's Editor**
| Patch | File |
|-------|------|
| `Form_Main.Lite_IsOverRowLimit`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` |
| `Form_Main.Lite_UpdateEvaluationWarning`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` |
| `Interactions.ProcessOnlineVersion`: replaced with immediate return | `RonsPlace.ApplicationCore.Forms.dll` |
| `VersionCheckEnabled = False` | `Editor.WinGUI.settings` |

**Ron's Data Edit**
| Patch | File |
|-------|------|
| `DataEditLicenseService.IsOverRowLimit`: constant `2500` -> `int.MaxValue` | `DataEdit.Engine.dll` |
| `AppOnlineInteractiveService.NewVersionStartCheck`: immediate return | `RonsPlace.ApplicationCore.WPF.dll` |
| `AppOnlineInteractiveService.ProcessOnlineVersion`: immediate return | `RonsPlace.ApplicationCore.WPF.dll` |
| `AppUpdateService.PeriodCheck`: immediate return | `RonsPlace.ApplicationCore.WPF.dll` |

- **Automatic backup**: every file is copied to `<file>.bak` before modification (only the earliest backup is kept).
- **Manual "Check for Updates"** menu remains fully functional in both products.
- **Signature + reflection locating** — if a target is not found (different version), that patch is skipped safely and reported.

### Usage

1. Install the product on the target PC and **close it**.
2. Double-click `RonEditorPatch.exe` — UAC will ask for Administrator rights, click **Yes**.
3. Select the product:
   - Press `1` for Ron's Editor
   - Press `2` for Ron's Data Edit
   (the install folder is located automatically; if not, you can type it)
4. Press `1` and Enter to install the patch.
5. Confirm you see `[PATCHED]` for each item.

### Command line mode

```
RonEditorPatch.exe install --product editor
RonEditorPatch.exe install --product dataedit
RonEditorPatch.exe restore --product dataedit
RonEditorPatch.exe status  --product editor
RonEditorPatch.exe install --dir "F:\Rons Data Edit"
RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe status  --dir "<install folder>"
```

`--product editor|dataedit` selects the product explicitly; `--dir <folder>` selects the install folder. The install folder is auto-detected from the registry and common install paths when omitted.

### Restore

Choose menu option `2` (Uninstall patch), or manually restore the `.bak` files.

### Files

```
├─ README.md                this file (bilingual)
├─ README.txt               Chinese guide (plain text)
├─ README_EN.txt            English guide (plain text)
├─ LICENSE                  MIT
├─ RonEditorPatch.exe       prebuilt binary (English UI)
└─ src/
   ├─ RonEditorPatch.cs     main source (C#, .NET Framework 4.x)
   ├─ DataEditSigs.cs       Data Edit signature constants
   └─ app.manifest          UAC requireAdministrator manifest
```

### Building from source

Requires the .NET Framework 4.x compiler (`csc.exe`, preinstalled on Windows):

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:RonEditorPatch.exe /target:exe /platform:anycpu /win32manifest:app.manifest src\RonEditorPatch.cs src\DataEditSigs.cs
```

---

## 中文说明

一个轻量便携的 Windows 补丁工具，用于解除 Rons Place Software 两款软件的**保存行数限制**并屏蔽**自动更新弹窗**：

- **Ron's Editor**：解除免费/Lite 版 CSV 文件最多保存 1000 行的限制。
- **Ron's Data Edit**：解除免费/Lite 版最多保存 2500 行的限制。

> ⚠️ 仅供学习研究使用。请勿利用本工具违反软件的许可协议，请拥有软件的合法副本。

### 支持的产品

| 产品 | 识别文件 | 解除的行数限制 | 屏蔽更新弹窗 |
|------|----------|----------------|--------------|
| Ron's Editor (2020.7.23.1031) | `Editor.WinGUI.exe` | 1000 → 2147483647 | 是 |
| Ron's Data Edit (2026.9.x) | `DataEdit.WPFGUI.exe` | 2500 → 2147483647 | 是 |

### 补丁内容

**Ron's Editor**
| 补丁 | 文件 |
|------|------|
| `Lite_IsOverRowLimit`：常量 `1000` → `int.MaxValue` | `Editor.WinGUI.exe` |
| `Lite_UpdateEvaluationWarning`：常量 `1000` → `int.MaxValue` | `Editor.WinGUI.exe` |
| `ProcessOnlineVersion`：改为直接返回 | `RonsPlace.ApplicationCore.Forms.dll` |
| `VersionCheckEnabled = False` | `Editor.WinGUI.settings` |

**Ron's Data Edit**
| 补丁 | 文件 |
|------|------|
| `IsOverRowLimit`：常量 `2500` → `int.MaxValue` | `DataEdit.Engine.dll` |
| `NewVersionStartCheck`：改为直接返回 | `RonsPlace.ApplicationCore.WPF.dll` |
| `ProcessOnlineVersion`：改为直接返回 | `RonsPlace.ApplicationCore.WPF.dll` |
| `PeriodCheck`：改为直接返回 | `RonsPlace.ApplicationCore.WPF.dll` |

- **自动备份**：修改前自动生成 `<文件名>.bak` 备份（只保留最早的原版备份）。
- 两款软件的**手动「检查更新」菜单均不受影响**，仍可正常使用。
- **特征码 + 反射双重定位**：若目标未找到（版本不同），会安全跳过并明确提示，不会损坏文件。

### 使用方法

1. 在目标电脑安装软件并**关闭**它。
2. 双击 `RonEditorPatch.exe`，UAC 弹窗点「是」（需要管理员权限）。
3. 选择要处理的软件：
   - 按 `1` = Ron's Editor
   - 按 `2` = Ron's Data Edit
   （安装目录自动查找，找不到时可手动输入）
4. 按 `1` 回车安装补丁。
5. 各项显示 `[PATCHED]` 即成功。

### 命令行模式

```
RonEditorPatch.exe install --product editor
RonEditorPatch.exe install --product dataedit
RonEditorPatch.exe restore --product dataedit
RonEditorPatch.exe status  --product editor
RonEditorPatch.exe install --dir "F:\Rons Data Edit"
RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe status  --dir "<安装目录>"
```

`--product editor|dataedit` 显式指定软件；`--dir <目录>` 指定安装目录。不指定时自动从注册表和常见路径检测。

### 恢复原版

菜单选 `2`（卸载补丁），或手动用 `.bak` 备份文件覆盖回去。

### 文件说明

```
├─ README.md                本文件（双语）
├─ README.txt               中文说明（纯文本）
├─ README_EN.txt            英文说明（纯文本）
├─ LICENSE                  MIT 协议
├─ RonEditorPatch.exe       编译好的工具（英文界面）
└─ src/
   ├─ RonEditorPatch.cs     主源码（C#，.NET Framework 4.x）
   ├─ DataEditSigs.cs       Data Edit 特征码常量
   └─ app.manifest          UAC 管理员权限清单
```

### 从源码编译

需要 .NET Framework 4.x 编译器（`csc.exe`，Windows 自带）：

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:RonEditorPatch.exe /target:exe /platform:anycpu /win32manifest:app.manifest src\RonEditorPatch.cs src\DataEditSigs.cs
```

---

## License / 许可

MIT — for educational and research purposes only. / 仅供学习研究使用。

# Ron's Editor Patch Tool / Ron's Editor 补丁工具

**English** | [中文](#中文说明)

A tiny portable Windows tool that removes the **1000-row CSV save limit** in the free/trial version of [Ron's Editor](https://www.ronsplace.ca/) and disables the automatic **update-check popup**.

> ⚠️ Educational / research purposes only. Do not use this tool to violate the software's license agreement. You must own a legitimate copy of the software.

---

## English

### What it does

| Patch | File | Effect |
|-------|------|--------|
| `Lite_IsOverRowLimit`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` | Save / Save As / Export are never disabled by the row count |
| `Lite_UpdateEvaluationWarning`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` | "Lite version can only save 1000 rows" warning never shows |
| `ProcessOnlineVersion`: replaced with immediate return | `RonsPlace.ApplicationCore.Forms.dll` | Automatic "new version available" popup is blocked |
| `VersionCheckEnabled = False` | `Editor.WinGUI.settings` | Automatic update check turned off |

- **Automatic backup**: every file is copied to `<file>.bak` before modification (only the earliest backup is kept).
- **Manual "Check for Updates"** menu remains fully functional.
- **Signature-based locating** (not fixed offsets) — if a signature is not found (e.g. different version), that patch is skipped safely and reported.

### Supported version

- Ron's Editor assembly version `2020.7.23.1031` (file version `2021.1.26.1742`).
- Other versions: the tool reports `signature not found` and skips the affected patch without damaging anything.

### Usage

1. Install Ron's Editor on the target PC and **close it**.
2. Double-click `RonEditorPatch.exe` — UAC will ask for Administrator rights, click **Yes**.
3. Press `1` and Enter to install the patch.
4. Confirm you see:

```
Editor.WinGUI.exe: [PATCHED] 1000-row limit removed (2 patch(es) found)
RonsPlace.ApplicationCore.Forms.dll: [PATCHED] update popup disabled
```

### Command line mode

```
RonEditorPatch.exe install --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe status  --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
```

The installation folder is auto-detected from the registry and common install paths; `--dir` is only needed if detection fails.

### Restore

Choose menu option `2` (Uninstall patch), or manually restore the `.bak` files.

### Files

```
├─ README.md                this file (bilingual)
├─ README.txt               Chinese guide (plain text)
├─ README_EN.txt            English guide (plain text)
├─ LICENSE                  MIT
├─ RonEditorPatch.exe       prebuilt binary (English UI)
└─ RonEditorPatch_github_nosrc.zip   all-in-one download package
```

---

## 中文说明

一个轻量便携的 Windows 补丁工具，用于解除 [Ron's Editor](https://www.ronsplace.ca/) 免费/试用版中 **CSV 文件最多保存 1000 行** 的限制，并屏蔽启动时自动弹出的**更新提示**。

> ⚠️ 仅供学习研究使用。请勿利用本工具违反软件的许可协议，请拥有软件的合法副本。

### 功能说明

| 补丁 | 文件 | 效果 |
|------|------|------|
| `Lite_IsOverRowLimit`：常量 `1000` → `int.MaxValue` | `Editor.WinGUI.exe` | 保存 / 另存为 / 导出不再因行数被禁用 |
| `Lite_UpdateEvaluationWarning`：常量 `1000` → `int.MaxValue` | `Editor.WinGUI.exe` | 「Lite 版只能保存 1000 行」警告不再显示 |
| `ProcessOnlineVersion`：改为直接返回 | `RonsPlace.ApplicationCore.Forms.dll` | 自动「发现新版本」弹窗被屏蔽 |
| `VersionCheckEnabled = False` | `Editor.WinGUI.settings` | 关闭自动更新检查 |

- **自动备份**：修改前自动生成 `<文件名>.bak` 备份（只保留最早的原版备份）。
- **手动「检查更新」菜单不受影响**，仍然可用。
- **基于特征码定位**（非固定偏移）：若特征码未找到（如版本不同），会安全跳过并明确提示，不会损坏文件。

### 支持版本

- Ron's Editor 程序集版本 `2020.7.23.1031`（文件版本 `2021.1.26.1742`）。
- 其他版本：工具会提示 `signature not found` 并安全跳过对应补丁。

### 使用方法

1. 在目标电脑安装 Ron's Editor 并**关闭**它。
2. 双击 `RonEditorPatch.exe`，UAC 弹窗点「是」（需要管理员权限）。
3. 按 `1` 回车安装补丁。
4. 看到以下内容即为成功：

```
Editor.WinGUI.exe: [PATCHED] 1000-row limit removed (2 patch(es) found)
RonsPlace.ApplicationCore.Forms.dll: [PATCHED] update popup disabled
```

### 命令行模式

```
RonEditorPatch.exe install --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
RonEditorPatch.exe status  --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
```

安装目录会自动从注册表和常见路径检测；只有检测失败时才需要 `--dir` 手动指定。

### 恢复原版

菜单选 `2`（卸载补丁），或手动用 `.bak` 备份文件覆盖回去。

### 文件说明

```
├─ README.md                本文件（双语）
├─ README.txt               中文说明（纯文本）
├─ README_EN.txt            英文说明（纯文本）
├─ LICENSE                  MIT 协议
├─ RonEditorPatch.exe       编译好的工具（英文界面）
└─ RonEditorPatch_github_nosrc.zip   一键下载打包
```

---

## License / 许可

MIT — for educational and research purposes only. / 仅供学习研究使用。

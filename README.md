# Ron's Editor Patch Tool

A tiny portable Windows tool that removes the **1000-row CSV save limit** in the free/trial version of [Ron's Editor](https://www.ronsplace.ca/) and disables the automatic **update-check popup**.

> ⚠️ Educational / research purposes only. Do not use this tool to violate the software's license agreement. You must own a legitimate copy of the software.

## What it does

| Patch | File | Effect |
|-------|------|--------|
| `Lite_IsOverRowLimit`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` | Save / Save As / Export are never disabled by the row count |
| `Lite_UpdateEvaluationWarning`: constant `1000` -> `int.MaxValue` | `Editor.WinGUI.exe` | "Lite version can only save 1000 rows" warning never shows |
| `ProcessOnlineVersion`: replaced with immediate return | `RonsPlace.ApplicationCore.Forms.dll` | Automatic "new version available" popup is blocked |
| `VersionCheckEnabled = False` | `Editor.WinGUI.settings` | Automatic update check turned off |

- **Automatic backup**: every file is copied to `<file>.bak` before modification (only the earliest backup is kept).
- **Manual "Check for Updates"** menu remains fully functional.
- **Signature-based locating** (not fixed offsets) — if a signature is not found (e.g. different version), that patch is skipped safely and reported.

## Supported version

- Ron's Editor assembly version `2020.7.23.1031` (file version `2021.1.26.1742`).
- Other versions: the tool reports `signature not found` and skips the affected patch without damaging anything.

## Usage

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

## Restore

Choose menu option `2` (Uninstall patch), or manually restore the `.bak` files.

## Files

```
├─ README.md            this file (English)
├─ README.txt           Chinese guide
├─ README_EN.txt        plain-text English guide
├─ LICENSE              MIT
└─ RonEditorPatch.exe   prebuilt binary (English UI)
```

## License

MIT — for educational and research purposes only.

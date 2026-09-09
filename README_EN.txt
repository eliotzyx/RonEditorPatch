============================================================
  Rons Place Patch Tool v2 - User Guide
  (Ron's Editor + Ron's Data Edit)
============================================================

[Supported products]
  - Ron's Editor (2020.7.23.1031): removes the 1000-row CSV
    save limit of the free/Lite version.
  - Ron's Data Edit (2026.9.x): removes the 2500-row save
    limit of the free/Lite version.
  Both: disables the automatic update-check popup.
  (The manual "Check for Updates" menu still works.)

[How to use]
  1. Install the product on the target PC and CLOSE it.
  2. Double-click RonEditorPatch.exe.
     - UAC will ask for Administrator rights - click "Yes".
  3. Select the product: 1 = Ron's Editor, 2 = Ron's Data Edit.
     (install folder is auto-detected; type it manually if asked)
  4. Press 1 (Install patch), then Enter.
  5. Confirm you see [PATCHED] for each item.

[Command line mode]
  RonEditorPatch.exe install --dir "<install folder>"
  RonEditorPatch.exe restore --dir "<install folder>"
  RonEditorPatch.exe status  --dir "<install folder>"

  Menu keys:
    1 = Install patch    2 = Uninstall patch (restore)
    3 = Check status     0 = Exit

[Auto-locate]
  The tool finds the install folder automatically from the
  Windows registry and common paths (Rons Place Apps /
  Rons Place Software). Use --dir only if detection fails.

[What it patches]
  Ron's Editor:
    - Editor.WinGUI.exe (2 constants: 1000 -> 2147483647)
    - RonsPlace.ApplicationCore.Forms.dll (update callback -> ret)
    - Editor.WinGUI.settings (VersionCheckEnabled = False)
  Ron's Data Edit:
    - DataEdit.Engine.dll (constant: 2500 -> 2147483647)
    - RonsPlace.ApplicationCore.WPF.dll (3 methods -> ret:
      NewVersionStartCheck / ProcessOnlineVersion / PeriodCheck)

[Backup & restore]
  - Every patched file is backed up first (same name + .bak).
  - To restore: choose 2 in the menu, or rename the .bak files
    back to their original names.

[Notes]
  - After patching, do NOT install a newer version via the
    app's "Check for Updates" - it would overwrite the patched
    files. If that happens, simply run this tool again.
  - Some antivirus software may flag the tool because it
    modifies program files; add it to the allow list if needed.
  - For educational/research purposes only.
============================================================


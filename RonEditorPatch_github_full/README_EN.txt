============================================================
  Ron's Editor Patch Tool - User Guide
  (Remove the 1000-row CSV limit + disable update popup)
============================================================

[Contents]
  RonEditorPatch.exe   - Main patch tool (single file, portable)

[Supported version]
  Ron's Editor 2020.7.23.1031 (file version 2021.1.26.1742)
  If another version is installed and a signature is not found,
  the tool safely skips that part and reports it - no damage.

[How to use]
  1. Install Ron's Editor on the target PC (any drive).
     IMPORTANT: close Ron's Editor before patching.
  2. Double-click RonEditorPatch.exe.
     - Windows UAC will ask for Administrator rights - click "Yes"
       (writing to Program Files requires admin).
  3. In the menu, press 1 (Install patch), then Enter.
  4. When you see:
       "[PATCHED] 1000-row limit removed"
       "[PATCHED] update popup disabled"
     the patch is complete.
  5. Open Ron's Editor and use it normally:
     - A single CSV file can now be saved with more than 1000 rows;
     - No more "A new version ... is available" popup at startup.

[Command line mode] (optional)
  RonEditorPatch.exe install --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
  RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
  RonEditorPatch.exe status  --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"

  Menu keys:
    1 = Install patch    2 = Uninstall patch (restore originals)
    3 = Check status     0 = Exit

[Auto-locate]
  The tool finds the Ron's Editor install folder automatically
  from the Windows registry and common install paths.
  If it cannot be found, use the --dir option or type the folder
  manually when prompted.

[What the patch does]
  1. Editor.WinGUI.exe (2 spots)
     - Lite_IsOverRowLimit(): the 1000-row check constant is
       changed to int.MaxValue, so Save / Save As / Export
       commands are never disabled by the row count.
     - Lite_UpdateEvaluationWarning(): the "can only save 1000
       rows" warning never appears.
  2. RonsPlace.ApplicationCore.Forms.dll (1 spot)
     - ProcessOnlineVersion(): the automatic update check
       callback now returns immediately - the "new version"
       popup can never show, even if the check runs.
     - The manual "Check for Updates" menu still works.
  3. Settings file Editor.WinGUI.settings
     - VersionCheckEnabled is set to False, turning off the
       automatic update check entirely.

[Backup & restore]
  - Every patched file is backed up first (same name + .bak;
    only the earliest backup is kept).
  - To restore: choose 2 in the menu, or rename the .bak files
    back to their original names.
  - Backup locations:
      C:\Program Files (x86)\Rons Place Apps\Rons Editor\Editor.WinGUI.exe.bak
      C:\Program Files (x86)\Rons Place Apps\Rons Editor\RonsPlace.ApplicationCore.Forms.dll.bak
      C:\ProgramData\Rons Place Apps\Rons Editor\Editor.WinGUI.settings.bak

[Notes]
  - After patching, do NOT install a newer version via the app's
    "Check for Updates" - it would overwrite the patched files.
    If that happens, simply run this tool again.
  - Some antivirus software may flag the tool because it modifies
    program files; add it to the allow list if needed.
  - For educational/research purposes only.
============================================================

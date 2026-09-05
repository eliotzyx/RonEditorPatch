using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

class RonEditorPatch
{
    // ---- Patch signatures (for Ron's Editor 2020.7.23.1031 / FileVersion 2021.1.26.1742) ----
    // Editor.WinGUI.exe: ldc.i4 1000 + cgt inside Lite_IsOverRowLimit()
    static readonly byte[] SIG_ROW_LIMIT = Hex("20E8030000FE02");
    // Editor.WinGUI.exe: ldc.i4 1000 + ble.s inside Lite_UpdateEvaluationWarning()
    static readonly byte[] SIG_WARN      = Hex("20E803000031");
    // RonsPlace.ApplicationCore.Forms.dll: full 36-byte IL of ProcessOnlineVersion()
    static readonly byte[] SIG_DLL_IL    = Hex("14FE066900000673820100060A0206188D14000001251602A2251703A26F16000006262A");
    // Replacement value: ldc.i4 2147483647 (int.MaxValue) = 20 FF FF FF 7F
    static readonly byte[] VAL_INTMAX    = Hex("20FFFFFF7F");
    const int DLL_IL_LEN = 36;

    static string AppDir;
    static string LogPath;
    static readonly List<string> Lines = new List<string>();

    static void Log(string s) { Lines.Add(s); Console.WriteLine(s); }

    static byte[] Hex(string h)
    {
        h = h.Replace(" ", "");
        byte[] b = new byte[h.Length / 2];
        for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
        return b;
    }

    static int FindBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    static int FindBytesFrom(byte[] haystack, byte[] needle, int from)
    {
        for (int i = from; i <= haystack.Length - needle.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    static int CountBytes(byte[] haystack, byte[] needle)
    {
        int n = 0, s = 0, i;
        while ((i = FindBytesFrom(haystack, needle, s)) >= 0) { n++; s = i + 1; }
        return n;
    }

    static bool IsPatchedConst(byte[] b, int off)
    {
        return off >= 0 && b.Length > off + 4 &&
               b[off] == 0x20 && b[off + 1] == 0xFF && b[off + 2] == 0xFF &&
               b[off + 3] == 0xFF && b[off + 4] == 0x7F;
    }

    static void Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RonEditorPatch.log");
        try { Console.Title = "Ron's Editor Patch Tool"; } catch { }

        string mode = "";
        string dirArg = null;
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i].Trim().ToLower();
            if (a == "install" || a == "restore" || a == "status") mode = a;
            else if (a == "i") mode = "install";
            else if (a == "r") mode = "restore";
            else if (a == "s") mode = "status";
            else if (a == "--dir" && i + 1 < args.Length) dirArg = args[i + 1];
        }

        try
        {
            if (string.IsNullOrEmpty(mode)) Interactive(dirArg);
            else Run(mode, dirArg, true);
        }
        catch (Exception ex)
        {
            Log("[!] Error: " + ex.Message);
            Console.Write("Press any key to exit...");
            try { Console.ReadKey(); } catch { }
        }
        try { File.WriteAllLines(LogPath, Lines, Encoding.UTF8); } catch { }
    }

    static void Interactive(string dirArg)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine("      Ron's Editor Patch Tool");
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("   [1] Install patch : remove 1000-row limit");
            Console.WriteLine("                       + disable update popup");
            Console.WriteLine("   [2] Uninstall patch : restore originals");
            Console.WriteLine("   [3] Check status");
            Console.WriteLine("   [0] Exit");
            Console.WriteLine("==============================================");
            Console.Write("Select [1/2/3/0]: ");
            string k = "";
            try { k = Console.ReadLine(); } catch { }
            if (k == "1") { Lines.Clear(); Run("install", dirArg, true); Pause(); }
            else if (k == "2") { Lines.Clear(); Run("restore", dirArg, true); Pause(); }
            else if (k == "3") { Lines.Clear(); Run("status", dirArg, true); Pause(); }
            else if (k == "0") return;
        }
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.Write("Press Enter to continue...");
        try { Console.ReadLine(); } catch { }
    }

    static void Run(string mode, string dirArg, bool pauseAtEnd)
    {
        Lines.Clear();
        Log("========== Ron's Editor Patch Tool ==========");
        Log("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        AppDir = FindAppDir(dirArg);
        if (AppDir == null)
        {
            Log("[!] Ron's Editor installation folder was not found.");
            Log("    Make sure the software is installed, or use --dir <folder>.");
            if (dirArg == null && mode != "status")
            {
                Console.Write("You may type the install folder now (empty to skip): ");
                string p = "";
                try { p = Console.ReadLine(); } catch { }
                if (!string.IsNullOrWhiteSpace(p)) { p = p.Trim().Trim('"'); if (Directory.Exists(p)) AppDir = p; }
            }
            if (AppDir == null) { if (pauseAtEnd) Pause(); return; }
        }
        Log("Install folder: " + AppDir);

        string exe = Path.Combine(AppDir, "Editor.WinGUI.exe");
        string dll = Path.Combine(AppDir, "RonsPlace.ApplicationCore.Forms.dll");
        string pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string settings = Path.Combine(pd, "Rons Place Apps", "Rons Editor", "Editor.WinGUI.settings");

        if (!File.Exists(exe)) { Log("[!] Editor.WinGUI.exe not found - wrong folder?"); if (pauseAtEnd) Pause(); return; }

        if (mode == "install")
        {
            if (Process.GetProcessesByName("Editor.WinGUI").Length > 0)
            {
                Log("[!] Ron's Editor is currently running. Close it first, then run this tool again.");
                if (pauseAtEnd) Pause(); return;
            }
            Log("");
            Log("---- 1/3: Editor.WinGUI.exe (remove 1000-row limit) ----");
            PatchExe(exe);
            Log("");
            Log("---- 2/3: RonsPlace.ApplicationCore.Forms.dll (disable update popup) ----");
            PatchDll(dll);
            Log("");
            Log("---- 3/3: Settings file (turn off automatic update check) ----");
            PatchSettings(settings);
            Log("");
            Log("========== Verification ==========");
            Verify(exe, dll, settings);
        }
        else if (mode == "restore")
        {
            if (Process.GetProcessesByName("Editor.WinGUI").Length > 0)
            {
                Log("[!] Ron's Editor is currently running. Close it first, then run this tool again.");
                if (pauseAtEnd) Pause(); return;
            }
            Log("");
            Log("---- Restoring original files ----");
            Restore(exe);
            Restore(dll);
            Restore(settings);
            Log("");
            Log("========== Status after restore ==========");
            Verify(exe, dll, settings);
        }
        else
        {
            Log("");
            Verify(exe, dll, settings);
        }
        if (pauseAtEnd) Pause();
    }

    // ---------- Patch implementation ----------

    static void PatchExe(string exe)
    {
        byte[] b = File.ReadAllBytes(exe);
        try
        {
            FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exe);
            Log("File version: " + vi.FileVersion);
        }
        catch { }

        bool changed = false;

        // Patch 1: the 1000-row limit in Lite_IsOverRowLimit()
        int off = FindBytes(b, SIG_ROW_LIMIT);
        if (off < 0)
            Log("  [!] Row-limit signature not found (different version?) - skipped");
        else if (IsPatchedConst(b, off))
            Log("  [=] Row limit: already patched @0x" + off.ToString("X"));
        else
        {
            Array.Copy(VAL_INTMAX, 0, b, off, VAL_INTMAX.Length);
            Log("  [+] Row limit: patched @0x" + off.ToString("X") + " (1000 -> 2147483647)");
            changed = true;
        }

        // Patch 2: the warning check in Lite_UpdateEvaluationWarning()
        off = FindBytes(b, SIG_WARN);
        if (off < 0)
            Log("  [!] Warning signature not found (different version?) - skipped");
        else if (IsPatchedConst(b, off))
            Log("  [=] Row-limit warning: already patched @0x" + off.ToString("X"));
        else
        {
            Array.Copy(VAL_INTMAX, 0, b, off, VAL_INTMAX.Length);
            Log("  [+] Row-limit warning: patched @0x" + off.ToString("X"));
            changed = true;
        }

        if (changed) WriteFile(exe, b);
        else Log("  [=] No changes needed for this file");
    }

    static void PatchDll(string dll)
    {
        if (!File.Exists(dll)) { Log("  [!] " + Path.GetFileName(dll) + " not found"); return; }
        byte[] b = File.ReadAllBytes(dll);
        int off = FindBytes(b, SIG_DLL_IL);
        if (off < 0) { Log("  [!] Update-callback signature not found (different version?) - skipped"); return; }

        bool already = b[off] == 0x2A;
        for (int i = 1; i < DLL_IL_LEN && already; i++) if (b[off + i] != 0x00) already = false;
        if (already) { Log("  [=] Update callback: already patched @0x" + off.ToString("X")); return; }

        b[off] = 0x2A; // ret
        for (int i = 1; i < DLL_IL_LEN; i++) b[off + i] = 0x00; // nop
        Log("  [+] Update callback: patched @0x" + off.ToString("X") + " (now returns immediately - no popup)");
        WriteFile(dll, b);
    }

    static void PatchSettings(string settings)
    {
        string dir = Path.GetDirectoryName(settings);
        if (!File.Exists(settings))
        {
            Log("  [!] Settings file does not exist (app may not have been run yet). Creating one with the update check disabled...");
            try
            {
                Directory.CreateDirectory(dir);
                string xml = "<Settings>\r\n  <Application>\r\n    <Online>\r\n      <VersionCheckEnabled Type=\"Boolean\">False</VersionCheckEnabled>\r\n    </Online>\r\n  </Application>\r\n</Settings>\r\n";
                File.WriteAllText(settings, xml, new UTF8Encoding(false));
                Log("  [+] Settings file created, VersionCheckEnabled = False");
            }
            catch (Exception ex) { Log("  [!] Failed to create: " + ex.Message); }
            return;
        }
        string text = File.ReadAllText(settings);
        Match m = Regex.Match(text, "<VersionCheckEnabled[^>]*>[^<]*</VersionCheckEnabled>");
        if (!m.Success)
        {
            Log("  [!] VersionCheckEnabled not found in settings (the DLL patch still blocks the popup)");
            return;
        }
        string val = m.Value;
        if (val.IndexOf(">True<", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            string nv = Regex.Replace(val, ">[^<]*<", ">False<");
            string newText = text.Substring(0, m.Index) + nv + text.Substring(m.Index + m.Length);
            string bak = settings + ".bak";
            try { if (!File.Exists(bak)) { File.Copy(settings, bak, true); Log("  [+] Original settings backed up: " + bak); } } catch { }
            try
            {
                File.WriteAllText(settings, newText, new UTF8Encoding(false));
                Log("  [+] VersionCheckEnabled set to False");
            }
            catch (Exception ex) { Log("  [!] Write failed: " + ex.Message); }
        }
        else if (val.IndexOf(">False<", StringComparison.OrdinalIgnoreCase) >= 0)
            Log("  [=] VersionCheckEnabled is already False");
        else
            Log("  [?] Unknown value: " + val);
    }

    static void WriteFile(string path, byte[] data)
    {
        string bak = path + ".bak";
        try
        {
            if (!File.Exists(bak)) { File.Copy(path, bak, true); Log("  [+] Original backed up: " + bak); }
            else Log("  [=] Backup already exists (kept): " + bak);
        }
        catch (Exception ex) { Log("  [!] Backup failed: " + ex.Message); return; }
        try
        {
            string tmp = path + ".patchtmp";
            File.WriteAllBytes(tmp, data);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
            Log("  [+] Patch written: " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            Log("  [!] Write failed (run as Administrator?): " + ex.Message);
        }
    }

    static void Restore(string path)
    {
        if (!File.Exists(path)) { Log("  [=] File not found, skip: " + Path.GetFileName(path)); return; }
        string bak = path + ".bak";
        if (!File.Exists(bak)) { Log("  [=] No backup, skip: " + Path.GetFileName(path)); return; }
        try
        {
            File.Copy(bak, path, true);
            Log("  [+] Restored original: " + Path.GetFileName(path));
        }
        catch (Exception ex) { Log("  [!] Restore failed: " + ex.Message); }
    }

    static void Verify(string exe, string dll, string settings)
    {
        if (File.Exists(exe))
        {
            byte[] b = File.ReadAllBytes(exe);
            int n = CountBytes(b, VAL_INTMAX);
            Log("Editor.WinGUI.exe: " + (n >= 2
                ? "[PATCHED] 1000-row limit removed (" + n + " patch(es) found)"
                : "[NOT PATCHED] only " + n + " patch(es) found (expected 2)"));
        }
        else Log("Editor.WinGUI.exe: [MISSING]");

        if (File.Exists(dll))
        {
            byte[] b = File.ReadAllBytes(dll);
            int off = FindBytes(b, SIG_DLL_IL);
            Log("RonsPlace.ApplicationCore.Forms.dll: " + (off < 0
                ? "[PATCHED] update popup disabled"
                : "[NOT PATCHED] update popup not disabled"));
        }
        else Log("RonsPlace.ApplicationCore.Forms.dll: [MISSING]");

        if (File.Exists(settings))
        {
            string t = File.ReadAllText(settings);
            Match m = Regex.Match(t, "<VersionCheckEnabled[^>]*>[^<]*</VersionCheckEnabled>");
            string v = m.Success ? m.Value : "(not found)";
            bool offState = v.IndexOf(">False<", StringComparison.OrdinalIgnoreCase) >= 0;
            Log("Settings VersionCheckEnabled: " + v + (offState ? "  [auto update check disabled]" : ""));
        }
        else Log("Settings file: [MISSING] (the DLL patch still blocks the update popup)");
    }

    // ---------- Locate installation folder ----------

    static string FindAppDir(string explicitDir)
    {
        List<string> cands = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitDir)) cands.Add(explicitDir.Trim().Trim('"'));
        try
        {
            RegistryKey[] roots = new RegistryKey[] { Registry.LocalMachine, Registry.CurrentUser };
            string[] subs = new string[] {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };
            foreach (RegistryKey root in roots)
            {
                foreach (string sub in subs)
                {
                    using (RegistryKey k = root.OpenSubKey(sub))
                    {
                        if (k == null) continue;
                        foreach (string name in k.GetSubKeyNames())
                        {
                            using (RegistryKey sk = k.OpenSubKey(name))
                            {
                                if (sk == null) continue;
                                string dn = (sk.GetValue("DisplayName") as string) ?? "";
                                string loc = (sk.GetValue("InstallLocation") as string) ?? "";
                                string icon = (sk.GetValue("DisplayIcon") as string) ?? "";
                                if (dn.IndexOf("Ron", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    loc.IndexOf("Rons", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (!string.IsNullOrEmpty(loc)) cands.Add(loc);
                                    if (!string.IsNullOrEmpty(icon))
                                    {
                                        try { cands.Add(Path.GetDirectoryName(icon)); } catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(pf86)) cands.Add(Path.Combine(pf86, "Rons Place Apps", "Rons Editor"));
        if (!string.IsNullOrEmpty(pf)) cands.Add(Path.Combine(pf, "Rons Place Apps", "Rons Editor"));
        foreach (string c in cands)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            try
            {
                if (Directory.Exists(c) && File.Exists(Path.Combine(c, "Editor.WinGUI.exe")))
                    return Path.GetFullPath(c);
            }
            catch { }
        }
        return null;
    }
}

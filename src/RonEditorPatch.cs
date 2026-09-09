using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

enum Product { None = 0, Editor = 1, DataEdit = 2 }

class RonEditorPatch
{
    // ================= Ron's Editor signatures (2020.7.23.1031) =================
    static readonly byte[] SIG_ROW_LIMIT = Hex("20E8030000FE02");   // Lite_IsOverRowLimit: ldc.i4 1000 + cgt
    static readonly byte[] SIG_WARN      = Hex("20E803000031");     // warning comparison: ldc.i4 1000 + ble.s
    static readonly byte[] SIG_DLL_IL    = Hex("14FE066900000673820100060A0206188D14000001251602A2251703A26F16000006262A"); // Forms.ProcessOnlineVersion IL
    static readonly byte[] VAL_INTMAX    = Hex("20FFFFFF7F");       // ldc.i4 2147483647

    // ================= Ron's Data Edit signatures (2026.9.x) =================
    static readonly byte[] SIG_DE_ROW   = Hex("20C4090000FE02");    // IsOverRowLimit: ldc.i4 2500 + cgt
    static readonly byte[] VAL_DE_ROW   = Hex("20FFFFFF7F");        // replacement (int.MaxValue)

    static string AppDir;
    static string LogPath;
    static readonly List<string> Lines = new List<string>();
    static readonly List<string> TempCopies = new List<string>();

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

    static int CountBytes(byte[] haystack, byte[] needle)
    {
        int n = 0, s = 0, i;
        while ((i = FindBytesFrom(haystack, needle, s)) >= 0) { n++; s = i + 1; }
        return n;
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

    static bool IsPatchedConst(byte[] b, int off)
    {
        return off >= 0 && b.Length > off + 4 &&
               b[off] == 0x20 && b[off + 1] == 0xFF && b[off + 2] == 0xFF &&
               b[off + 3] == 0xFF && b[off + 4] == 0x7F;
    }

    static Assembly OnResolve(object s, ResolveEventArgs e)
    {
        try
        {
            string name = new AssemblyName(e.Name).Name;
            if (!string.IsNullOrEmpty(AppDir))
            {
                string p = Path.Combine(AppDir, name + ".dll");
                if (File.Exists(p)) return Assembly.LoadFrom(p);
                p = Path.Combine(AppDir, name + ".exe");
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
        }
        catch { }
        return null;
    }

    static void Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RonEditorPatch.log");
        try { Console.Title = "Rons Place Patch Tool"; } catch { }

        string mode = "";
        string dirArg = null;
        string productArg = null;
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i].Trim().ToLower();
            if (a == "install" || a == "restore" || a == "status") mode = a;
            else if (a == "i") mode = "install";
            else if (a == "r") mode = "restore";
            else if (a == "s") mode = "status";
            else if (a == "--dir" && i + 1 < args.Length) dirArg = args[i + 1];
            else if (a == "--product" && i + 1 < args.Length) productArg = args[i + 1].Trim().ToLower();
        }

        try
        {
            if (string.IsNullOrEmpty(mode)) Interactive();
            else Run(mode, dirArg, productArg, true);
        }
        catch (Exception ex)
        {
            Log("[!] Error: " + ex.Message);
            Console.Write("Press any key to exit...");
            try { Console.ReadKey(); } catch { }
        }
        try { File.WriteAllLines(LogPath, Lines, Encoding.UTF8); } catch { }
    }

    // ================= interactive product selection =================

    static void Interactive()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine("   Rons Place Patch Tool");
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("   Select the software to patch:");
            Console.WriteLine("   [1] Ron's Editor     (remove 1000-row limit)");
            Console.WriteLine("   [2] Ron's Data Edit  (remove 2500-row limit)");
            Console.WriteLine("   [0] Exit");
            Console.WriteLine("==============================================");
            Console.Write("Select [1/2/0]: ");
            string k = "";
            try { k = Console.ReadLine(); } catch { }
            if (k == null) return; // EOF
            if (k == "1")
            {
                if (ProductMenu(Product.Editor)) { }
            }
            else if (k == "2")
            {
                if (ProductMenu(Product.DataEdit)) { }
            }
            else if (k == "0") return;
        }
    }

    static bool ProductMenu(Product product)
    {
        string name = product == Product.Editor ? "Ron's Editor" : "Ron's Data Edit";
        string exeName = product == Product.Editor ? "Editor.WinGUI.exe" : "DataEdit.WPFGUI.exe";
        string procName = product == Product.Editor ? "Editor.WinGUI" : "DataEdit.WPFGUI";

        string dir = FindProductDir(product, null);
        if (dir == null)
        {
            Console.WriteLine();
            Console.WriteLine("[!] " + name + " install folder was not found automatically.");
            Console.Write("Please type its install folder (empty to cancel): ");
            string p = "";
            try { p = Console.ReadLine(); } catch { }
            if (!string.IsNullOrWhiteSpace(p))
            {
                p = p.Trim().Trim('"');
                if (Directory.Exists(p) && File.Exists(Path.Combine(p, exeName))) dir = Path.GetFullPath(p);
                else { Console.WriteLine("[!] Folder does not contain " + exeName + " - cancelled."); return false; }
            }
            else return false;
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine("   Product: " + name);
            Console.WriteLine("   Folder : " + dir);
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("   [1] Install patch");
            Console.WriteLine("   [2] Uninstall patch (restore originals)");
            Console.WriteLine("   [3] Check status");
            Console.WriteLine("   [0] Back to product selection");
            Console.WriteLine("==============================================");
            Console.Write("Select [1/2/3/0]: ");
            string k = "";
            try { k = Console.ReadLine(); } catch { }
            if (k == null) return true; // EOF -> back
            if (k == "1")
            {
                if (Process.GetProcessesByName(procName).Length > 0)
                {
                    Log("[!] " + name + " is currently running. Close it first.");
                    Pause();
                    continue;
                }
                Lines.Clear();
                RunProduct(product, "install", dir, procName);
                Pause();
            }
            else if (k == "2")
            {
                if (Process.GetProcessesByName(procName).Length > 0)
                {
                    Log("[!] " + name + " is currently running. Close it first.");
                    Pause();
                    continue;
                }
                Lines.Clear();
                RunProduct(product, "restore", dir, procName);
                Pause();
            }
            else if (k == "3")
            {
                Lines.Clear();
                RunProduct(product, "status", dir, procName);
                Pause();
            }
            else if (k == "0") return true;
        }
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.Write("Press Enter to continue...");
        try { Console.ReadLine(); } catch { }
    }

    // ================= CLI mode =================

    static void Run(string mode, string dirArg, string productArg, bool pauseAtEnd)
    {
        Lines.Clear();
        Log("========== Rons Place Patch Tool ==========");
        Log("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        Product forceProduct = Product.None;
        if (productArg != null)
        {
            if (productArg == "editor") forceProduct = Product.Editor;
            else if (productArg == "dataedit") forceProduct = Product.DataEdit;
        }

        string dir = dirArg;
        if (dir == null)
        {
            if (forceProduct != Product.None) dir = FindProductDir(forceProduct, null);
            else dir = FindAnyDir();
        }
        else dir = dir.Trim().Trim('"');

        if (dir == null || !Directory.Exists(dir))
        {
            Log("[!] Install folder was not found.");
            Log("    Use --dir <folder>, or run without arguments for the menu.");
            if (pauseAtEnd) Pause(); return;
        }

        AppDir = Path.GetFullPath(dir);
        bool isEditor = File.Exists(Path.Combine(AppDir, "Editor.WinGUI.exe"));
        bool isDataEdit = File.Exists(Path.Combine(AppDir, "DataEdit.WPFGUI.exe"));
        if (forceProduct == Product.Editor) { isEditor = true; isDataEdit = false; }
        if (forceProduct == Product.DataEdit) { isEditor = false; isDataEdit = true; }
        if (!isEditor && !isDataEdit)
        {
            Log("[!] No supported product found in this folder.");
            Log("    (need Editor.WinGUI.exe or DataEdit.WPFGUI.exe)");
            if (pauseAtEnd) Pause(); return;
        }

        Log("Install folder: " + AppDir);
        Log("Product(s): " + (isEditor ? "Ron's Editor " : "") + (isDataEdit ? "Ron's Data Edit" : ""));

        if (mode == "install")
        {
            if (isEditor && Process.GetProcessesByName("Editor.WinGUI").Length > 0) { Log("[!] Ron's Editor is running - close it first."); if (pauseAtEnd) Pause(); return; }
            if (isDataEdit && Process.GetProcessesByName("DataEdit.WPFGUI").Length > 0) { Log("[!] Ron's Data Edit is running - close it first."); if (pauseAtEnd) Pause(); return; }
            if (isEditor) PatchEditor();
            if (isDataEdit) PatchDataEdit();
            Log("");
            Log("========== Verification ==========");
            if (isEditor) VerifyEditor();
            if (isDataEdit) VerifyDataEdit();
        }
        else if (mode == "restore")
        {
            if (isEditor && Process.GetProcessesByName("Editor.WinGUI").Length > 0) { Log("[!] Ron's Editor is running - close it first."); if (pauseAtEnd) Pause(); return; }
            if (isDataEdit && Process.GetProcessesByName("DataEdit.WPFGUI").Length > 0) { Log("[!] Ron's Data Edit is running - close it first."); if (pauseAtEnd) Pause(); return; }
            Log("");
            Log("---- Restoring original files ----");
            if (isEditor)
            {
                Restore(Path.Combine(AppDir, "Editor.WinGUI.exe"));
                Restore(Path.Combine(AppDir, "RonsPlace.ApplicationCore.Forms.dll"));
                Restore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Rons Place Apps", "Rons Editor", "Editor.WinGUI.settings"));
            }
            if (isDataEdit)
            {
                Restore(Path.Combine(AppDir, "DataEdit.Engine.dll"));
                Restore(Path.Combine(AppDir, "RonsPlace.ApplicationCore.WPF.dll"));
            }
            Log("");
            Log("========== Status after restore ==========");
            if (isEditor) VerifyEditor();
            if (isDataEdit) VerifyDataEdit();
        }
        else
        {
            Log("");
            if (isEditor) VerifyEditor();
            if (isDataEdit) VerifyDataEdit();
        }
        if (pauseAtEnd) Pause();
    }

    static void RunProduct(Product product, string mode, string dir, string procName)
    {
        AppDir = Path.GetFullPath(dir);
        if (product == Product.Editor)
        {
            Log("========== Ron's Editor ==========");
            if (mode == "install") { PatchEditor(); Log(""); Log("========== Verification =========="); VerifyEditor(); }
            else if (mode == "restore")
            {
                Restore(Path.Combine(AppDir, "Editor.WinGUI.exe"));
                Restore(Path.Combine(AppDir, "RonsPlace.ApplicationCore.Forms.dll"));
                Restore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Rons Place Apps", "Rons Editor", "Editor.WinGUI.settings"));
                Log(""); Log("========== Status after restore =========="); VerifyEditor();
            }
            else { VerifyEditor(); }
        }
        else
        {
            Log("========== Ron's Data Edit ==========");
            if (mode == "install") { PatchDataEdit(); Log(""); Log("========== Verification =========="); VerifyDataEdit(); }
            else if (mode == "restore")
            {
                Restore(Path.Combine(AppDir, "DataEdit.Engine.dll"));
                Restore(Path.Combine(AppDir, "RonsPlace.ApplicationCore.WPF.dll"));
                Log(""); Log("========== Status after restore =========="); VerifyDataEdit();
            }
            else { VerifyDataEdit(); }
        }
    }

    // ================= Ron's Editor patches =================

    static void PatchEditor()
    {
        string exe = Path.Combine(AppDir, "Editor.WinGUI.exe");
        string dll = Path.Combine(AppDir, "RonsPlace.ApplicationCore.Forms.dll");
        string pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string settings = Path.Combine(pd, "Rons Place Apps", "Rons Editor", "Editor.WinGUI.settings");

        Log("");
        Log("---- 1/3: Editor.WinGUI.exe (remove 1000-row limit) ----");
        byte[] b = File.ReadAllBytes(exe);
        try { FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exe); Log("File version: " + vi.FileVersion); } catch { }
        bool changed = false;
        int off = FindBytes(b, SIG_ROW_LIMIT);
        if (off < 0)
        {
            if (CountBytes(b, VAL_INTMAX) >= 2) Log("  [=] Row limit: already patched");
            else Log("  [!] Row-limit signature not found (different version?) - skipped");
        }
        else if (IsPatchedConst(b, off)) Log("  [=] Row limit: already patched @0x" + off.ToString("X"));
        else { Array.Copy(VAL_INTMAX, 0, b, off, VAL_INTMAX.Length); Log("  [+] Row limit: patched @0x" + off.ToString("X") + " (1000 -> 2147483647)"); changed = true; }
        off = FindBytes(b, SIG_WARN);
        if (off < 0)
        {
            if (CountBytes(b, VAL_INTMAX) >= 2) Log("  [=] Row-limit warning: already patched");
            else Log("  [!] Warning signature not found (different version?) - skipped");
        }
        else if (IsPatchedConst(b, off)) Log("  [=] Row-limit warning: already patched @0x" + off.ToString("X"));
        else { Array.Copy(VAL_INTMAX, 0, b, off, VAL_INTMAX.Length); Log("  [+] Row-limit warning: patched @0x" + off.ToString("X")); changed = true; }
        if (changed) WriteFile(exe, b);
        else Log("  [=] No changes needed for this file");

        Log("");
        Log("---- 2/3: RonsPlace.ApplicationCore.Forms.dll (disable update popup) ----");
        if (File.Exists(dll))
        {
            byte[] d = File.ReadAllBytes(dll);
            bool dchanged = PatchMethodToRet(d, dll,
                "RonsPlace.ApplicationCore.Forms.Extensions.Interactions",
                "ProcessOnlineVersion", SIG_DLL_IL, "update popup callback");
            if (dchanged) WriteFile(dll, d);
        }
        else Log("  [!] " + Path.GetFileName(dll) + " not found");

        Log("");
        Log("---- 3/3: Settings file (turn off auto update check) ----");
        PatchSettingsEditor(settings);
    }

    static void PatchSettingsEditor(string settings)
    {
        string dir = Path.GetDirectoryName(settings);
        if (!File.Exists(settings))
        {
            Log("  [!] Settings file does not exist. Creating one with the update check disabled...");
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
        if (!m.Success) { Log("  [!] VersionCheckEnabled not found in settings (the DLL patch still blocks the popup)"); return; }
        string val = m.Value;
        if (val.IndexOf(">True<", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            string nv = Regex.Replace(val, ">[^<]*<", ">False<");
            string newText = text.Substring(0, m.Index) + nv + text.Substring(m.Index + m.Length);
            string bak = settings + ".bak";
            try { if (!File.Exists(bak)) { File.Copy(settings, bak, true); Log("  [+] Original settings backed up: " + bak); } } catch { }
            try { File.WriteAllText(settings, newText, new UTF8Encoding(false)); Log("  [+] VersionCheckEnabled set to False"); }
            catch (Exception ex) { Log("  [!] Write failed: " + ex.Message); }
        }
        else if (val.IndexOf(">False<", StringComparison.OrdinalIgnoreCase) >= 0)
            Log("  [=] VersionCheckEnabled is already False");
        else Log("  [?] Unknown value: " + val);
    }

    // ================= Ron's Data Edit patches =================

    static void PatchDataEdit()
    {
        string engine = Path.Combine(AppDir, "DataEdit.Engine.dll");
        string wpf = Path.Combine(AppDir, "RonsPlace.ApplicationCore.WPF.dll");

        Log("");
        Log("---- 1/2: DataEdit.Engine.dll (remove 2500-row limit) ----");
        if (File.Exists(engine))
        {
            byte[] b = File.ReadAllBytes(engine);
            try { FileVersionInfo vi = FileVersionInfo.GetVersionInfo(engine); Log("File version: " + vi.FileVersion); } catch { }
            int off = FindBytes(b, SIG_DE_ROW);
            if (off < 0)
            {
                if (CountBytes(b, VAL_DE_ROW) >= 1) Log("  [=] Row limit: already patched");
                else Log("  [!] Row-limit signature not found (different version?) - skipped");
            }
            else if (IsPatchedConst(b, off)) Log("  [=] Row limit: already patched @0x" + off.ToString("X"));
            else
            {
                Array.Copy(VAL_DE_ROW, 0, b, off, VAL_DE_ROW.Length);
                Log("  [+] Row limit: patched @0x" + off.ToString("X") + " (2500 -> 2147483647)");
                WriteFile(engine, b);
            }
        }
        else Log("  [!] " + Path.GetFileName(engine) + " not found");

        Log("");
        Log("---- 2/2: RonsPlace.ApplicationCore.WPF.dll (disable auto update check + popup) ----");
        if (!File.Exists(wpf)) { Log("  [!] " + Path.GetFileName(wpf) + " not found"); return; }
        byte[] w = File.ReadAllBytes(wpf);
        bool wchanged = false;

        wchanged |= PatchMethodToRet(w, wpf,
            "RonsPlace.ApplicationCore.WPF.Services.AppOnlineInteractiveService",
            "NewVersionStartCheck", Hex(DataEditSigs.IL_NewVersionStartCheck),
            "startup auto version check");

        wchanged |= PatchMethodToRet(w, wpf,
            "RonsPlace.ApplicationCore.WPF.Services.AppOnlineInteractiveService",
            "ProcessOnlineVersion", Hex(DataEditSigs.IL_ProcessOnlineVersion),
            "version popup callback");

        wchanged |= PatchMethodToRet(w, wpf,
            "RonsPlace.ApplicationCore.WPF.Services.AppUpdateService",
            "PeriodCheck", Hex(DataEditSigs.IL_PeriodCheck),
            "periodic update check");

        if (wchanged) WriteFile(wpf, w);
        else Log("  [=] No changes needed for this file");
    }

    static bool PatchMethodToRet(byte[] fileBytes, string filePath, string typeName, string methodName, byte[] fallbackSig, string label)
    {
        int off = -1, len = 0;

        // 1) reflection-based location (version independent)
        try { if (LocateMethodByReflection(filePath, typeName, methodName, out off, out len)) { } }
        catch { off = -1; }

        // 2) fallback: version-specific signature
        if (off < 0)
        {
            try
            {
                if (fallbackSig != null && fallbackSig.Length > 0)
                {
                    int f = FindBytes(fileBytes, fallbackSig);
                    if (f >= 0) { off = f; len = fallbackSig.Length; }
                }
            }
            catch { off = -1; }
        }

        if (off < 0 || len <= 0)
        {
            Log("  [!] " + label + ": method not found (different version?) - skipped");
            return false;
        }

        bool already = fileBytes[off] == 0x2A && fileBytes[off + len - 1] == 0x2A;
        for (int i = 1; i < len - 1 && already; i++) if (fileBytes[off + i] != 0x00) already = false;
        if (already) { Log("  [=] " + label + ": already patched @0x" + off.ToString("X")); return false; }

        fileBytes[off] = 0x2A;                                     // ret at start
        for (int i = 1; i < len - 1; i++) fileBytes[off + i] = 0x00; // nop padding
        fileBytes[off + len - 1] = 0x2A;                           // ret at end (CLR requires it)
        Log("  [+] " + label + ": patched @0x" + off.ToString("X") + " (now returns immediately)");
        return true;
    }

    static bool LocateMethodByReflection(string filePath, string typeName, string methodName, out int off, out int len)
    {
        off = -1; len = 0;
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), "ronpatch_" + Guid.NewGuid().ToString("N") + Path.GetExtension(filePath));
            File.Copy(filePath, tmp, true);
            TempCopies.Add(tmp);
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
            Assembly asm = Assembly.LoadFrom(tmp);
            Type t = asm.GetType(typeName);
            if (t == null) return false;
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (m == null) return false;
            MethodBody b = m.GetMethodBody();
            if (b == null) return false;
            byte[] il = b.GetILAsByteArray();
            if (il == null || il.Length == 0) return false;
            byte[] file = File.ReadAllBytes(filePath);
            off = FindBytes(file, il);
            if (off < 0) return false;
            len = il.Length;
            return true;
        }
        catch { return false; }
    }

    // ================= write / restore / verify =================

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

    static void VerifyEditor()
    {
        string exe = Path.Combine(AppDir, "Editor.WinGUI.exe");
        string dll = Path.Combine(AppDir, "RonsPlace.ApplicationCore.Forms.dll");
        string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Rons Place Apps", "Rons Editor", "Editor.WinGUI.settings");
        Log("[Ron's Editor]");
        if (File.Exists(exe))
        {
            byte[] b = File.ReadAllBytes(exe);
            int n = CountBytes(b, VAL_INTMAX);
            Log("  Editor.WinGUI.exe: " + (n >= 2 ? "[PATCHED] 1000-row limit removed (" + n + ")" : "[NOT PATCHED] only " + n + " found (expected 2)"));
        }
        else Log("  Editor.WinGUI.exe: [MISSING]");
        if (File.Exists(dll))
        {
            byte[] b = File.ReadAllBytes(dll);
            int off = FindBytes(b, SIG_DLL_IL);
            Log("  RonsPlace.ApplicationCore.Forms.dll: " + (off < 0 ? "[PATCHED] update popup disabled" : "[NOT PATCHED]"));
        }
        else Log("  RonsPlace.ApplicationCore.Forms.dll: [MISSING]");
        if (File.Exists(settings))
        {
            string t = File.ReadAllText(settings);
            Match m = Regex.Match(t, "<VersionCheckEnabled[^>]*>[^<]*</VersionCheckEnabled>");
            string v = m.Success ? m.Value : "(not found)";
            bool offState = v.IndexOf(">False<", StringComparison.OrdinalIgnoreCase) >= 0;
            Log("  Settings: " + v + (offState ? "  [auto update check disabled]" : ""));
        }
        else Log("  Settings: [MISSING]");
    }

    static void VerifyDataEdit()
    {
        string engine = Path.Combine(AppDir, "DataEdit.Engine.dll");
        string wpf = Path.Combine(AppDir, "RonsPlace.ApplicationCore.WPF.dll");
        Log("[Ron's Data Edit]");
        if (File.Exists(engine))
        {
            byte[] b = File.ReadAllBytes(engine);
            int n = CountBytes(b, VAL_DE_ROW);
            Log("  DataEdit.Engine.dll: " + (n >= 1 ? "[PATCHED] 2500-row limit removed (" + n + ")" : "[NOT PATCHED]"));
        }
        else Log("  DataEdit.Engine.dll: [MISSING]");
        if (File.Exists(wpf))
        {
            byte[] b = File.ReadAllBytes(wpf);
            int missing = 0;
            if (FindBytes(b, Hex(DataEditSigs.IL_NewVersionStartCheck)) >= 0) missing++;
            if (FindBytes(b, Hex(DataEditSigs.IL_ProcessOnlineVersion)) >= 0) missing++;
            if (FindBytes(b, Hex(DataEditSigs.IL_PeriodCheck)) >= 0) missing++;
            Log("  RonsPlace.ApplicationCore.WPF.dll: " + (missing == 0 ? "[PATCHED] auto update check disabled (3 methods)" : "[NOT PATCHED] " + missing + " method(s) still original"));
        }
        else Log("  RonsPlace.ApplicationCore.WPF.dll: [MISSING]");
    }

    // ================= folder locating =================

    static string FindAnyDir()
    {
        return FindProductDir(Product.None, null);
    }

    static string FindProductDir(Product product, string explicitDir)
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
                                bool isDataEditEntry = dn.IndexOf("Data Edit", StringComparison.OrdinalIgnoreCase) >= 0;
                                bool isEditorEntry = !isDataEditEntry &&
                                    (dn.IndexOf("Ron", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     loc.IndexOf("Rons", StringComparison.OrdinalIgnoreCase) >= 0);
                                bool match = false;
                                if (product == Product.DataEdit) match = isDataEditEntry;
                                else if (product == Product.Editor) match = isEditorEntry;
                                else match = isDataEditEntry || isEditorEntry;
                                if (!match) continue;
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
        catch { }
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (product == Product.Editor || product == Product.None)
        {
            if (!string.IsNullOrEmpty(pf86)) cands.Add(Path.Combine(pf86, "Rons Place Apps", "Rons Editor"));
            if (!string.IsNullOrEmpty(pf)) cands.Add(Path.Combine(pf, "Rons Place Apps", "Rons Editor"));
        }
        if (product == Product.DataEdit || product == Product.None)
        {
            if (!string.IsNullOrEmpty(pf86)) cands.Add(Path.Combine(pf86, "Rons Place Software", "Rons Data Edit"));
            if (!string.IsNullOrEmpty(pf)) cands.Add(Path.Combine(pf, "Rons Place Software", "Rons Data Edit"));
        }
        foreach (string c in cands)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            try
            {
                if (!Directory.Exists(c)) continue;
                if (product == Product.Editor && File.Exists(Path.Combine(c, "Editor.WinGUI.exe"))) return Path.GetFullPath(c);
                if (product == Product.DataEdit && File.Exists(Path.Combine(c, "DataEdit.WPFGUI.exe"))) return Path.GetFullPath(c);
                if (product == Product.None &&
                    (File.Exists(Path.Combine(c, "Editor.WinGUI.exe")) || File.Exists(Path.Combine(c, "DataEdit.WPFGUI.exe"))))
                    return Path.GetFullPath(c);
            }
            catch { }
        }
        return null;
    }
}

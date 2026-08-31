============================================================
  Ron's Editor 破解补丁工具 使用说明
  （解除 1000 行限制 + 屏蔽更新提示）
============================================================

【工具包含】
  RonEditorPatch.exe   —— 补丁主程序（单文件，绿色便携）

【适用版本】
  Ron's Editor 2020.7.23.1031（文件版本 2021.1.26.1742）
  其他版本安装后如提示"特征码未找到"，补丁会安全跳过，不会损坏文件。

【使用步骤】
  1. 先把 Ron's Editor 安装到目标电脑（任意盘符均可）。
     注意：补丁前请关闭 Ron's Editor 软件（若正在运行）。
  2. 双击 RonEditorPatch.exe。
     - Windows 会弹出 UAC 管理员权限提示，点"是"（写入 Program Files 需要管理员）。
  3. 出现菜单后按数字键 1（安装补丁），回车。
  4. 看到 "[已破解] 1000 行限制已解除"、"[已破解] 更新弹窗已屏蔽"
     即表示成功。
  5. 打开 Ron's Editor 正常使用：
     - 单个 CSV 文件可以保存超过 1000 行；
     - 启动时不再弹出"发现新版本"的更新提示。

【命令行方式】（可选）
  RonEditorPatch.exe install --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
  RonEditorPatch.exe restore --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"
  RonEditorPatch.exe status  --dir "C:\Program Files (x86)\Rons Place Apps\Rons Editor"

  菜单数字说明：
    1 = 安装补丁      2 = 卸载补丁（恢复原版）    3 = 检测状态

【自动定位】
  工具会自动从注册表和常见安装路径查找 Ron's Editor。
  若提示找不到，可用 --dir 参数或按提示手动输入安装目录。

【补丁内容】
  1. Editor.WinGUI.exe（2 处）
     - Lite_IsOverRowLimit()：1000 行限制判断改为 int.MaxValue，
       保存/另存为/导出命令不再因超过 1000 行而被禁用。
     - Lite_UpdateEvaluationWarning()：不再显示"只能保存 1000 行"警告。
  2. RonsPlace.ApplicationCore.Forms.dll（1 处）
     - ProcessOnlineVersion()：自动更新检查的回调改为直接返回，
       即使联网检查也不会再弹"发现新版本"提示。
     - 手动"检查更新"菜单不受影响，仍可正常使用。
  3. 设置文件 Editor.WinGUI.settings
     - VersionCheckEnabled 改为 False，彻底关闭自动更新检查。

【备份与恢复】
  - 每次写入前自动备份原文件（同名 .bak，只保留最早的一份）。
  - 想恢复原版：菜单选 2（卸载补丁），或把 .bak 文件改回原名。
  - 备份位置：
      C:\Program Files (x86)\Rons Place Apps\Rons Editor\Editor.WinGUI.exe.bak
      C:\Program Files (x86)\Rons Place Apps\Rons Editor\RonsPlace.ApplicationCore.Forms.dll.bak
      C:\ProgramData\Rons Place Apps\Rons Editor\Editor.WinGUI.settings.bak

【注意事项】
  - 补丁后不要点软件的"检查更新"安装新版，新版会覆盖补丁文件，
    需要重新运行本工具再打一次补丁。
  - 个别杀毒软件可能对补丁工具报风险（因为它修改程序文件），
    如被拦截请添加信任后重试。
  - 本工具仅供学习研究使用。
============================================================

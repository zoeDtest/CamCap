using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyVersion("2.0.3.0")]
[assembly: AssemblyFileVersion("2.0.3.0")]
[assembly: AssemblyInformationalVersion("2.0.3")]

internal static class SingleFileInstaller
{
    private const string AppName = "CamCapture";
    private const string InstallerTitle = "CamCapture 安装程序";
    private const string ProgramFolder = "Program";
    private const string ConfigFolder = "Config";
    private const string DataFolder = "Data";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var defaultParentDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var parentDir = SelectInstallParentDirectory(defaultParentDir);
            if (string.IsNullOrWhiteSpace(parentDir))
            {
                return;
            }

            var installDir = BuildInstallDirectory(parentDir);
            var createDesktopShortcut = AskCreateDesktopShortcut();

            var confirm = MessageBox.Show(
                "即将安装 " + AppName + " 到：" + installDir + Environment.NewLine + Environment.NewLine + "是否继续？",
                InstallerTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (confirm != DialogResult.OK)
            {
                return;
            }

            InstallPackage(installDir, createDesktopShortcut);

            var launch = MessageBox.Show(
                "安装完成。是否立即启动 " + AppName + "？",
                InstallerTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (launch == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GetProgramPath(installDir),
                    WorkingDirectory = Path.Combine(installDir, ProgramFolder),
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "安装失败：" + ex.Message + Environment.NewLine + Environment.NewLine + ex,
                InstallerTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string SelectInstallParentDirectory(string defaultParentDir)
    {
        using (var dialog = new FolderBrowserDialog())
        {
            dialog.Description = "请选择安装位置，程序会在该位置下创建 CamCapture 文件夹。";
            dialog.SelectedPath = defaultParentDir;
            dialog.ShowNewFolderButton = true;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }

    private static string BuildInstallDirectory(string selectedDir)
    {
        var trimmed = selectedDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var selectedName = Path.GetFileName(trimmed);

        if (string.Equals(selectedName, AppName, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return Path.Combine(trimmed, AppName);
    }

    private static bool AskCreateDesktopShortcut()
    {
        return MessageBox.Show(
            "是否创建桌面快捷方式？",
            InstallerTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private static void InstallPackage(string installDir, bool createDesktopShortcut)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "CamCaptureInstaller_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            ExtractPayload(tempRoot);

            Directory.CreateDirectory(installDir);
            CopyDirectory(tempRoot, installDir);
            Directory.CreateDirectory(Path.Combine(installDir, ConfigFolder));
            Directory.CreateDirectory(Path.Combine(installDir, DataFolder, "processing"));
            Directory.CreateDirectory(Path.Combine(installDir, DataFolder, "storage"));
            Directory.CreateDirectory(Path.Combine(installDir, DataFolder, "Logs"));
            Directory.CreateDirectory(Path.Combine(installDir, DataFolder, "SdkLog"));

            CreateShortcuts(installDir, createDesktopShortcut);
            WriteUninstallScript(installDir);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                }
            }
        }
    }

    private static void ExtractPayload(string tempRoot)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
        {
            throw new InvalidOperationException("找不到内置安装资源。");
        }

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("无法读取内置安装资源。");
            }

            var tempZip = Path.Combine(tempRoot, "payload.zip");
            using (var file = File.Create(tempZip))
            {
                stream.CopyTo(file);
            }

            ZipFile.ExtractToDirectory(tempZip, tempRoot);
            File.Delete(tempZip);
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = directory.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            var destination = Path.Combine(targetDir, relative);
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Copy(file, destination, true);
        }
    }

    private static void CreateShortcuts(string installDir, bool createDesktopShortcut)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var programPath = GetProgramPath(installDir);
        var programDir = Path.GetDirectoryName(programPath);

        if (createDesktopShortcut)
        {
            CreateShortcut(
                Path.Combine(desktop, "CamCapture.lnk"),
                programPath,
                programDir,
                programPath);
        }

        CreateShortcut(
            Path.Combine(startMenu, "CamCapture.lnk"),
            programPath,
            programDir,
            programPath);

        CreateShortcut(
            Path.Combine(startMenu, "CamCapture 卸载.lnk"),
            Path.Combine(installDir, "uninstall.cmd"),
            installDir,
            programPath);
    }

    private static string GetProgramPath(string installDir)
    {
        return Path.Combine(installDir, ProgramFolder, "CamCapture.exe");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            throw new InvalidOperationException("无法创建快捷方式。");
        }

        dynamic shell = Activator.CreateInstance(shellType);
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = iconPath + ",0";
        shortcut.Save();
    }

    private static void WriteUninstallScript(string installDir)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var scriptPath = Path.Combine(installDir, "uninstall.cmd");

        var script = "@echo off" + Environment.NewLine +
                     "setlocal" + Environment.NewLine +
                     "del \"" + Path.Combine(desktop, "CamCapture.lnk") + "\" >nul 2>nul" + Environment.NewLine +
                     "del \"" + Path.Combine(startMenu, "CamCapture.lnk") + "\" >nul 2>nul" + Environment.NewLine +
                     "del \"" + Path.Combine(startMenu, "CamCapture 卸载.lnk") + "\" >nul 2>nul" + Environment.NewLine +
                     "cd /d \"%TEMP%\"" + Environment.NewLine +
                     "timeout /t 1 /nobreak >nul" + Environment.NewLine +
                     "rd /s /q \"" + installDir + "\"" + Environment.NewLine +
                     "echo CamCapture has been removed." + Environment.NewLine;

        File.WriteAllText(scriptPath, script, Encoding.UTF8);
    }
}

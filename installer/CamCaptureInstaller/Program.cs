using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace CamCaptureInstaller;

internal static class Program
{
    private const string AppName = "CamCapture";
    private const string InstallerTitle = "CamCapture 安装程序";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

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
                $"即将安装 {AppName} 到：{installDir}{Environment.NewLine}{Environment.NewLine}是否继续？",
                InstallerTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (confirm != DialogResult.OK)
            {
                return;
            }

            InstallPackage(installDir, createDesktopShortcut);

            var launch = MessageBox.Show(
                $"安装完成。是否立即启动 {AppName}？",
                InstallerTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (launch == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(installDir, "CamCapture.exe"),
                    WorkingDirectory = installDir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"安装失败：{ex.Message}{Environment.NewLine}{Environment.NewLine}{ex}",
                InstallerTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string? SelectInstallParentDirectory(string defaultParentDir)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择安装位置，程序会在该位置下创建 CamCapture 文件夹。",
            SelectedPath = defaultParentDir,
            ShowNewFolderButton = true
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
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
            Directory.CreateDirectory(Path.Combine(installDir, "captures"));
            Directory.CreateDirectory(Path.Combine(installDir, "SdkLog"));

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
            .First(name => name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("找不到内置安装资源。");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(tempRoot, overwriteFiles: true);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destination = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);
        }
    }

    private static void CreateShortcuts(string installDir, bool createDesktopShortcut)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

        if (createDesktopShortcut)
        {
            CreateShortcut(
                Path.Combine(desktop, "CamCapture.lnk"),
                Path.Combine(installDir, "CamCapture.exe"),
                installDir,
                Path.Combine(installDir, "CamCapture.exe"));
        }

        CreateShortcut(
            Path.Combine(startMenu, "CamCapture.lnk"),
            Path.Combine(installDir, "CamCapture.exe"),
            installDir,
            Path.Combine(installDir, "CamCapture.exe"));

        CreateShortcut(
            Path.Combine(startMenu, "CamCapture 卸载.lnk"),
            Path.Combine(installDir, "uninstall.cmd"),
            installDir,
            Path.Combine(installDir, "CamCapture.exe"));
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath)
    {
        var shell = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建快捷方式。");
        dynamic shellObject = Activator.CreateInstance(shell)!;
        dynamic shortcut = shellObject.CreateShortcut(shortcutPath);
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

        var script = $"""
@echo off
setlocal
del "{Path.Combine(desktop, "CamCapture.lnk")}" >nul 2>nul
del "{Path.Combine(startMenu, "CamCapture.lnk")}" >nul 2>nul
del "{Path.Combine(startMenu, "CamCapture 卸载.lnk")}" >nul 2>nul
cd /d "%TEMP%"
timeout /t 1 /nobreak >nul
rd /s /q "{installDir}"
echo CamCapture has been removed.
""";

        File.WriteAllText(scriptPath, script, Encoding.UTF8);
    }
}

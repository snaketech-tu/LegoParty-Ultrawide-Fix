using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LegoPartyUltrawidePatcher;

internal static class Program
{
    public const string GameExe = "LEGO Party.exe";

    [STAThread]
    private static void Main()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PatcherForm());
    }
}

internal sealed class PatcherForm : Form
{
    private readonly TextBox _gameDir = new() { Dock = DockStyle.Fill };
    private readonly Button _browse = new() { Text = "Browse...", AutoSize = true };
    private readonly CheckBox _enableLogs = new() { Text = "Enable BepInEx logs (console window + LogOutput.log)", AutoSize = true };
    private readonly Button _patch = new() { Text = "Patch", AutoSize = true, Padding = new Padding(16, 2, 16, 2) };
    private readonly Button _uninstall = new() { Text = "Uninstall", AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Height = 18 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(560, 0) };

    public PatcherForm()
    {
        Text = "LegoParty Ultrawide Fix - Patcher";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(12);

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var heading = new Label { Text = "LEGO Party game folder:", AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 2);
        layout.Controls.Add(_gameDir, 0, 1);
        layout.Controls.Add(_browse, 1, 1);
        layout.Controls.Add(_enableLogs, 0, 2);
        layout.SetColumnSpan(_enableLogs, 2);
        layout.Controls.Add(_progress, 0, 3);
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        buttons.Controls.Add(_patch);
        buttons.Controls.Add(_uninstall);
        layout.Controls.Add(buttons, 1, 3);
        layout.Controls.Add(_status, 0, 4);
        layout.SetColumnSpan(_status, 2);
        Controls.Add(layout);

        _gameDir.Text = SteamLocator.FindGameDir() ?? "";
        _status.Text = _gameDir.Text.Length > 0 ? "Game folder found via Steam." : "Select the folder that contains \"LEGO Party.exe\".";
        _browse.Click += (_, _) => Browse();
        _patch.Click += async (_, _) => await PatchAsync();
        _uninstall.Click += async (_, _) => await UninstallAsync();
        AcceptButton = _patch;
    }

    private bool TryGetGameDir(string action, out string dir)
    {
        dir = _gameDir.Text.Trim().Trim('"');
        if (!File.Exists(Path.Combine(dir, Program.GameExe)))
        {
            Fail($"\"{Program.GameExe}\" was not found in that folder.");
            return false;
        }
        if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Program.GameExe)).Length > 0)
        {
            Fail($"LEGO Party is running. Close the game and press {action} again.");
            return false;
        }
        return true;
    }

    private async Task UninstallAsync()
    {
        if (!TryGetGameDir("Uninstall", out string dir)) return;

        var otherMods = Uninstaller.OtherPlugins(dir);
        bool removeBepInEx;
        if (otherMods.Count > 0)
        {
            var answer = MessageBox.Show(this,
                "BepInEx in this folder also loads other mods:\n  " + string.Join("\n  ", otherMods) + "\n\n" +
                "Yes: remove the ultrawide fix and BepInEx, including those mods.\n" +
                "No: remove only the ultrawide fix and keep BepInEx.",
                "Uninstall", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Cancel) return;
            removeBepInEx = answer == DialogResult.Yes;
        }
        else
        {
            var answer = MessageBox.Show(this,
                "Remove the ultrawide fix and BepInEx from this folder?\nThe game itself is not touched.",
                "Uninstall", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (answer != DialogResult.OK) return;
            removeBepInEx = true;
        }

        SetBusy(true);
        _status.Text = "Uninstalling...";
        try
        {
            await Task.Run(() => Uninstaller.Uninstall(dir, removeBepInEx));
            _progress.Value = 0;
            _status.ForeColor = Color.DarkGreen;
            _status.Text = removeBepInEx
                ? "Uninstalled. The game now runs without BepInEx and without the ultrawide fix."
                : "The ultrawide fix was removed. BepInEx and your other mods are still installed.";
        }
        catch (Exception e)
        {
            Fail("Uninstalling failed: " + e.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the LEGO Party game folder (contains \"LEGO Party.exe\")",
            SelectedPath = Directory.Exists(_gameDir.Text) ? _gameDir.Text : "",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _gameDir.Text = dialog.SelectedPath;
    }

    private async Task PatchAsync()
    {
        if (!TryGetGameDir("Patch", out string dir)) return;

        SetBusy(true);
        bool logs = _enableLogs.Checked;
        var progress = new Progress<(int percent, string message)>(p =>
        {
            _progress.Value = Math.Max(0, Math.Min(100, p.percent));
            _status.Text = p.message;
        });
        try
        {
            await Installer.InstallAsync(dir, logs, progress);
            _status.ForeColor = Color.DarkGreen;
            _status.Text = "Patched. Start the game normally; the first start takes a few minutes longer.";
        }
        catch (Exception e)
        {
            Fail("Patching failed: " + e.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _patch.Enabled = _uninstall.Enabled = _browse.Enabled = _gameDir.Enabled = _enableLogs.Enabled = !busy;
        UseWaitCursor = busy;
        if (busy) _status.ForeColor = SystemColors.ControlText;
    }

    private void Fail(string message)
    {
        _status.ForeColor = Color.Firebrick;
        _status.Text = message;
    }
}

internal static class Installer
{
    private const string PluginPath = @"BepInEx\plugins\LegoPartyUltrawide\LegoPartyUltrawide.dll";
    private const string BepInExConfig = @"BepInEx\config\BepInEx.cfg";
    private static string ArchiveName => Uri.UnescapeDataString(BepInExInfo.Url.Substring(BepInExInfo.Url.LastIndexOf('/') + 1));

    public static async Task InstallAsync(string gameDir, bool enableLogs, IProgress<(int, string)> progress)
    {
        string root = Path.GetFullPath(gameDir);
        byte[] archive = await GetBepInExArchiveAsync(progress);

        await Task.Run(() =>
        {
            progress.Report((60, "Installing BepInEx..."));
            ExtractBepInEx(archive, root);

            progress.Report((95, "Installing the ultrawide plugin..."));
            string plugin = Path.Combine(root, PluginPath);
            Directory.CreateDirectory(Path.GetDirectoryName(plugin)!);
            using (var source = typeof(Installer).Assembly.GetManifestResourceStream("LegoPartyUltrawide.dll")!)
            using (var target = File.Create(plugin))
                source.CopyTo(target);

            SetLogging(Path.Combine(root, BepInExConfig), enableLogs);
        });
        progress.Report((100, "Done."));
    }

    // Uses a copy of the archive placed next to the patcher if present, otherwise downloads it.
    // Either way the bytes must match the pinned SHA-256.
    private static async Task<byte[]> GetBepInExArchiveAsync(IProgress<(int, string)> progress)
    {
        string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ArchiveName);
        byte[] data;
        if (File.Exists(local))
        {
            progress.Report((10, $"Using {ArchiveName} next to the patcher..."));
            data = File.ReadAllBytes(local);
        }
        else
        {
            progress.Report((5, $"Downloading BepInEx {BepInExInfo.Version}..."));
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LegoPartyUltrawidePatcher");
            data = await http.GetByteArrayAsync(BepInExInfo.Url);
        }

        progress.Report((55, "Verifying download..."));
        string hash;
        using (var sha = SHA256.Create())
            hash = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        if (hash != BepInExInfo.Sha256)
            throw new InvalidDataException(
                $"The BepInEx archive failed the checksum check (expected {BepInExInfo.Sha256}, got {hash}). Nothing was changed.");
        return data;
    }

    private static void ExtractBepInEx(byte[] archive, string root)
    {
        using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith("/") || entry.FullName == "changelog.txt") continue;
            string target = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    // Sets "Enabled" in [Logging.Console] and [Logging.Disk]. BepInEx fills in every other setting on first start.
    private static void SetLogging(string configPath, bool enabled)
    {
        string value = "Enabled = " + (enabled ? "true" : "false");
        var lines = File.Exists(configPath) ? File.ReadAllLines(configPath).ToList() : new List<string>();
        foreach (var section in new[] { "[Logging.Console]", "[Logging.Disk]" })
        {
            int start = lines.FindIndex(l => l.Trim() == section);
            if (start < 0)
            {
                lines.AddRange(new[] { "", section, value });
                continue;
            }
            int end = lines.FindIndex(start + 1, l => l.TrimStart().StartsWith("["));
            if (end < 0) end = lines.Count;
            int key = lines.FindIndex(start + 1, end - start - 1, l => l.TrimStart().StartsWith("Enabled ="));
            if (key >= 0) lines[key] = value;
            else lines.Insert(start + 1, value);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllLines(configPath, lines);
    }
}

internal static class Uninstaller
{
    private const string PluginFolder = @"BepInEx\plugins\LegoPartyUltrawide";
    private const string PluginConfig = @"BepInEx\config\snaketech.legoparty.ultrawide.cfg";

    // Everything a BepInEx IL2CPP install adds to the game folder; the game's own files are never touched.
    private static readonly string[] BepInExFiles = { "winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt" };
    private static readonly string[] BepInExFolders = { "BepInEx", "dotnet" };

    // Names of other plugins BepInEx loads from this folder, so the user can decide whether to keep BepInEx.
    public static List<string> OtherPlugins(string gameDir)
    {
        string plugins = Path.Combine(gameDir, @"BepInEx\plugins");
        if (!Directory.Exists(plugins)) return new List<string>();
        string ours = Path.GetFileName(PluginFolder);
        return Directory.EnumerateFileSystemEntries(plugins)
            .Select(Path.GetFileName)
            .Where(name => !name.Equals(ours, StringComparison.OrdinalIgnoreCase))
            .Where(name => Directory.Exists(Path.Combine(plugins, name)) || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void Uninstall(string gameDir, bool removeBepInEx)
    {
        string root = Path.GetFullPath(gameDir);
        DeleteDirectory(Path.Combine(root, PluginFolder));
        DeleteFile(Path.Combine(root, PluginConfig));
        if (!removeBepInEx) return;

        foreach (var file in BepInExFiles) DeleteFile(Path.Combine(root, file));
        foreach (var folder in BepInExFolders) DeleteDirectory(Path.Combine(root, folder));
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}

internal static class SteamLocator
{
    private const string GameFolder = @"steamapps\common\Lego Party";

    public static string? FindGameDir()
    {
        try
        {
            var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (string.IsNullOrEmpty(steamPath)) return null;

            var libraries = new List<string> { steamPath! };
            string vdf = Path.Combine(steamPath!, @"steamapps\libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));

            return libraries
                .Select(lib => Path.Combine(lib.Replace('/', '\\'), GameFolder))
                .FirstOrDefault(dir => File.Exists(Path.Combine(dir, Program.GameExe)));
        }
        catch
        {
            return null;
        }
    }
}

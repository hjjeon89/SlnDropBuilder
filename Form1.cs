using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace SlnDropBuilder;

public sealed partial class Form1 : Form
{
    private string? _lastRootPath;
    private string[]? _lastSelectedTargetPaths;
    private readonly Dictionary<string, RichTextBox> _targetLogBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TabPage> _targetLogPages = new(StringComparer.OrdinalIgnoreCase);
    private bool _hideWarnings = true;
    private bool _isBuilding;
    private bool _useDefaultOutputFolder = true;
    private CancellationTokenSource? _buildCancellationTokenSource;

    public Form1()
    {
        InitializeComponent();
        UpdateLogTabHeaderSize();
        AppendLog("Ready. Drop a root folder that contains C# projects or solutions.");
        AppendLog("If multiple executable projects are found, choose only the projects that are ready to build.");
    }

    private void HandleHideWarningsCheckedChanged(object? sender, EventArgs e)
    {
        _hideWarnings = _hideWarningsCheckBox.Checked;
    }

    private void HandleUseDefaultOutputFolderButtonClick(object? sender, EventArgs e)
    {
        UseDefaultOutputFolder();
    }

    private void HandleStopBuildButtonClick(object? sender, EventArgs e)
    {
        if (!_isBuilding || _buildCancellationTokenSource is null)
        {
            return;
        }

        _stopBuildButton.Enabled = false;
        SetStatus("Stopping build...");
        AppendLog("Build cancellation requested. Stopping active dotnet processes...");
        _buildCancellationTokenSource.Cancel();
    }

    private void HandleBrowseOutputFolderButtonClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the folder where the build folder will be created.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        string? currentBaseFolder = GetCurrentOutputBaseFolder();
        if (currentBaseFolder is not null && Directory.Exists(currentBaseFolder))
        {
            dialog.InitialDirectory = currentBaseFolder;
        }
        else if (_lastRootPath is not null && Directory.Exists(_lastRootPath))
        {
            dialog.InitialDirectory = _lastRootPath;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _useDefaultOutputFolder = false;
            _outputFolderTextBox.Text = Path.Combine(dialog.SelectedPath, "build");
        }
    }

    private string? GetCurrentOutputBaseFolder()
    {
        string outputPath = _outputFolderTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(outputPath) ? null : Path.GetDirectoryName(outputPath);
    }

    private void UseDefaultOutputFolder()
    {
        _useDefaultOutputFolder = true;
        _outputFolderTextBox.Text = _lastRootPath is null
            ? string.Empty
            : Path.Combine(_lastRootPath, "build");
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = HasSingleDroppedDirectory(e) && !_isBuilding
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void HandleDragDrop(object? sender, DragEventArgs e)
    {
        if (_isBuilding)
        {
            AppendLog("A build is already running. Drop is ignored.");
            return;
        }

        string? rootPath = GetSingleDroppedDirectory(e);
        if (rootPath is null)
        {
            AppendLog("Warning: only an existing directory can be dropped.");
            return;
        }

        await BuildFromRootAsync(rootPath);
    }

    private async void HandleRebuildButtonClick(object? sender, EventArgs e)
    {
        if (_isBuilding)
        {
            return;
        }

        if (_lastRootPath is null || !Directory.Exists(_lastRootPath))
        {
            _rebuildButton.Enabled = false;
            AppendLog("Warning: no valid previous folder is available to rebuild.");
            return;
        }

        await BuildFromRootAsync(_lastRootPath, _lastSelectedTargetPaths);
    }

    private static bool HasSingleDroppedDirectory(DragEventArgs e)
    {
        return GetSingleDroppedDirectory(e) is not null;
    }

    private static string? GetSingleDroppedDirectory(DragEventArgs e)
    {
        IDataObject? data = e.Data;
        if (data is null || !data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (data.GetData(DataFormats.FileDrop) is not string[] { Length: 1 } paths)
        {
            return null;
        }

        string path = paths[0];
        return Directory.Exists(path) ? path : null;
    }

    private async Task BuildFromRootAsync(string rootPath, string[]? preferredTargetPaths = null)
    {
        _isBuilding = true;
        _buildCancellationTokenSource?.Dispose();
        _buildCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _buildCancellationTokenSource.Token;
        _lastRootPath = rootPath;
        if (_useDefaultOutputFolder)
        {
            _outputFolderTextBox.Text = Path.Combine(rootPath, "build");
        }

        _rebuildButton.Enabled = false;
        _stopBuildButton.Enabled = true;
        SetBuildOptionsEnabled(false);
        _progressBar.Value = 0;
        SetStatus("Scanning solutions...");
        _dropPathLabel.Text = rootPath;
        AppendLog("");
        AppendLog($"Root: {rootPath}");

        try
        {
            string buildOutputPath = GetBuildOutputPath(rootPath);

            string[] targetPaths = await Task.Run(() => FindBuildTargets(rootPath, buildOutputPath), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (targetPaths.Length == 0)
            {
                AppendLog("Warning: no executable .csproj, .sln, or .slnx files were found.");
                SetStatus("No build targets found");
                return;
            }

            targetPaths = SelectTargetsToBuild(rootPath, targetPaths, preferredTargetPaths);
            if (targetPaths.Length == 0)
            {
                AppendLog("Build canceled: no build targets were selected.");
                SetStatus("Build canceled");
                return;
            }

            _lastSelectedTargetPaths = targetPaths;
            PrepareTargetLogTabs(rootPath, targetPaths);

            int maxParallelBuilds = (int)_parallelBuildCountInput.Value;
            _progressBar.Maximum = targetPaths.Length * 4;
            AppendLog($"Building {targetPaths.Length} selected target(s). Max parallel: {maxParallelBuilds}.");
            AppendLog($"Build output: {buildOutputPath}");
            SetStatus("Build is running in the background...");

            int failures = await ExecuteBuildAsync(buildOutputPath, targetPaths, maxParallelBuilds, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SetStatus(failures == 0
                ? "Build completed successfully"
                : $"Build completed with {failures} failed target(s)");
            AppendLog(failures == 0 ? "All builds completed successfully." : $"Finished with {failures} failed target(s).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkPendingTargetsCanceled();
            SetStatus("Build canceled");
            AppendLog("Build canceled by user. Partial output has been kept.");
        }
        catch (Exception ex)
        {
            SetStatus("Build failed");
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            _isBuilding = false;
            _stopBuildButton.Enabled = false;
            SetBuildOptionsEnabled(true);
            _rebuildButton.Enabled = _lastRootPath is not null && Directory.Exists(_lastRootPath);
            _buildCancellationTokenSource.Dispose();
            _buildCancellationTokenSource = null;
        }
    }

    private string GetBuildOutputPath(string rootPath)
    {
        string configuredPath = _outputFolderTextBox.Text.Trim();
        if (_useDefaultOutputFolder || string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(rootPath, "build");
        }

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
    }

    private void SetBuildOptionsEnabled(bool enabled)
    {
        _parallelBuildCountInput.Enabled = enabled;
        _outputFolderTextBox.Enabled = enabled;
        _browseOutputFolderButton.Enabled = enabled;
        _useDefaultOutputFolderButton.Enabled = enabled;
    }

    private void HandleLogTabDrawItem(object? sender, DrawItemEventArgs e)
    {
        TabPage tabPage = _logTabs.TabPages[e.Index];
        Rectangle bounds = e.Bounds;
        bool isSelected = _logTabs.SelectedIndex == e.Index;

        Color backColor = Color.FromArgb(42, 48, 57);
        Color foreColor = Color.FromArgb(222, 226, 234);

        if (tabPage.Tag is string status)
        {
            if (status.Equals("Success", StringComparison.OrdinalIgnoreCase))
            {
                backColor = Color.LightBlue;
                foreColor = Color.Black;
            }
            else if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                backColor = Color.Orange;
                foreColor = Color.Black;
            }
            else if (status.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
            {
                backColor = Color.Khaki;
                foreColor = Color.Black;
            }
        }

        if (isSelected)
        {
            bounds.Inflate(0, 2);
        }

        using SolidBrush backBrush = new(backColor);
        using SolidBrush foreBrush = new(foreColor);
        e.Graphics.FillRectangle(backBrush, bounds);

        TextRenderer.DrawText(
            e.Graphics,
            tabPage.Text,
            _logTabs.Font,
            bounds,
            foreColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
    }

    private async Task<int> ExecuteBuildAsync(
        string buildOutputPath,
        string[] targetPaths,
        int maxParallelBuilds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(buildOutputPath))
        {
            SetStatus("Deleting previous build folder...");
            AppendLog($"Deleting: {buildOutputPath}");
            Directory.Delete(buildOutputPath, true);
        }

        Directory.CreateDirectory(buildOutputPath);
        cancellationToken.ThrowIfCancellationRequested();

        int failures = 0;
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, maxParallelBuilds),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(targetPaths, parallelOptions, async (targetPath, targetCancellationToken) =>
        {
            bool succeeded = await BuildTargetAsync(targetPath, buildOutputPath, targetCancellationToken);
            if (!succeeded)
            {
                Interlocked.Increment(ref failures);
            }
        });

        return failures;
    }

    private static string[] FindBuildTargets(string rootPath, string buildOutputPath)
    {
        string[] projectPaths = Directory.EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(buildOutputPath, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (projectPaths.Length > 0)
        {
            return projectPaths
                .Where(IsExecutableProject)
                .ToArray();
        }

        return Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(buildOutputPath, path))
            .Where(IsSolutionFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string[] SelectTargetsToBuild(string rootPath, string[] targetPaths, string[]? preferredTargetPaths)
    {
        if (preferredTargetPaths is not null)
        {
            string[] existingPreferredPaths = preferredTargetPaths
                .Where(path => File.Exists(path) && IsBuildTargetFile(path))
                .Where(path => !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || IsExecutableProject(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (existingPreferredPaths.Length > 0)
            {
                AppendLog($"Using previous selection: {existingPreferredPaths.Length} target(s).");
                return existingPreferredPaths;
            }

            AppendLog("Warning: previous build target selection is no longer valid.");
        }

        if (targetPaths.Length == 1)
        {
            return targetPaths;
        }

        using BuildTargetSelectionDialog dialog = new(rootPath, targetPaths);
        return dialog.ShowDialog(this) == DialogResult.OK
            ? dialog.SelectedTargetPaths
            : Array.Empty<string>();
    }

    private async Task<bool> BuildTargetAsync(
        string targetPath,
        string buildOutputPath,
        CancellationToken cancellationToken)
    {
        string targetName = Path.GetFileNameWithoutExtension(targetPath);
        string outputFolderName = GetOutputFolderName(targetPath);
        bool targetSucceeded = false;
        bool targetCanceled = false;
        AppendLog("");
        AppendLog($"Target: {targetPath}");
        AppendTargetLog(targetPath, $"Target: {targetPath}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetStatus($"Cleaning {targetName}...");

            if (!await RunDotnetAsync(targetPath, cancellationToken, "clean", targetPath, "--nologo", "-v", "q"))
            {
                IncrementProgress();
                return false;
            }

            IncrementProgress();
            SetStatus($"Restoring {targetName}...");
            if (!await RunDotnetAsync(targetPath, cancellationToken, "restore", targetPath, "--nologo"))
            {
                IncrementProgress();
                return false;
            }

            IncrementProgress();
            foreach (string configuration in new[] { "Debug", "Release" })
            {
                SetStatus($"Building {targetName} ({configuration})...");
                string outputPath = Path.Combine(buildOutputPath, outputFolderName, configuration)
                    + Path.DirectorySeparatorChar;

                bool succeeded = await RunDotnetAsync(
                    targetPath,
                    cancellationToken,
                    "build",
                    targetPath,
                    "-c",
                    configuration,
                    "--nologo",
                    "-p:Platform=Any CPU",
                    $"-p:OutputPath={outputPath}");

                IncrementProgress();
                if (!succeeded)
                {
                    return false;
                }
            }

            AppendLog($"Completed: {targetName}");
            AppendTargetLog(targetPath, $"Completed: {targetName}");
            targetSucceeded = true;
            return true;
        }
        catch (OperationCanceledException)
        {
            targetCanceled = true;
            AppendTargetLog(targetPath, "Build canceled.");
            throw;
        }
        finally
        {
            MarkTargetTabCompleted(targetPath, targetCanceled ? "Canceled" : targetSucceeded ? "Success" : "Failed");
        }
    }

    private static string GetOutputFolderName(string targetPath)
    {
        if (targetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            string? projectDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(projectDirectory))
            {
                return new DirectoryInfo(projectDirectory).Name;
            }
        }

        return Path.GetFileNameWithoutExtension(targetPath);
    }

    private static bool IsBuildTargetFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExecutableProject(string projectPath)
    {
        try
        {
            XDocument document = XDocument.Load(projectPath);
            return document
                .Descendants()
                .Where(element => element.Name.LocalName.Equals("OutputType", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Value.Trim())
                .Any(value => value.Equals("Exe", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("WinExe", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSolutionFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderBuildOutput(string buildOutputPath, string path)
    {
        buildOutputPath = Path.GetFullPath(buildOutputPath);
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(buildOutputPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> RunDotnetAsync(
        string targetPath,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string commandLine = $"> dotnet {string.Join(" ", arguments.Select(QuoteForLog))}";
        AppendLog(commandLine);
        AppendTargetLog(targetPath, commandLine);
        Stopwatch stopwatch = Stopwatch.StartNew();

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        TaskCompletionSource<int> exitCodeSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += (_, e) => AppendProcessLine(targetPath, e.Data);
        process.ErrorDataReceived += (_, e) => AppendProcessLine(targetPath, e.Data);
        process.Exited += (_, _) => exitCodeSource.TrySetResult(process.ExitCode);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            string message = $"Failed to start dotnet: {ex.Message}";
            AppendLog(message);
            AppendTargetLog(targetPath, message);
            return false;
        }

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
            () => TryKillProcessTree(process));

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        int exitCode = await exitCodeSource.Task;
        stopwatch.Stop();
        cancellationToken.ThrowIfCancellationRequested();

        if (exitCode != 0)
        {
            string message = $"Command failed with exit code {exitCode}. Elapsed: {FormatElapsed(stopwatch.Elapsed)}";
            AppendLog(message);
            AppendTargetLog(targetPath, message);
            return false;
        }

        string completedMessage = $"Command completed. Elapsed: {FormatElapsed(stopwatch.Elapsed)}";
        AppendLog(completedMessage);
        AppendTargetLog(targetPath, completedMessage);
        return true;
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The process may have exited between the check and the kill request.
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.TotalMinutes >= 1
            ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s"
            : $"{elapsed.TotalSeconds:0.0}s";
    }

    private static string QuoteForLog(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }

    private void PrepareTargetLogTabs(string rootPath, string[] targetPaths)
    {
        if (InvokeRequired)
        {
            Invoke(() => PrepareTargetLogTabs(rootPath, targetPaths));
            return;
        }

        while (_logTabs.TabPages.Count > 1)
        {
            _logTabs.TabPages.RemoveAt(1);
        }

        _targetLogBoxes.Clear();
        _targetLogPages.Clear();
        foreach (string targetPath in targetPaths)
        {
            RichTextBox targetLogBox = CreateLogBox();
            TabPage tabPage = CreateLogTabPage(Path.GetFileNameWithoutExtension(targetPath), targetLogBox);
            _targetLogBoxes[targetPath] = targetLogBox;
            _targetLogPages[targetPath] = tabPage;
            _logTabs.TabPages.Add(tabPage);
            targetLogBox.AppendText(Path.GetRelativePath(rootPath, targetPath) + Environment.NewLine);
            targetLogBox.AppendText(new string('-', 80) + Environment.NewLine);
        }

        UpdateLogTabHeaderSize();
        _logTabs.Invalidate();
    }

    private void UpdateLogTabHeaderSize()
    {
        int maxWidth = 72;
        using Graphics graphics = _logTabs.CreateGraphics();

        foreach (TabPage tabPage in _logTabs.TabPages)
        {
            Size textSize = TextRenderer.MeasureText(graphics, tabPage.Text, _logTabs.Font);
            maxWidth = Math.Max(maxWidth, textSize.Width + 32);
        }

        _logTabs.ItemSize = new Size(maxWidth, 26);
    }

    private static TabPage CreateLogTabPage(string title, RichTextBox logBox)
    {
        TabPage tabPage = new(title)
        {
            BackColor = Color.FromArgb(14, 17, 22),
            Padding = new Padding(0)
        };
        tabPage.Controls.Add(logBox);
        return tabPage;
    }

    private static RichTextBox CreateLogBox()
    {
        return new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(14, 17, 22),
            ForeColor = Color.FromArgb(222, 226, 234),
            Font = new Font("Consolas", 10F),
            WordWrap = false,
            DetectUrls = false
        };
    }

    private void AppendProcessLine(string targetPath, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (_hideWarnings && IsWarningLine(line))
        {
            return;
        }

        AppendLog(line);
        AppendTargetLog(targetPath, line);
    }

    private static bool IsWarningLine(string line)
    {
        return line.Contains(": warning ", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" warning ", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" warning MSB", StringComparison.OrdinalIgnoreCase)
            || line.Contains("경고", StringComparison.OrdinalIgnoreCase);
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _logBox.AppendText(message + Environment.NewLine);
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private void AppendTargetLog(string targetPath, string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendTargetLog(targetPath, message));
            return;
        }

        if (!_targetLogBoxes.TryGetValue(targetPath, out RichTextBox? targetLogBox))
        {
            return;
        }

        targetLogBox.AppendText(message + Environment.NewLine);
        targetLogBox.SelectionStart = targetLogBox.TextLength;
        targetLogBox.ScrollToCaret();
    }

    private void MarkTargetTabCompleted(string targetPath, string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => MarkTargetTabCompleted(targetPath, status));
            return;
        }

        if (!_targetLogPages.TryGetValue(targetPath, out TabPage? tabPage))
        {
            return;
        }

        tabPage.Tag = status;
        _logTabs.Invalidate();
    }

    private void MarkPendingTargetsCanceled()
    {
        if (InvokeRequired)
        {
            BeginInvoke(MarkPendingTargetsCanceled);
            return;
        }

        foreach (TabPage tabPage in _targetLogPages.Values.Where(page => page.Tag is null))
        {
            tabPage.Tag = "Canceled";
        }

        _logTabs.Invalidate();
    }

    private void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
    }

    private void IncrementProgress()
    {
        if (InvokeRequired)
        {
            BeginInvoke(IncrementProgress);
            return;
        }

        if (_progressBar.Value < _progressBar.Maximum)
        {
            _progressBar.Value++;
        }
    }
}

public sealed class BuildTargetSelectionDialog : Form
{
    private readonly CheckedListBox _targetList;
    private readonly string[] _targetPaths;

    public BuildTargetSelectionDialog(string rootPath, string[] targetPaths)
    {
        _targetPaths = targetPaths;

        Text = "Select Build Targets";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 520);
        Size = new Size(860, 620);
        BackColor = Color.FromArgb(30, 34, 40);
        Font = new Font("Segoe UI", 10F);

        Label titleLabel = new()
        {
            Dock = DockStyle.Top,
            Height = 54,
            Padding = new Padding(18, 12, 18, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Text = "Select executable projects to build"
        };

        Label descriptionLabel = new()
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(18, 0, 18, 8),
            ForeColor = Color.FromArgb(183, 191, 204),
            Text = "Libraries are hidden. Unchecked executable projects are skipped."
        };

        _targetList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(14, 17, 22),
            BorderStyle = BorderStyle.None,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(222, 226, 234),
            Font = new Font("Consolas", 10F),
            HorizontalScrollbar = true,
            IntegralHeight = false
        };

        foreach (string targetPath in targetPaths)
        {
            _targetList.Items.Add(Path.GetRelativePath(rootPath, targetPath), false);
        }

        Button selectAllButton = CreateSecondaryButton("Select All");
        selectAllButton.Click += (_, _) => SetAllChecked(true);

        Button clearButton = CreateSecondaryButton("Clear");
        clearButton.Click += (_, _) => SetAllChecked(false);

        Button cancelButton = CreateSecondaryButton("Cancel");
        cancelButton.DialogResult = DialogResult.Cancel;

        Button buildButton = CreatePrimaryButton("Build Selected");
        buildButton.Click += (_, _) =>
        {
            if (SelectedTargetPaths.Length == 0)
            {
                MessageBox.Show(this, "Select at least one build target.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
        };

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(18, 10, 18, 12),
            BackColor = Color.FromArgb(42, 48, 57)
        };
        buttonPanel.Controls.Add(buildButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(clearButton);
        buttonPanel.Controls.Add(selectAllButton);

        Controls.Add(_targetList);
        Controls.Add(descriptionLabel);
        Controls.Add(titleLabel);
        Controls.Add(buttonPanel);

        AcceptButton = buildButton;
        CancelButton = cancelButton;
    }

    public string[] SelectedTargetPaths
    {
        get
        {
            return _targetList.CheckedIndices
                .Cast<int>()
                .Select(index => _targetPaths[index])
                .ToArray();
        }
    }

    private void SetAllChecked(bool isChecked)
    {
        for (int i = 0; i < _targetList.Items.Count; i++)
        {
            _targetList.SetItemChecked(i, isChecked);
        }
    }

    private static Button CreatePrimaryButton(string text)
    {
        Button button = CreateSecondaryButton(text);
        button.BackColor = Color.FromArgb(72, 98, 140);
        button.ForeColor = Color.White;
        button.Width = 132;
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        Button button = new()
        {
            BackColor = Color.FromArgb(58, 65, 76),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(232, 236, 244),
            Height = 32,
            Margin = new Padding(8, 0, 0, 0),
            Text = text,
            UseVisualStyleBackColor = false,
            Width = 96
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}

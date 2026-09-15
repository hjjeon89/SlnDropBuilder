using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace SlnDropBuilder;

public sealed class Form1 : Form
{
    private readonly Panel _dropPanel;
    private readonly Label _dropTitleLabel;
    private readonly Label _dropPathLabel;
    private readonly Button _rebuildButton;
    private readonly CheckBox _hideWarningsCheckBox;
    private readonly Label _parallelBuildCountLabel;
    private readonly NumericUpDown _parallelBuildCountInput;
    private readonly TabControl _logTabs;
    private readonly RichTextBox _logBox;
    private readonly ProgressBar _progressBar;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private string? _lastRootPath;
    private string[]? _lastSelectedTargetPaths;
    private readonly Dictionary<string, RichTextBox> _targetLogBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TabPage> _targetLogPages = new(StringComparer.OrdinalIgnoreCase);
    private bool _hideWarnings = true;
    private bool _isBuilding;

    public Form1()
    {
        Text = "Sln Drop Builder";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 620);
        Size = new Size(1080, 720);
        BackColor = Color.FromArgb(30, 34, 40);
        Font = new Font("Segoe UI", 10F);
        AllowDrop = true;

        _dropPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            BackColor = Color.FromArgb(42, 48, 57),
            Padding = new Padding(24),
            AllowDrop = true
        };

        _dropTitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            Text = "Drop a folder to build all child solutions",
            TextAlign = ContentAlignment.MiddleCenter
        };

        _dropPathLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(183, 191, 204),
            Font = new Font("Segoe UI", 10.5F),
            Text = "Outputs are written to build\\{Project}\\Debug or Release under the dropped folder.",
            TextAlign = ContentAlignment.MiddleCenter
        };

        _rebuildButton = new Button
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            BackColor = Color.FromArgb(72, 98, 140),
            AllowDrop = true,
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Location = new Point(_dropPanel.Width - 150, _dropPanel.Height - 48),
            Size = new Size(126, 32),
            TabIndex = 0,
            Text = "Rebuild",
            UseVisualStyleBackColor = false
        };
        _rebuildButton.FlatAppearance.BorderSize = 0;

        _hideWarningsCheckBox = new CheckBox
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            AutoSize = true,
            BackColor = Color.FromArgb(42, 48, 57),
            Checked = true,
            ForeColor = Color.FromArgb(222, 226, 234),
            TabIndex = 1,
            Text = "Hide warnings",
            UseVisualStyleBackColor = false
        };

        _parallelBuildCountLabel = new Label
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            AutoSize = true,
            BackColor = Color.FromArgb(42, 48, 57),
            ForeColor = Color.FromArgb(222, 226, 234),
            Text = "Max parallel"
        };

        _parallelBuildCountInput = new NumericUpDown
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            BackColor = Color.FromArgb(14, 17, 22),
            ForeColor = Color.FromArgb(222, 226, 234),
            Minimum = 1,
            Maximum = Math.Max(1, Environment.ProcessorCount),
            Value = Math.Min(2, Math.Max(1, Environment.ProcessorCount)),
            Width = 52
        };

        _logTabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            HotTrack = true,
            Multiline = false,
            SizeMode = TabSizeMode.Fixed
        };

        _logBox = CreateLogBox();
        _logTabs.TabPages.Add(CreateLogTabPage("All", _logBox));
        UpdateLogTabHeaderSize();

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 18,
            Style = ProgressBarStyle.Continuous
        };

        _statusLabel = new ToolStripStatusLabel("Ready")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(42, 48, 57),
            ForeColor = Color.White,
            SizingGrip = false
        };
        _statusStrip.Items.Add(_statusLabel);

        _dropPanel.Controls.Add(_rebuildButton);
        _dropPanel.Controls.Add(_hideWarningsCheckBox);
        _dropPanel.Controls.Add(_parallelBuildCountInput);
        _dropPanel.Controls.Add(_parallelBuildCountLabel);
        _dropPanel.Controls.Add(_dropPathLabel);
        _dropPanel.Controls.Add(_dropTitleLabel);
        _rebuildButton.BringToFront();
        _hideWarningsCheckBox.BringToFront();
        _parallelBuildCountInput.BringToFront();
        _parallelBuildCountLabel.BringToFront();
        Controls.Add(_logTabs);
        Controls.Add(_progressBar);
        Controls.Add(_statusStrip);
        Controls.Add(_dropPanel);

        DragEnter += HandleDragEnter;
        DragDrop += HandleDragDrop;
        _dropPanel.DragEnter += HandleDragEnter;
        _dropPanel.DragDrop += HandleDragDrop;
        _dropPanel.Resize += (_, _) => UpdateRebuildButtonLocation();
        _logTabs.DrawItem += HandleLogTabDrawItem;
        _rebuildButton.DragEnter += HandleDragEnter;
        _rebuildButton.DragDrop += HandleDragDrop;
        _rebuildButton.Click += HandleRebuildButtonClick;
        _hideWarningsCheckBox.CheckedChanged += (_, _) => _hideWarnings = _hideWarningsCheckBox.Checked;

        UpdateRebuildButtonLocation();
        AppendLog("Ready. Drop a root folder that contains C# projects or solutions.");
        AppendLog("If multiple executable projects are found, choose only the projects that are ready to build.");
    }

    private void UpdateRebuildButtonLocation()
    {
        _rebuildButton.Location = new Point(
            Math.Max(24, _dropPanel.ClientSize.Width - _rebuildButton.Width - 24),
            Math.Max(72, _dropPanel.ClientSize.Height - _rebuildButton.Height - 16));

        _hideWarningsCheckBox.Location = new Point(
            Math.Max(24, _rebuildButton.Left - _hideWarningsCheckBox.Width - 18),
            _rebuildButton.Top + 6);

        _parallelBuildCountInput.Location = new Point(
            Math.Max(24, _hideWarningsCheckBox.Left - _parallelBuildCountInput.Width - 24),
            _rebuildButton.Top + 3);

        _parallelBuildCountLabel.Location = new Point(
            Math.Max(24, _parallelBuildCountInput.Left - _parallelBuildCountLabel.Width - 8),
            _rebuildButton.Top + 6);
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
        _lastRootPath = rootPath;
        _rebuildButton.Enabled = false;
        _progressBar.Value = 0;
        SetStatus("Scanning solutions...");
        _dropPathLabel.Text = rootPath;
        AppendLog("");
        AppendLog($"Root: {rootPath}");

        try
        {
            string buildOutputPath = Path.Combine(rootPath, "build");

            string[] targetPaths = await Task.Run(() => FindBuildTargets(rootPath));

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
            SetStatus("Build is running in the background...");

            int failures = await Task.Run(() => ExecuteBuildAsync(buildOutputPath, targetPaths, maxParallelBuilds));

            SetStatus(failures == 0
                ? "Build completed successfully"
                : $"Build completed with {failures} failed target(s)");
            AppendLog(failures == 0 ? "All builds completed successfully." : $"Finished with {failures} failed target(s).");
        }
        catch (Exception ex)
        {
            SetStatus("Build failed");
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            _isBuilding = false;
            _rebuildButton.Enabled = _lastRootPath is not null && Directory.Exists(_lastRootPath);
        }
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

    private async Task<int> ExecuteBuildAsync(string buildOutputPath, string[] targetPaths, int maxParallelBuilds)
    {
        if (Directory.Exists(buildOutputPath))
        {
            SetStatus("Deleting previous build folder...");
            AppendLog($"Deleting: {buildOutputPath}");
            Directory.Delete(buildOutputPath, true);
        }

        Directory.CreateDirectory(buildOutputPath);

        int failures = 0;
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, maxParallelBuilds)
        };

        await Parallel.ForEachAsync(targetPaths, parallelOptions, async (targetPath, _) =>
        {
            bool succeeded = await BuildTargetAsync(targetPath, buildOutputPath);
            if (!succeeded)
            {
                Interlocked.Increment(ref failures);
            }
        });

        return failures;
    }

    private static string[] FindBuildTargets(string rootPath)
    {
        string[] projectPaths = Directory.EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(rootPath, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (projectPaths.Length > 0)
        {
            return projectPaths
                .Where(IsExecutableProject)
                .ToArray();
        }

        return Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(rootPath, path))
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

    private async Task<bool> BuildTargetAsync(string targetPath, string buildOutputPath)
    {
        string targetName = Path.GetFileNameWithoutExtension(targetPath);
        string outputFolderName = GetOutputFolderName(targetPath);
        bool targetSucceeded = false;
        AppendLog("");
        AppendLog($"Target: {targetPath}");
        AppendTargetLog(targetPath, $"Target: {targetPath}");

        try
        {
            SetStatus($"Cleaning {targetName}...");

            if (!await RunDotnetAsync(targetPath, "clean", targetPath, "--nologo", "-v", "q"))
            {
                IncrementProgress();
                return false;
            }

            IncrementProgress();
            SetStatus($"Restoring {targetName}...");
            if (!await RunDotnetAsync(targetPath, "restore", targetPath, "--nologo"))
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
        finally
        {
            MarkTargetTabCompleted(targetPath, targetSucceeded);
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

    private static bool IsUnderBuildOutput(string rootPath, string path)
    {
        string buildOutputPath = Path.GetFullPath(Path.Combine(rootPath, "build"));
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(buildOutputPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> RunDotnetAsync(string targetPath, params string[] arguments)
    {
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

        TaskCompletionSource<int> exitCodeSource = new();
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

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        int exitCode = await exitCodeSource.Task;
        stopwatch.Stop();

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

    private void MarkTargetTabCompleted(string targetPath, bool succeeded)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => MarkTargetTabCompleted(targetPath, succeeded));
            return;
        }

        if (!_targetLogPages.TryGetValue(targetPath, out TabPage? tabPage))
        {
            return;
        }

        tabPage.Tag = succeeded ? "Success" : "Failed";
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
            _targetList.Items.Add(Path.GetRelativePath(rootPath, targetPath), true);
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

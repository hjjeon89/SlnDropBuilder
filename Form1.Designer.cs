#nullable enable

namespace SlnDropBuilder;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _dropPanel = new Panel();
        _rebuildButton = new Button();
        _stopBuildButton = new Button();
        _hideWarningsCheckBox = new CheckBox();
        _parallelBuildCountInput = new NumericUpDown();
        _parallelBuildCountLabel = new Label();
        _parallelBuildWarningLabel = new Label();
        _outputFolderLabel = new Label();
        _outputFolderTextBox = new TextBox();
        _browseOutputFolderButton = new Button();
        _useDefaultOutputFolderButton = new Button();
        _dropPathLabel = new Label();
        _dropTitleLabel = new Label();
        _logTabs = new TabControl();
        _allLogTabPage = new TabPage();
        _logBox = new RichTextBox();
        _progressBar = new ProgressBar();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _dropPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_parallelBuildCountInput).BeginInit();
        _logTabs.SuspendLayout();
        _allLogTabPage.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // _dropPanel
        // 
        _dropPanel.AllowDrop = true;
        _dropPanel.BackColor = Color.FromArgb(42, 48, 57);
        _dropPanel.Controls.Add(_rebuildButton);
        _dropPanel.Controls.Add(_stopBuildButton);
        _dropPanel.Controls.Add(_hideWarningsCheckBox);
        _dropPanel.Controls.Add(_parallelBuildCountInput);
        _dropPanel.Controls.Add(_parallelBuildCountLabel);
        _dropPanel.Controls.Add(_parallelBuildWarningLabel);
        _dropPanel.Controls.Add(_outputFolderLabel);
        _dropPanel.Controls.Add(_outputFolderTextBox);
        _dropPanel.Controls.Add(_browseOutputFolderButton);
        _dropPanel.Controls.Add(_useDefaultOutputFolderButton);
        _dropPanel.Controls.Add(_dropPathLabel);
        _dropPanel.Controls.Add(_dropTitleLabel);
        _dropPanel.Dock = DockStyle.Top;
        _dropPanel.Location = new Point(0, 0);
        _dropPanel.Name = "_dropPanel";
        _dropPanel.Padding = new Padding(24);
        _dropPanel.Size = new Size(1064, 229);
        _dropPanel.TabIndex = 0;
        _dropPanel.DragDrop += HandleDragDrop;
        _dropPanel.DragEnter += HandleDragEnter;
        // 
        // _rebuildButton
        // 
        _rebuildButton.AllowDrop = true;
        _rebuildButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _rebuildButton.BackColor = Color.FromArgb(72, 98, 140);
        _rebuildButton.Enabled = false;
        _rebuildButton.FlatAppearance.BorderSize = 0;
        _rebuildButton.FlatStyle = FlatStyle.Flat;
        _rebuildButton.ForeColor = Color.White;
        _rebuildButton.Location = new Point(911, 103);
        _rebuildButton.Name = "_rebuildButton";
        _rebuildButton.Size = new Size(126, 32);
        _rebuildButton.TabIndex = 0;
        _rebuildButton.Text = "Rebuild";
        _rebuildButton.UseVisualStyleBackColor = false;
        _rebuildButton.Click += HandleRebuildButtonClick;
        _rebuildButton.DragDrop += HandleDragDrop;
        _rebuildButton.DragEnter += HandleDragEnter;
        // 
        // _stopBuildButton
        // 
        _stopBuildButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _stopBuildButton.BackColor = Color.FromArgb(140, 62, 62);
        _stopBuildButton.Enabled = false;
        _stopBuildButton.FlatAppearance.BorderSize = 0;
        _stopBuildButton.FlatStyle = FlatStyle.Flat;
        _stopBuildButton.ForeColor = Color.White;
        _stopBuildButton.Location = new Point(777, 103);
        _stopBuildButton.Name = "_stopBuildButton";
        _stopBuildButton.Size = new Size(126, 32);
        _stopBuildButton.TabIndex = 1;
        _stopBuildButton.Text = "Stop Build";
        _stopBuildButton.UseVisualStyleBackColor = false;
        _stopBuildButton.Click += HandleStopBuildButtonClick;
        // 
        // _hideWarningsCheckBox
        // 
        _hideWarningsCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _hideWarningsCheckBox.AutoSize = true;
        _hideWarningsCheckBox.BackColor = Color.FromArgb(42, 48, 57);
        _hideWarningsCheckBox.Checked = true;
        _hideWarningsCheckBox.CheckState = CheckState.Checked;
        _hideWarningsCheckBox.ForeColor = Color.FromArgb(222, 226, 234);
        _hideWarningsCheckBox.Location = new Point(644, 109);
        _hideWarningsCheckBox.Name = "_hideWarningsCheckBox";
        _hideWarningsCheckBox.Size = new Size(115, 23);
        _hideWarningsCheckBox.TabIndex = 2;
        _hideWarningsCheckBox.Text = "Hide warnings";
        _hideWarningsCheckBox.UseVisualStyleBackColor = false;
        _hideWarningsCheckBox.CheckedChanged += HandleHideWarningsCheckedChanged;
        // 
        // _parallelBuildCountInput
        // 
        _parallelBuildCountInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _parallelBuildCountInput.BackColor = Color.FromArgb(14, 17, 22);
        _parallelBuildCountInput.ForeColor = Color.FromArgb(222, 226, 234);
        _parallelBuildCountInput.Location = new Point(577, 106);
        _parallelBuildCountInput.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
        _parallelBuildCountInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _parallelBuildCountInput.Name = "_parallelBuildCountInput";
        _parallelBuildCountInput.Size = new Size(52, 25);
        _parallelBuildCountInput.TabIndex = 3;
        _parallelBuildCountInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // _parallelBuildCountLabel
        // 
        _parallelBuildCountLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _parallelBuildCountLabel.AutoSize = true;
        _parallelBuildCountLabel.BackColor = Color.FromArgb(42, 48, 57);
        _parallelBuildCountLabel.ForeColor = Color.FromArgb(222, 226, 234);
        _parallelBuildCountLabel.Location = new Point(486, 109);
        _parallelBuildCountLabel.Name = "_parallelBuildCountLabel";
        _parallelBuildCountLabel.Size = new Size(82, 19);
        _parallelBuildCountLabel.TabIndex = 4;
        _parallelBuildCountLabel.Text = "Max parallel";
        // 
        // _parallelBuildWarningLabel
        // 
        _parallelBuildWarningLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _parallelBuildWarningLabel.AutoEllipsis = true;
        _parallelBuildWarningLabel.ForeColor = Color.FromArgb(255, 190, 92);
        _parallelBuildWarningLabel.Location = new Point(21, 141);
        _parallelBuildWarningLabel.Name = "_parallelBuildWarningLabel";
        _parallelBuildWarningLabel.Size = new Size(1016, 24);
        _parallelBuildWarningLabel.TabIndex = 4;
        _parallelBuildWarningLabel.Text = "Warning: parallel targets sharing a referenced project may fail with CS2012 (file in use). Set Max parallel to 1 to avoid this.";
        _parallelBuildWarningLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _outputFolderLabel
        // 
        _outputFolderLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _outputFolderLabel.ForeColor = Color.FromArgb(222, 226, 234);
        _outputFolderLabel.Location = new Point(21, 177);
        _outputFolderLabel.Name = "_outputFolderLabel";
        _outputFolderLabel.Size = new Size(94, 24);
        _outputFolderLabel.TabIndex = 5;
        _outputFolderLabel.Text = "Output path";
        _outputFolderLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _outputFolderTextBox
        // 
        _outputFolderTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _outputFolderTextBox.BackColor = Color.FromArgb(14, 17, 22);
        _outputFolderTextBox.BorderStyle = BorderStyle.FixedSingle;
        _outputFolderTextBox.ForeColor = Color.FromArgb(222, 226, 234);
        _outputFolderTextBox.Location = new Point(123, 176);
        _outputFolderTextBox.Name = "_outputFolderTextBox";
        _outputFolderTextBox.PlaceholderText = "Drop a folder to set the default build path";
        _outputFolderTextBox.ReadOnly = true;
        _outputFolderTextBox.Size = new Size(706, 25);
        _outputFolderTextBox.TabIndex = 6;
        // 
        // _browseOutputFolderButton
        // 
        _browseOutputFolderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _browseOutputFolderButton.BackColor = Color.FromArgb(58, 65, 76);
        _browseOutputFolderButton.FlatAppearance.BorderSize = 0;
        _browseOutputFolderButton.FlatStyle = FlatStyle.Flat;
        _browseOutputFolderButton.ForeColor = Color.FromArgb(232, 236, 244);
        _browseOutputFolderButton.Location = new Point(837, 173);
        _browseOutputFolderButton.Name = "_browseOutputFolderButton";
        _browseOutputFolderButton.Size = new Size(88, 32);
        _browseOutputFolderButton.TabIndex = 7;
        _browseOutputFolderButton.Text = "Browse...";
        _browseOutputFolderButton.UseVisualStyleBackColor = false;
        _browseOutputFolderButton.Click += HandleBrowseOutputFolderButtonClick;
        // 
        // _useDefaultOutputFolderButton
        // 
        _useDefaultOutputFolderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _useDefaultOutputFolderButton.BackColor = Color.FromArgb(58, 65, 76);
        _useDefaultOutputFolderButton.FlatAppearance.BorderSize = 0;
        _useDefaultOutputFolderButton.FlatStyle = FlatStyle.Flat;
        _useDefaultOutputFolderButton.ForeColor = Color.FromArgb(232, 236, 244);
        _useDefaultOutputFolderButton.Location = new Point(933, 173);
        _useDefaultOutputFolderButton.Name = "_useDefaultOutputFolderButton";
        _useDefaultOutputFolderButton.Size = new Size(104, 32);
        _useDefaultOutputFolderButton.TabIndex = 8;
        _useDefaultOutputFolderButton.Text = "Use Default";
        _useDefaultOutputFolderButton.UseVisualStyleBackColor = false;
        _useDefaultOutputFolderButton.Click += HandleUseDefaultOutputFolderButtonClick;
        // 
        // _dropPathLabel
        // 
        _dropPathLabel.Font = new Font("Segoe UI", 10.5F);
        _dropPathLabel.ForeColor = Color.FromArgb(183, 191, 204);
        _dropPathLabel.Location = new Point(24, 68);
        _dropPathLabel.Name = "_dropPathLabel";
        _dropPathLabel.Size = new Size(1016, 38);
        _dropPathLabel.TabIndex = 9;
        _dropPathLabel.Text = "Drop a folder, then choose the output path below. Debug and Release are created per project.";
        _dropPathLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // _dropTitleLabel
        // 
        _dropTitleLabel.Dock = DockStyle.Top;
        _dropTitleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        _dropTitleLabel.ForeColor = Color.White;
        _dropTitleLabel.Location = new Point(24, 24);
        _dropTitleLabel.Name = "_dropTitleLabel";
        _dropTitleLabel.Size = new Size(1016, 44);
        _dropTitleLabel.TabIndex = 10;
        _dropTitleLabel.Text = "Drop a folder to build all child solutions";
        _dropTitleLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // _logTabs
        // 
        _logTabs.Controls.Add(_allLogTabPage);
        _logTabs.Dock = DockStyle.Fill;
        _logTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _logTabs.HotTrack = true;
        _logTabs.Location = new Point(0, 229);
        _logTabs.Name = "_logTabs";
        _logTabs.SelectedIndex = 0;
        _logTabs.Size = new Size(1064, 412);
        _logTabs.SizeMode = TabSizeMode.Fixed;
        _logTabs.TabIndex = 1;
        _logTabs.DrawItem += HandleLogTabDrawItem;
        // 
        // _allLogTabPage
        // 
        _allLogTabPage.BackColor = Color.FromArgb(14, 17, 22);
        _allLogTabPage.Controls.Add(_logBox);
        _allLogTabPage.Location = new Point(4, 26);
        _allLogTabPage.Name = "_allLogTabPage";
        _allLogTabPage.Size = new Size(1056, 382);
        _allLogTabPage.TabIndex = 0;
        _allLogTabPage.Text = "All";
        // 
        // _logBox
        // 
        _logBox.BackColor = Color.FromArgb(14, 17, 22);
        _logBox.BorderStyle = BorderStyle.None;
        _logBox.DetectUrls = false;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 10F);
        _logBox.ForeColor = Color.FromArgb(222, 226, 234);
        _logBox.Location = new Point(0, 0);
        _logBox.Name = "_logBox";
        _logBox.ReadOnly = true;
        _logBox.Size = new Size(1056, 382);
        _logBox.TabIndex = 0;
        _logBox.Text = "";
        _logBox.WordWrap = false;
        // 
        // _progressBar
        // 
        _progressBar.Dock = DockStyle.Bottom;
        _progressBar.Location = new Point(0, 641);
        _progressBar.Name = "_progressBar";
        _progressBar.Size = new Size(1064, 18);
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.TabIndex = 2;
        // 
        // _statusStrip
        // 
        _statusStrip.BackColor = Color.FromArgb(42, 48, 57);
        _statusStrip.ForeColor = Color.White;
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel });
        _statusStrip.Location = new Point(0, 659);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new Size(1064, 22);
        _statusStrip.SizingGrip = false;
        _statusStrip.TabIndex = 3;
        // 
        // _statusLabel
        // 
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(1049, 17);
        _statusLabel.Spring = true;
        _statusLabel.Text = "Ready";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // Form1
        // 
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(30, 34, 40);
        ClientSize = new Size(1064, 681);
        Controls.Add(_logTabs);
        Controls.Add(_progressBar);
        Controls.Add(_statusStrip);
        Controls.Add(_dropPanel);
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(920, 620);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sln Drop Builder";
        DragDrop += HandleDragDrop;
        DragEnter += HandleDragEnter;
        _dropPanel.ResumeLayout(false);
        _dropPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_parallelBuildCountInput).EndInit();
        _logTabs.ResumeLayout(false);
        _allLogTabPage.ResumeLayout(false);
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private Panel _dropPanel = null!;
    private Label _dropTitleLabel = null!;
    private Label _dropPathLabel = null!;
    private Button _rebuildButton = null!;
    private Button _stopBuildButton = null!;
    private CheckBox _hideWarningsCheckBox = null!;
    private Label _parallelBuildCountLabel = null!;
    private NumericUpDown _parallelBuildCountInput = null!;
    private Label _parallelBuildWarningLabel = null!;
    private Label _outputFolderLabel = null!;
    private TextBox _outputFolderTextBox = null!;
    private Button _browseOutputFolderButton = null!;
    private Button _useDefaultOutputFolderButton = null!;
    private TabControl _logTabs = null!;
    private TabPage _allLogTabPage = null!;
    private RichTextBox _logBox = null!;
    private ProgressBar _progressBar = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
}

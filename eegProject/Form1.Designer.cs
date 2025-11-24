namespace eegProject
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPageUsers = new System.Windows.Forms.TabPage();
            this.gridUsers = new System.Windows.Forms.DataGridView();
            this.panelUsersActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefreshUsers = new System.Windows.Forms.Button();
            this.btnAddUser = new System.Windows.Forms.Button();
            this.btnEditUser = new System.Windows.Forms.Button();
            this.btnDeleteUser = new System.Windows.Forms.Button();
            this.btnResetPassword = new System.Windows.Forms.Button();
            this.tabPageSessions = new System.Windows.Forms.TabPage();
            this.gridSessions = new System.Windows.Forms.DataGridView();
            this.panelSessionsActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefreshSessions = new System.Windows.Forms.Button();
            this.btnAddSession = new System.Windows.Forms.Button();
            this.btnEditSession = new System.Windows.Forms.Button();
            this.btnDeleteSession = new System.Windows.Forms.Button();
            this.btnManageExperimentTypes = new System.Windows.Forms.Button();
            this.btnManageTimeLabels = new System.Windows.Forms.Button();
            this.tabPageEEG = new System.Windows.Forms.TabPage();
            this.gridEEG = new System.Windows.Forms.DataGridView();
            this.panelEEGActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefreshEEG = new System.Windows.Forms.Button();
            this.cmbEegSessions = new System.Windows.Forms.ComboBox();
            this.btnStreamMonitor = new System.Windows.Forms.Button();
            this.btnStopStream = new System.Windows.Forms.Button();
            this.lblStreamStatus = new System.Windows.Forms.Label();
            this.tabPageAnalysis = new System.Windows.Forms.TabPage();
            this.gridAnalyses = new System.Windows.Forms.DataGridView();
            this.panelAnalysisActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefreshAnalysis = new System.Windows.Forms.Button();
            this.btnTriggerAnalysis = new System.Windows.Forms.Button();
            this.btnBatchComparison = new System.Windows.Forms.Button();
            this.btnViewMetrics = new System.Windows.Forms.Button();
            this.btnDeleteAnalysis = new System.Windows.Forms.Button();
            this.tabPageSinav = new System.Windows.Forms.TabPage();
            this.tabPageUserNotes = new System.Windows.Forms.TabPage();
            this.splitUserNotes = new System.Windows.Forms.SplitContainer();
            this.gridUsersForNotes = new System.Windows.Forms.DataGridView();
            this.panelNotesRight = new System.Windows.Forms.Panel();
            this.txtUserNotes = new System.Windows.Forms.TextBox();
            this.panelNotesActions = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNotesUserName = new System.Windows.Forms.Label();
            this.btnSaveNotes = new System.Windows.Forms.Button();
            this.tabPageExport = new System.Windows.Forms.TabPage();
            this.panelExport = new System.Windows.Forms.FlowLayoutPanel();
            this.grpExportFilters = new System.Windows.Forms.GroupBox();
            this.lblExportUser = new System.Windows.Forms.Label();
            this.cmbExportUser = new System.Windows.Forms.ComboBox();
            this.lblExportScope = new System.Windows.Forms.Label();
            this.cmbExportScope = new System.Windows.Forms.ComboBox();
            this.lblExportSession = new System.Windows.Forms.Label();
            this.cmbExportSession = new System.Windows.Forms.ComboBox();
            this.lblExportExperiment = new System.Windows.Forms.Label();
            this.cmbExportExperiment = new System.Windows.Forms.ComboBox();
            this.chkAllTimeLabels = new System.Windows.Forms.CheckBox();
            this.lstExportTimeLabels = new System.Windows.Forms.CheckedListBox();
            this.chkMultiUserSheets = new System.Windows.Forms.CheckBox();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnExportJson = new System.Windows.Forms.Button();
            this.tabPageLogs = new System.Windows.Forms.TabPage();
            this.gridLogs = new System.Windows.Forms.DataGridView();
            this.panelLogsActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefreshLogs = new System.Windows.Forms.Button();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.tabMain.SuspendLayout();
            this.tabPageUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).BeginInit();
            this.panelUsersActions.SuspendLayout();
            this.tabPageSessions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSessions)).BeginInit();
            this.panelSessionsActions.SuspendLayout();
            this.tabPageEEG.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridEEG)).BeginInit();
            this.panelEEGActions.SuspendLayout();
            this.tabPageAnalysis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyses)).BeginInit();
            this.panelAnalysisActions.SuspendLayout();
            this.tabPageUserNotes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitUserNotes)).BeginInit();
            this.splitUserNotes.Panel1.SuspendLayout();
            this.splitUserNotes.Panel2.SuspendLayout();
            this.splitUserNotes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsersForNotes)).BeginInit();
            this.panelNotesRight.SuspendLayout();
            this.panelNotesActions.SuspendLayout();
            this.tabPageExport.SuspendLayout();
            this.panelExport.SuspendLayout();
            this.grpExportFilters.SuspendLayout();
            this.tabPageLogs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLogs)).BeginInit();
            this.panelLogsActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPageUsers);
            this.tabMain.Controls.Add(this.tabPageSessions);
            this.tabMain.Controls.Add(this.tabPageEEG);
            this.tabMain.Controls.Add(this.tabPageAnalysis);
            this.tabMain.Controls.Add(this.tabPageSinav);
            this.tabMain.Controls.Add(this.tabPageUserNotes);
            this.tabMain.Controls.Add(this.tabPageExport);
            this.tabMain.Controls.Add(this.tabPageLogs);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1164, 761);
            this.tabMain.TabIndex = 0;
            // 
            // tabPageUsers
            // 
            this.tabPageUsers.Controls.Add(this.gridUsers);
            this.tabPageUsers.Controls.Add(this.panelUsersActions);
            this.tabPageUsers.Location = new System.Drawing.Point(4, 25);
            this.tabPageUsers.Name = "tabPageUsers";
            this.tabPageUsers.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUsers.Size = new System.Drawing.Size(1156, 732);
            this.tabPageUsers.TabIndex = 0;
            this.tabPageUsers.Text = "Kullanicilar";
            this.tabPageUsers.UseVisualStyleBackColor = true;
            // 
            // gridUsers
            // 
            this.gridUsers.AllowUserToAddRows = false;
            this.gridUsers.AllowUserToDeleteRows = false;
            this.gridUsers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridUsers.Location = new System.Drawing.Point(3, 54);
            this.gridUsers.MultiSelect = false;
            this.gridUsers.Name = "gridUsers";
            this.gridUsers.ReadOnly = true;
            this.gridUsers.RowHeadersVisible = false;
            this.gridUsers.RowHeadersWidth = 51;
            this.gridUsers.RowTemplate.Height = 28;
            this.gridUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridUsers.Size = new System.Drawing.Size(1150, 675);
            this.gridUsers.TabIndex = 1;
            this.gridUsers.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridUsers_CellContentClick);
            // 
            // panelUsersActions
            // 
            this.panelUsersActions.AutoSize = true;
            this.panelUsersActions.Controls.Add(this.btnRefreshUsers);
            this.panelUsersActions.Controls.Add(this.btnAddUser);
            this.panelUsersActions.Controls.Add(this.btnEditUser);
            this.panelUsersActions.Controls.Add(this.btnDeleteUser);
            this.panelUsersActions.Controls.Add(this.btnResetPassword);
            this.panelUsersActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelUsersActions.Location = new System.Drawing.Point(3, 3);
            this.panelUsersActions.Name = "panelUsersActions";
            this.panelUsersActions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.panelUsersActions.Size = new System.Drawing.Size(1150, 51);
            this.panelUsersActions.TabIndex = 0;
            this.panelUsersActions.Paint += new System.Windows.Forms.PaintEventHandler(this.panelUsersActions_Paint);
            // 
            // btnRefreshUsers
            // 
            this.btnRefreshUsers.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshUsers.Name = "btnRefreshUsers";
            this.btnRefreshUsers.Size = new System.Drawing.Size(120, 40);
            this.btnRefreshUsers.TabIndex = 0;
            this.btnRefreshUsers.Text = "Yenile";
            this.btnRefreshUsers.UseVisualStyleBackColor = true;
            this.btnRefreshUsers.Click += new System.EventHandler(this.btnRefreshUsers_Click);
            // 
            // btnAddUser
            // 
            this.btnAddUser.Location = new System.Drawing.Point(129, 3);
            this.btnAddUser.Name = "btnAddUser";
            this.btnAddUser.Size = new System.Drawing.Size(130, 40);
            this.btnAddUser.TabIndex = 1;
            this.btnAddUser.Text = "Yeni Kullanici";
            this.btnAddUser.UseVisualStyleBackColor = true;
            this.btnAddUser.Click += new System.EventHandler(this.btnAddUser_Click);
            // 
            // btnEditUser
            // 
            this.btnEditUser.Location = new System.Drawing.Point(265, 3);
            this.btnEditUser.Name = "btnEditUser";
            this.btnEditUser.Size = new System.Drawing.Size(130, 40);
            this.btnEditUser.TabIndex = 2;
            this.btnEditUser.Text = "Duzenle";
            this.btnEditUser.UseVisualStyleBackColor = true;
            this.btnEditUser.Click += new System.EventHandler(this.btnEditUser_Click);
            // 
            // btnDeleteUser
            // 
            this.btnDeleteUser.Location = new System.Drawing.Point(401, 3);
            this.btnDeleteUser.Name = "btnDeleteUser";
            this.btnDeleteUser.Size = new System.Drawing.Size(130, 40);
            this.btnDeleteUser.TabIndex = 3;
            this.btnDeleteUser.Text = "Sil";
            this.btnDeleteUser.UseVisualStyleBackColor = true;
            this.btnDeleteUser.Click += new System.EventHandler(this.btnDeleteUser_Click);
            // 
            // btnResetPassword
            // 
            this.btnResetPassword.Location = new System.Drawing.Point(537, 3);
            this.btnResetPassword.Name = "btnResetPassword";
            this.btnResetPassword.Size = new System.Drawing.Size(150, 40);
            this.btnResetPassword.TabIndex = 4;
            this.btnResetPassword.Text = "Parola Sifirla";
            this.btnResetPassword.UseVisualStyleBackColor = true;
            this.btnResetPassword.Click += new System.EventHandler(this.btnResetPassword_Click);
            // 
            // tabPageSessions
            // 
            this.tabPageSessions.Controls.Add(this.gridSessions);
            this.tabPageSessions.Controls.Add(this.panelSessionsActions);
            this.tabPageSessions.Location = new System.Drawing.Point(4, 25);
            this.tabPageSessions.Name = "tabPageSessions";
            this.tabPageSessions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSessions.Size = new System.Drawing.Size(1156, 732);
            this.tabPageSessions.TabIndex = 1;
            this.tabPageSessions.Text = "Oturumlar";
            this.tabPageSessions.UseVisualStyleBackColor = true;
            // 
            // gridSessions
            // 
            this.gridSessions.AllowUserToAddRows = false;
            this.gridSessions.AllowUserToDeleteRows = false;
            this.gridSessions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridSessions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSessions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSessions.Location = new System.Drawing.Point(3, 54);
            this.gridSessions.MultiSelect = false;
            this.gridSessions.Name = "gridSessions";
            this.gridSessions.ReadOnly = true;
            this.gridSessions.RowHeadersVisible = false;
            this.gridSessions.RowHeadersWidth = 51;
            this.gridSessions.RowTemplate.Height = 28;
            this.gridSessions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSessions.Size = new System.Drawing.Size(1150, 675);
            this.gridSessions.TabIndex = 2;
            // 
            // panelSessionsActions
            // 
            this.panelSessionsActions.AutoSize = true;
            this.panelSessionsActions.Controls.Add(this.btnRefreshSessions);
            this.panelSessionsActions.Controls.Add(this.btnAddSession);
            this.panelSessionsActions.Controls.Add(this.btnEditSession);
            this.panelSessionsActions.Controls.Add(this.btnDeleteSession);
            this.panelSessionsActions.Controls.Add(this.btnManageExperimentTypes);
            this.panelSessionsActions.Controls.Add(this.btnManageTimeLabels);
            this.panelSessionsActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSessionsActions.Location = new System.Drawing.Point(3, 3);
            this.panelSessionsActions.Name = "panelSessionsActions";
            this.panelSessionsActions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.panelSessionsActions.Size = new System.Drawing.Size(1150, 51);
            this.panelSessionsActions.TabIndex = 1;
            // 
            // btnRefreshSessions
            // 
            this.btnRefreshSessions.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshSessions.Name = "btnRefreshSessions";
            this.btnRefreshSessions.Size = new System.Drawing.Size(120, 40);
            this.btnRefreshSessions.TabIndex = 0;
            this.btnRefreshSessions.Text = "Yenile";
            this.btnRefreshSessions.UseVisualStyleBackColor = true;
            this.btnRefreshSessions.Click += new System.EventHandler(this.btnRefreshSessions_Click);
            // 
            // btnAddSession
            // 
            this.btnAddSession.Location = new System.Drawing.Point(129, 3);
            this.btnAddSession.Name = "btnAddSession";
            this.btnAddSession.Size = new System.Drawing.Size(130, 40);
            this.btnAddSession.TabIndex = 1;
            this.btnAddSession.Text = "Yeni Oturum";
            this.btnAddSession.UseVisualStyleBackColor = true;
            this.btnAddSession.Click += new System.EventHandler(this.btnAddSession_Click);
            // 
            // btnEditSession
            // 
            this.btnEditSession.Location = new System.Drawing.Point(265, 3);
            this.btnEditSession.Name = "btnEditSession";
            this.btnEditSession.Size = new System.Drawing.Size(130, 40);
            this.btnEditSession.TabIndex = 2;
            this.btnEditSession.Text = "Duzenle";
            this.btnEditSession.UseVisualStyleBackColor = true;
            this.btnEditSession.Click += new System.EventHandler(this.btnEditSession_Click);
            // 
            // btnDeleteSession
            // 
            this.btnDeleteSession.Location = new System.Drawing.Point(401, 3);
            this.btnDeleteSession.Name = "btnDeleteSession";
            this.btnDeleteSession.Size = new System.Drawing.Size(130, 40);
            this.btnDeleteSession.TabIndex = 3;
            this.btnDeleteSession.Text = "Sil";
            this.btnDeleteSession.UseVisualStyleBackColor = true;
            this.btnDeleteSession.Click += new System.EventHandler(this.btnDeleteSession_Click);
            // 
            // btnManageExperimentTypes
            // 
            this.btnManageExperimentTypes.Location = new System.Drawing.Point(537, 3);
            this.btnManageExperimentTypes.Name = "btnManageExperimentTypes";
            this.btnManageExperimentTypes.Size = new System.Drawing.Size(180, 40);
            this.btnManageExperimentTypes.TabIndex = 4;
            this.btnManageExperimentTypes.Text = "Deney Turleri";
            this.btnManageExperimentTypes.UseVisualStyleBackColor = true;
            this.btnManageExperimentTypes.Click += new System.EventHandler(this.btnManageExperimentTypes_Click);
            // 
            // btnManageTimeLabels
            // 
            this.btnManageTimeLabels.Location = new System.Drawing.Point(723, 3);
            this.btnManageTimeLabels.Name = "btnManageTimeLabels";
            this.btnManageTimeLabels.Size = new System.Drawing.Size(180, 40);
            this.btnManageTimeLabels.TabIndex = 5;
            this.btnManageTimeLabels.Text = "Zaman Etiketleri";
            this.btnManageTimeLabels.UseVisualStyleBackColor = true;
            this.btnManageTimeLabels.Click += new System.EventHandler(this.btnManageTimeLabels_Click);
            // 
            // tabPageEEG
            // 
            this.tabPageEEG.Controls.Add(this.gridEEG);
            this.tabPageEEG.Controls.Add(this.panelEEGActions);
            this.tabPageEEG.Location = new System.Drawing.Point(4, 25);
            this.tabPageEEG.Name = "tabPageEEG";
            this.tabPageEEG.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEEG.Size = new System.Drawing.Size(1156, 732);
            this.tabPageEEG.TabIndex = 2;
            this.tabPageEEG.Text = "EEG Verisi";
            this.tabPageEEG.UseVisualStyleBackColor = true;
            // 
            // gridEEG
            // 
            this.gridEEG.AllowUserToAddRows = false;
            this.gridEEG.AllowUserToDeleteRows = false;
            this.gridEEG.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.gridEEG.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridEEG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridEEG.Location = new System.Drawing.Point(3, 54);
            this.gridEEG.MultiSelect = false;
            this.gridEEG.Name = "gridEEG";
            this.gridEEG.ReadOnly = true;
            this.gridEEG.RowHeadersVisible = false;
            this.gridEEG.RowHeadersWidth = 51;
            this.gridEEG.RowTemplate.Height = 28;
            this.gridEEG.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridEEG.Size = new System.Drawing.Size(1150, 675);
            this.gridEEG.TabIndex = 3;
            // 
            // panelEEGActions
            // 
            this.panelEEGActions.AutoSize = true;
            this.panelEEGActions.Controls.Add(this.btnRefreshEEG);
            this.panelEEGActions.Controls.Add(this.cmbEegSessions);
            this.panelEEGActions.Controls.Add(this.btnStreamMonitor);
            this.panelEEGActions.Controls.Add(this.btnStopStream);
            this.panelEEGActions.Controls.Add(this.lblStreamStatus);
            this.panelEEGActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelEEGActions.Location = new System.Drawing.Point(3, 3);
            this.panelEEGActions.Name = "panelEEGActions";
            this.panelEEGActions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.panelEEGActions.Size = new System.Drawing.Size(1150, 51);
            this.panelEEGActions.TabIndex = 2;
            // 
            // btnRefreshEEG
            // 
            this.btnRefreshEEG.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshEEG.Name = "btnRefreshEEG";
            this.btnRefreshEEG.Size = new System.Drawing.Size(120, 40);
            this.btnRefreshEEG.TabIndex = 0;
            this.btnRefreshEEG.Text = "Yenile";
            this.btnRefreshEEG.UseVisualStyleBackColor = true;
            this.btnRefreshEEG.Click += new System.EventHandler(this.btnRefreshEEG_Click);
            // 
            // cmbEegSessions
            // 
            this.cmbEegSessions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEegSessions.FormattingEnabled = true;
            this.cmbEegSessions.Location = new System.Drawing.Point(129, 3);
            this.cmbEegSessions.Name = "cmbEegSessions";
            this.cmbEegSessions.Size = new System.Drawing.Size(558, 24);
            this.cmbEegSessions.TabIndex = 1;
            this.cmbEegSessions.SelectedIndexChanged += new System.EventHandler(this.cmbEegSessions_SelectedIndexChanged);
            // 
            // btnStreamMonitor
            // 
            this.btnStreamMonitor.Location = new System.Drawing.Point(693, 3);
            this.btnStreamMonitor.Name = "btnStreamMonitor";
            this.btnStreamMonitor.Size = new System.Drawing.Size(150, 40);
            this.btnStreamMonitor.TabIndex = 2;
            this.btnStreamMonitor.Text = "Baslat";
            this.btnStreamMonitor.UseVisualStyleBackColor = true;
            this.btnStreamMonitor.Click += new System.EventHandler(this.btnStreamMonitor_Click);
            // 
            // btnStopStream
            // 
            this.btnStopStream.Enabled = false;
            this.btnStopStream.Location = new System.Drawing.Point(849, 3);
            this.btnStopStream.Name = "btnStopStream";
            this.btnStopStream.Size = new System.Drawing.Size(150, 40);
            this.btnStopStream.TabIndex = 3;
            this.btnStopStream.Text = "Durdur";
            this.btnStopStream.UseVisualStyleBackColor = true;
            this.btnStopStream.Click += new System.EventHandler(this.btnStopStream_Click);
            // 
            // lblStreamStatus
            // 
            this.lblStreamStatus.AutoSize = true;
            this.lblStreamStatus.Location = new System.Drawing.Point(1022, 12);
            this.lblStreamStatus.Margin = new System.Windows.Forms.Padding(20, 12, 0, 0);
            this.lblStreamStatus.Name = "lblStreamStatus";
            this.lblStreamStatus.Size = new System.Drawing.Size(83, 16);
            this.lblStreamStatus.TabIndex = 4;
            this.lblStreamStatus.Text = "Durum: Hazir";
            // 
            // tabPageAnalysis
            // 
            this.tabPageAnalysis.Controls.Add(this.gridAnalyses);
            this.tabPageAnalysis.Controls.Add(this.panelAnalysisActions);
            this.tabPageAnalysis.Location = new System.Drawing.Point(4, 25);
            this.tabPageAnalysis.Name = "tabPageAnalysis";
            this.tabPageAnalysis.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAnalysis.Size = new System.Drawing.Size(1156, 732);
            this.tabPageAnalysis.TabIndex = 3;
            this.tabPageAnalysis.Text = "Analizler";
            this.tabPageAnalysis.UseVisualStyleBackColor = true;
            // 
            // gridAnalyses
            // 
            this.gridAnalyses.AllowUserToAddRows = false;
            this.gridAnalyses.AllowUserToDeleteRows = false;
            this.gridAnalyses.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridAnalyses.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAnalyses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAnalyses.Location = new System.Drawing.Point(3, 54);
            this.gridAnalyses.MultiSelect = false;
            this.gridAnalyses.Name = "gridAnalyses";
            this.gridAnalyses.ReadOnly = true;
            this.gridAnalyses.RowHeadersVisible = false;
            this.gridAnalyses.RowHeadersWidth = 51;
            this.gridAnalyses.RowTemplate.Height = 28;
            this.gridAnalyses.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAnalyses.Size = new System.Drawing.Size(1150, 675);
            this.gridAnalyses.TabIndex = 4;
            this.gridAnalyses.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridAnalyses_CellContentClick);
            // 
            // panelAnalysisActions
            // 
            this.panelAnalysisActions.AutoSize = true;
            this.panelAnalysisActions.Controls.Add(this.btnRefreshAnalysis);
            this.panelAnalysisActions.Controls.Add(this.btnTriggerAnalysis);
            this.panelAnalysisActions.Controls.Add(this.btnBatchComparison);
            this.panelAnalysisActions.Controls.Add(this.btnViewMetrics);
            this.panelAnalysisActions.Controls.Add(this.btnDeleteAnalysis);
            this.panelAnalysisActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAnalysisActions.Location = new System.Drawing.Point(3, 3);
            this.panelAnalysisActions.Name = "panelAnalysisActions";
            this.panelAnalysisActions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.panelAnalysisActions.Size = new System.Drawing.Size(1150, 51);
            this.panelAnalysisActions.TabIndex = 3;
            this.panelAnalysisActions.Paint += new System.Windows.Forms.PaintEventHandler(this.panelAnalysisActions_Paint);
            // 
            // btnRefreshAnalysis
            // 
            this.btnRefreshAnalysis.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshAnalysis.Name = "btnRefreshAnalysis";
            this.btnRefreshAnalysis.Size = new System.Drawing.Size(120, 40);
            this.btnRefreshAnalysis.TabIndex = 0;
            this.btnRefreshAnalysis.Text = "Yenile";
            this.btnRefreshAnalysis.UseVisualStyleBackColor = true;
            this.btnRefreshAnalysis.Click += new System.EventHandler(this.btnRefreshAnalysis_Click);
            // 
            // btnTriggerAnalysis
            // 
            this.btnTriggerAnalysis.Location = new System.Drawing.Point(129, 3);
            this.btnTriggerAnalysis.Name = "btnTriggerAnalysis";
            this.btnTriggerAnalysis.Size = new System.Drawing.Size(180, 40);
            this.btnTriggerAnalysis.TabIndex = 1;
            this.btnTriggerAnalysis.Text = "Analiz Tetikle";
            this.btnTriggerAnalysis.UseVisualStyleBackColor = true;
            this.btnTriggerAnalysis.Click += new System.EventHandler(this.btnTriggerAnalysis_Click);
            // 
            // btnBatchComparison
            // 
            this.btnBatchComparison.Location = new System.Drawing.Point(315, 3);
            this.btnBatchComparison.Name = "btnBatchComparison";
            this.btnBatchComparison.Size = new System.Drawing.Size(180, 40);
            this.btnBatchComparison.TabIndex = 2;
            this.btnBatchComparison.Text = "Toplu Karsilastirma";
            this.btnBatchComparison.UseVisualStyleBackColor = true;
            this.btnBatchComparison.Click += new System.EventHandler(this.btnBatchComparison_Click);
            // 
            // btnViewMetrics
            // 
            this.btnViewMetrics.Location = new System.Drawing.Point(501, 3);
            this.btnViewMetrics.Name = "btnViewMetrics";
            this.btnViewMetrics.Size = new System.Drawing.Size(150, 40);
            this.btnViewMetrics.TabIndex = 3;
            this.btnViewMetrics.Text = "Detay Gor";
            this.btnViewMetrics.UseVisualStyleBackColor = true;
            this.btnViewMetrics.Click += new System.EventHandler(this.btnViewMetrics_Click);
            // 
            // btnDeleteAnalysis
            // 
            this.btnDeleteAnalysis.Location = new System.Drawing.Point(657, 3);
            this.btnDeleteAnalysis.Name = "btnDeleteAnalysis";
            this.btnDeleteAnalysis.Size = new System.Drawing.Size(130, 40);
            this.btnDeleteAnalysis.TabIndex = 3;
            this.btnDeleteAnalysis.Text = "Sil";
            this.btnDeleteAnalysis.UseVisualStyleBackColor = true;
            this.btnDeleteAnalysis.Click += new System.EventHandler(this.btnDeleteAnalysis_Click);
            // 
            // tabPageSinav
            // 
            this.tabPageSinav.Location = new System.Drawing.Point(4, 25);
            this.tabPageSinav.Name = "tabPageSinav";
            this.tabPageSinav.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSinav.Size = new System.Drawing.Size(1156, 732);
            this.tabPageSinav.TabIndex = 7;
            this.tabPageSinav.Text = "Sinav Modulu";
            this.tabPageSinav.UseVisualStyleBackColor = true;
            // 
            // tabPageUserNotes
            // 
            this.tabPageUserNotes.Controls.Add(this.splitUserNotes);
            this.tabPageUserNotes.Location = new System.Drawing.Point(4, 25);
            this.tabPageUserNotes.Name = "tabPageUserNotes";
            this.tabPageUserNotes.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUserNotes.Size = new System.Drawing.Size(1156, 732);
            this.tabPageUserNotes.TabIndex = 6;
            this.tabPageUserNotes.Text = "Kullanici Notlari";
            this.tabPageUserNotes.UseVisualStyleBackColor = true;
            // 
            // splitUserNotes
            // 
            this.splitUserNotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitUserNotes.Location = new System.Drawing.Point(3, 3);
            this.splitUserNotes.Name = "splitUserNotes";
            // 
            // splitUserNotes.Panel1
            // 
            this.splitUserNotes.Panel1.Controls.Add(this.gridUsersForNotes);
            // 
            // splitUserNotes.Panel2
            // 
            this.splitUserNotes.Panel2.Controls.Add(this.panelNotesRight);
            this.splitUserNotes.Size = new System.Drawing.Size(1150, 726);
            this.splitUserNotes.SplitterDistance = 392;
            this.splitUserNotes.TabIndex = 0;
            // 
            // gridUsersForNotes
            // 
            this.gridUsersForNotes.AllowUserToAddRows = false;
            this.gridUsersForNotes.AllowUserToDeleteRows = false;
            this.gridUsersForNotes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridUsersForNotes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridUsersForNotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridUsersForNotes.Location = new System.Drawing.Point(0, 0);
            this.gridUsersForNotes.MultiSelect = false;
            this.gridUsersForNotes.Name = "gridUsersForNotes";
            this.gridUsersForNotes.ReadOnly = true;
            this.gridUsersForNotes.RowHeadersVisible = false;
            this.gridUsersForNotes.RowHeadersWidth = 51;
            this.gridUsersForNotes.RowTemplate.Height = 28;
            this.gridUsersForNotes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridUsersForNotes.Size = new System.Drawing.Size(392, 726);
            this.gridUsersForNotes.TabIndex = 0;
            // 
            // panelNotesRight
            // 
            this.panelNotesRight.Controls.Add(this.txtUserNotes);
            this.panelNotesRight.Controls.Add(this.panelNotesActions);
            this.panelNotesRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelNotesRight.Location = new System.Drawing.Point(0, 0);
            this.panelNotesRight.Name = "panelNotesRight";
            this.panelNotesRight.Size = new System.Drawing.Size(754, 726);
            this.panelNotesRight.TabIndex = 0;
            // 
            // txtUserNotes
            // 
            this.txtUserNotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUserNotes.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtUserNotes.Location = new System.Drawing.Point(0, 66);
            this.txtUserNotes.Multiline = true;
            this.txtUserNotes.Name = "txtUserNotes";
            this.txtUserNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtUserNotes.Size = new System.Drawing.Size(754, 660);
            this.txtUserNotes.TabIndex = 1;
            // 
            // panelNotesActions
            // 
            this.panelNotesActions.AutoSize = true;
            this.panelNotesActions.Controls.Add(this.lblNotesUserName);
            this.panelNotesActions.Controls.Add(this.btnSaveNotes);
            this.panelNotesActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelNotesActions.Location = new System.Drawing.Point(0, 0);
            this.panelNotesActions.Name = "panelNotesActions";
            this.panelNotesActions.Padding = new System.Windows.Forms.Padding(10);
            this.panelNotesActions.Size = new System.Drawing.Size(754, 66);
            this.panelNotesActions.TabIndex = 0;
            // 
            // lblNotesUserName
            // 
            this.lblNotesUserName.AutoSize = true;
            this.lblNotesUserName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblNotesUserName.Location = new System.Drawing.Point(13, 17);
            this.lblNotesUserName.Margin = new System.Windows.Forms.Padding(3, 7, 0, 0);
            this.lblNotesUserName.Name = "lblNotesUserName";
            this.lblNotesUserName.Size = new System.Drawing.Size(179, 28);
            this.lblNotesUserName.TabIndex = 0;
            this.lblNotesUserName.Text = "Kullanici seciniz...";
            // 
            // btnSaveNotes
            // 
            this.btnSaveNotes.Location = new System.Drawing.Point(195, 13);
            this.btnSaveNotes.Name = "btnSaveNotes";
            this.btnSaveNotes.Size = new System.Drawing.Size(150, 40);
            this.btnSaveNotes.TabIndex = 1;
            this.btnSaveNotes.Text = "Notlari Kaydet";
            this.btnSaveNotes.UseVisualStyleBackColor = true;
            // 
            // tabPageExport
            // 
            this.tabPageExport.Controls.Add(this.panelExport);
            this.tabPageExport.Location = new System.Drawing.Point(4, 25);
            this.tabPageExport.Name = "tabPageExport";
            this.tabPageExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageExport.Size = new System.Drawing.Size(1156, 732);
            this.tabPageExport.TabIndex = 4;
            this.tabPageExport.Text = "Export";
            this.tabPageExport.UseVisualStyleBackColor = true;
            // 
            // panelExport
            // 
            this.panelExport.AutoScroll = true;
            this.panelExport.Controls.Add(this.grpExportFilters);
            this.panelExport.Controls.Add(this.btnExportExcel);
            this.panelExport.Controls.Add(this.btnExportJson);
            this.panelExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelExport.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelExport.Location = new System.Drawing.Point(3, 3);
            this.panelExport.Name = "panelExport";
            this.panelExport.Padding = new System.Windows.Forms.Padding(10);
            this.panelExport.Size = new System.Drawing.Size(1150, 726);
            this.panelExport.TabIndex = 0;
            // 
            // grpExportFilters
            // 
            this.grpExportFilters.Controls.Add(this.lblExportUser);
            this.grpExportFilters.Controls.Add(this.cmbExportUser);
            this.grpExportFilters.Controls.Add(this.lblExportScope);
            this.grpExportFilters.Controls.Add(this.cmbExportScope);
            this.grpExportFilters.Controls.Add(this.lblExportSession);
            this.grpExportFilters.Controls.Add(this.cmbExportSession);
            this.grpExportFilters.Controls.Add(this.lblExportExperiment);
            this.grpExportFilters.Controls.Add(this.cmbExportExperiment);
            this.grpExportFilters.Controls.Add(this.chkAllTimeLabels);
            this.grpExportFilters.Controls.Add(this.lstExportTimeLabels);
            this.grpExportFilters.Controls.Add(this.chkMultiUserSheets);
            this.grpExportFilters.Location = new System.Drawing.Point(13, 13);
            this.grpExportFilters.Name = "grpExportFilters";
            this.grpExportFilters.Size = new System.Drawing.Size(600, 550);
            this.grpExportFilters.TabIndex = 0;
            this.grpExportFilters.TabStop = false;
            this.grpExportFilters.Text = "Excel Aktarim Secenekleri";
            // 
            // lblExportUser
            // 
            this.lblExportUser.AutoSize = true;
            this.lblExportUser.Location = new System.Drawing.Point(20, 35);
            this.lblExportUser.Name = "lblExportUser";
            this.lblExportUser.Size = new System.Drawing.Size(56, 16);
            this.lblExportUser.TabIndex = 0;
            this.lblExportUser.Text = "Kullanici";
            // 
            // cmbExportUser
            // 
            this.cmbExportUser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExportUser.FormattingEnabled = true;
            this.cmbExportUser.Location = new System.Drawing.Point(180, 32);
            this.cmbExportUser.Name = "cmbExportUser";
            this.cmbExportUser.Size = new System.Drawing.Size(400, 24);
            this.cmbExportUser.TabIndex = 1;
            // 
            // lblExportScope
            // 
            this.lblExportScope.AutoSize = true;
            this.lblExportScope.Location = new System.Drawing.Point(20, 75);
            this.lblExportScope.Name = "lblExportScope";
            this.lblExportScope.Size = new System.Drawing.Size(87, 16);
            this.lblExportScope.TabIndex = 2;
            this.lblExportScope.Text = "Veri Kapsami";
            // 
            // cmbExportScope
            // 
            this.cmbExportScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExportScope.FormattingEnabled = true;
            this.cmbExportScope.Location = new System.Drawing.Point(180, 72);
            this.cmbExportScope.Name = "cmbExportScope";
            this.cmbExportScope.Size = new System.Drawing.Size(400, 24);
            this.cmbExportScope.TabIndex = 3;
            // 
            // lblExportSession
            // 
            this.lblExportSession.AutoSize = true;
            this.lblExportSession.Location = new System.Drawing.Point(20, 115);
            this.lblExportSession.Name = "lblExportSession";
            this.lblExportSession.Size = new System.Drawing.Size(49, 16);
            this.lblExportSession.TabIndex = 4;
            this.lblExportSession.Text = "Oturum";
            // 
            // cmbExportSession
            // 
            this.cmbExportSession.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExportSession.Enabled = false;
            this.cmbExportSession.FormattingEnabled = true;
            this.cmbExportSession.Location = new System.Drawing.Point(180, 112);
            this.cmbExportSession.Name = "cmbExportSession";
            this.cmbExportSession.Size = new System.Drawing.Size(400, 24);
            this.cmbExportSession.TabIndex = 5;
            // 
            // lblExportExperiment
            // 
            this.lblExportExperiment.AutoSize = true;
            this.lblExportExperiment.Location = new System.Drawing.Point(20, 155);
            this.lblExportExperiment.Name = "lblExportExperiment";
            this.lblExportExperiment.Size = new System.Drawing.Size(77, 16);
            this.lblExportExperiment.TabIndex = 6;
            this.lblExportExperiment.Text = "Deney Turu";
            // 
            // cmbExportExperiment
            // 
            this.cmbExportExperiment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExportExperiment.FormattingEnabled = true;
            this.cmbExportExperiment.Location = new System.Drawing.Point(180, 152);
            this.cmbExportExperiment.Name = "cmbExportExperiment";
            this.cmbExportExperiment.Size = new System.Drawing.Size(400, 24);
            this.cmbExportExperiment.TabIndex = 7;
            // 
            // chkAllTimeLabels
            // 
            this.chkAllTimeLabels.AutoSize = true;
            this.chkAllTimeLabels.Checked = true;
            this.chkAllTimeLabels.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAllTimeLabels.Location = new System.Drawing.Point(180, 195);
            this.chkAllTimeLabels.Name = "chkAllTimeLabels";
            this.chkAllTimeLabels.Size = new System.Drawing.Size(152, 20);
            this.chkAllTimeLabels.TabIndex = 8;
            this.chkAllTimeLabels.Text = "Tum zaman etiketleri";
            this.chkAllTimeLabels.UseVisualStyleBackColor = true;
            // 
            // lstExportTimeLabels
            // 
            this.lstExportTimeLabels.CheckOnClick = true;
            this.lstExportTimeLabels.Enabled = false;
            this.lstExportTimeLabels.FormattingEnabled = true;
            this.lstExportTimeLabels.Location = new System.Drawing.Point(180, 225);
            this.lstExportTimeLabels.Name = "lstExportTimeLabels";
            this.lstExportTimeLabels.Size = new System.Drawing.Size(400, 208);
            this.lstExportTimeLabels.TabIndex = 9;
            // 
            // chkMultiUserSheets
            // 
            this.chkMultiUserSheets.AutoSize = true;
            this.chkMultiUserSheets.Location = new System.Drawing.Point(20, 465);
            this.chkMultiUserSheets.Name = "chkMultiUserSheets";
            this.chkMultiUserSheets.Size = new System.Drawing.Size(454, 20);
            this.chkMultiUserSheets.TabIndex = 10;
            this.chkMultiUserSheets.Text = "Tum kullanicilari tek dosyada sheet sheet ekle (zaman etiketlerine gore)";
            this.chkMultiUserSheets.UseVisualStyleBackColor = true;
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Location = new System.Drawing.Point(13, 569);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(200, 50);
            this.btnExportExcel.TabIndex = 1;
            this.btnExportExcel.Text = "Excel\'e Aktar";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            // 
            // btnExportJson
            // 
            this.btnExportJson.Location = new System.Drawing.Point(13, 625);
            this.btnExportJson.Name = "btnExportJson";
            this.btnExportJson.Size = new System.Drawing.Size(200, 50);
            this.btnExportJson.TabIndex = 2;
            this.btnExportJson.Text = "JSON\'a Aktar";
            this.btnExportJson.UseVisualStyleBackColor = true;
            this.btnExportJson.Click += new System.EventHandler(this.btnExportJson_Click);
            // 
            // tabPageLogs
            // 
            this.tabPageLogs.Controls.Add(this.gridLogs);
            this.tabPageLogs.Controls.Add(this.panelLogsActions);
            this.tabPageLogs.Location = new System.Drawing.Point(4, 25);
            this.tabPageLogs.Name = "tabPageLogs";
            this.tabPageLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLogs.Size = new System.Drawing.Size(1156, 732);
            this.tabPageLogs.TabIndex = 5;
            this.tabPageLogs.Text = "Loglar";
            this.tabPageLogs.UseVisualStyleBackColor = true;
            // 
            // gridLogs
            // 
            this.gridLogs.AllowUserToAddRows = false;
            this.gridLogs.AllowUserToDeleteRows = false;
            this.gridLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLogs.Location = new System.Drawing.Point(3, 49);
            this.gridLogs.MultiSelect = false;
            this.gridLogs.Name = "gridLogs";
            this.gridLogs.ReadOnly = true;
            this.gridLogs.RowHeadersVisible = false;
            this.gridLogs.RowHeadersWidth = 51;
            this.gridLogs.RowTemplate.Height = 28;
            this.gridLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridLogs.Size = new System.Drawing.Size(1150, 680);
            this.gridLogs.TabIndex = 1;
            // 
            // panelLogsActions
            // 
            this.panelLogsActions.AutoSize = true;
            this.panelLogsActions.Controls.Add(this.btnRefreshLogs);
            this.panelLogsActions.Controls.Add(this.btnClearLogs);
            this.panelLogsActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelLogsActions.Location = new System.Drawing.Point(3, 3);
            this.panelLogsActions.Name = "panelLogsActions";
            this.panelLogsActions.Size = new System.Drawing.Size(1150, 46);
            this.panelLogsActions.TabIndex = 2;
            // 
            // btnRefreshLogs
            // 
            this.btnRefreshLogs.Location = new System.Drawing.Point(3, 3);
            this.btnRefreshLogs.Name = "btnRefreshLogs";
            this.btnRefreshLogs.Size = new System.Drawing.Size(200, 40);
            this.btnRefreshLogs.TabIndex = 0;
            this.btnRefreshLogs.Text = "LoglarÄ± Yenile";
            this.btnRefreshLogs.UseVisualStyleBackColor = true;
            this.btnRefreshLogs.Click += new System.EventHandler(this.btnRefreshLogs_Click);
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Location = new System.Drawing.Point(209, 3);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(200, 40);
            this.btnClearLogs.TabIndex = 1;
            this.btnClearLogs.Text = "LoglarÄ± Temizle";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.btnClearLogs_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1164, 761);
            this.Controls.Add(this.tabMain);
            this.Name = "Form1";
            this.Text = "EEG Yonetim Paneli";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabMain.ResumeLayout(false);
            this.tabPageUsers.ResumeLayout(false);
            this.tabPageUsers.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).EndInit();
            this.panelUsersActions.ResumeLayout(false);
            this.tabPageSessions.ResumeLayout(false);
            this.tabPageSessions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSessions)).EndInit();
            this.panelSessionsActions.ResumeLayout(false);
            this.tabPageEEG.ResumeLayout(false);
            this.tabPageEEG.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridEEG)).EndInit();
            this.panelEEGActions.ResumeLayout(false);
            this.panelEEGActions.PerformLayout();
            this.tabPageAnalysis.ResumeLayout(false);
            this.tabPageAnalysis.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyses)).EndInit();
            this.panelAnalysisActions.ResumeLayout(false);
            this.tabPageUserNotes.ResumeLayout(false);
            this.splitUserNotes.Panel1.ResumeLayout(false);
            this.splitUserNotes.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitUserNotes)).EndInit();
            this.splitUserNotes.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridUsersForNotes)).EndInit();
            this.panelNotesRight.ResumeLayout(false);
            this.panelNotesRight.PerformLayout();
            this.panelNotesActions.ResumeLayout(false);
            this.panelNotesActions.PerformLayout();
            this.tabPageExport.ResumeLayout(false);
            this.panelExport.ResumeLayout(false);
            this.grpExportFilters.ResumeLayout(false);
            this.grpExportFilters.PerformLayout();
            this.tabPageLogs.ResumeLayout(false);
            this.tabPageLogs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLogs)).EndInit();
            this.panelLogsActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPageUsers;
        private System.Windows.Forms.DataGridView gridUsers;
        private System.Windows.Forms.FlowLayoutPanel panelUsersActions;
        private System.Windows.Forms.Button btnRefreshUsers;
        private System.Windows.Forms.Button btnAddUser;
        private System.Windows.Forms.Button btnEditUser;
        private System.Windows.Forms.Button btnDeleteUser;
        private System.Windows.Forms.Button btnResetPassword;
        private System.Windows.Forms.TabPage tabPageSessions;
        private System.Windows.Forms.DataGridView gridSessions;
        private System.Windows.Forms.FlowLayoutPanel panelSessionsActions;
        private System.Windows.Forms.Button btnRefreshSessions;
        private System.Windows.Forms.Button btnAddSession;
        private System.Windows.Forms.Button btnEditSession;
        private System.Windows.Forms.Button btnDeleteSession;
        private System.Windows.Forms.Button btnManageExperimentTypes;
        private System.Windows.Forms.Button btnManageTimeLabels;
        private System.Windows.Forms.TabPage tabPageEEG;
        private System.Windows.Forms.DataGridView gridEEG;
        private System.Windows.Forms.FlowLayoutPanel panelEEGActions;
        private System.Windows.Forms.Button btnRefreshEEG;
        private System.Windows.Forms.ComboBox cmbEegSessions;
        private System.Windows.Forms.Button btnStreamMonitor;
        private System.Windows.Forms.Button btnStopStream;
        private System.Windows.Forms.Label lblStreamStatus;
        private System.Windows.Forms.TabPage tabPageAnalysis;
        private System.Windows.Forms.DataGridView gridAnalyses;
        private System.Windows.Forms.FlowLayoutPanel panelAnalysisActions;
        private System.Windows.Forms.Button btnRefreshAnalysis;
        private System.Windows.Forms.Button btnTriggerAnalysis;
        private System.Windows.Forms.Button btnBatchComparison;
        private System.Windows.Forms.Button btnViewMetrics;
        private System.Windows.Forms.Button btnDeleteAnalysis;
        private System.Windows.Forms.TabPage tabPageExport;
        private System.Windows.Forms.FlowLayoutPanel panelExport;
        private System.Windows.Forms.GroupBox grpExportFilters;
        private System.Windows.Forms.Label lblExportUser;
        private System.Windows.Forms.ComboBox cmbExportUser;
        private System.Windows.Forms.Label lblExportScope;
        private System.Windows.Forms.ComboBox cmbExportScope;
        private System.Windows.Forms.Label lblExportSession;
        private System.Windows.Forms.ComboBox cmbExportSession;
        private System.Windows.Forms.Label lblExportExperiment;
        private System.Windows.Forms.ComboBox cmbExportExperiment;
        private System.Windows.Forms.CheckBox chkAllTimeLabels;
        private System.Windows.Forms.CheckedListBox lstExportTimeLabels;
        private System.Windows.Forms.CheckBox chkMultiUserSheets;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnExportJson;
        private System.Windows.Forms.TabPage tabPageLogs;
        private System.Windows.Forms.DataGridView gridLogs;
        private System.Windows.Forms.FlowLayoutPanel panelLogsActions;
        private System.Windows.Forms.Button btnRefreshLogs;
        private System.Windows.Forms.Button btnClearLogs;
        private System.Windows.Forms.TabPage tabPageUserNotes;
        private System.Windows.Forms.SplitContainer splitUserNotes;
        private System.Windows.Forms.DataGridView gridUsersForNotes;
        private System.Windows.Forms.Panel panelNotesRight;
        private System.Windows.Forms.TextBox txtUserNotes;
        private System.Windows.Forms.FlowLayoutPanel panelNotesActions;
        private System.Windows.Forms.Label lblNotesUserName;
        private System.Windows.Forms.Button btnSaveNotes;
        private System.Windows.Forms.TabPage tabPageSinav;
    }
}




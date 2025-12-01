namespace eegProject
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPageUsers = new System.Windows.Forms.TabPage();
            this.gridUsers = new System.Windows.Forms.DataGridView();
            this.btnRefreshUsers = new System.Windows.Forms.Button();
            this.btnAddUser = new System.Windows.Forms.Button();
            this.btnEditUser = new System.Windows.Forms.Button();
            this.btnDeleteUser = new System.Windows.Forms.Button();
            this.btnResetPassword = new System.Windows.Forms.Button();
            this.tabPageSessions = new System.Windows.Forms.TabPage();
            this.gridSessions = new System.Windows.Forms.DataGridView();
            this.btnRefreshSessions = new System.Windows.Forms.Button();
            this.btnAddSession = new System.Windows.Forms.Button();
            this.btnEditSession = new System.Windows.Forms.Button();
            this.btnDeleteSession = new System.Windows.Forms.Button();
            this.btnDeleteEegData = new System.Windows.Forms.Button();
            this.btnManageExperimentTypes = new System.Windows.Forms.Button();
            this.btnManageTimeLabels = new System.Windows.Forms.Button();
            this.tabPageEEG = new System.Windows.Forms.TabPage();
            this.gridEEG = new System.Windows.Forms.DataGridView();
            this.cmbEegSessions = new System.Windows.Forms.ComboBox();
            this.btnRefreshEEG = new System.Windows.Forms.Button();
            this.btnStreamMonitor = new System.Windows.Forms.Button();
            this.btnStopStream = new System.Windows.Forms.Button();
            this.lblStreamStatus = new System.Windows.Forms.Label();
            this.tabPageAnalysis = new System.Windows.Forms.TabPage();
            this.gridAnalyses = new System.Windows.Forms.DataGridView();
            this.btnRefreshAnalysis = new System.Windows.Forms.Button();
            this.btnTriggerAnalysis = new System.Windows.Forms.Button();
            this.btnBatchComparison = new System.Windows.Forms.Button();
            this.btnViewMetrics = new System.Windows.Forms.Button();
            this.btnDeleteAnalysis = new System.Windows.Forms.Button();
            this.tabPageExport = new System.Windows.Forms.TabPage();
            this.cmbExportUser = new System.Windows.Forms.ComboBox();
            this.cmbExportScope = new System.Windows.Forms.ComboBox();
            this.cmbExportSession = new System.Windows.Forms.ComboBox();
            this.cmbExportExperiment = new System.Windows.Forms.ComboBox();
            this.lstExportTimeLabels = new System.Windows.Forms.CheckedListBox();
            this.chkAllTimeLabels = new System.Windows.Forms.CheckBox();
            this.chkMultiUserSheets = new System.Windows.Forms.CheckBox();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnExportJson = new System.Windows.Forms.Button();
            this.lblExportUser = new System.Windows.Forms.Label();
            this.lblExportScope = new System.Windows.Forms.Label();
            this.lblExportSession = new System.Windows.Forms.Label();
            this.tabPageLogs = new System.Windows.Forms.TabPage();
            this.gridLogs = new System.Windows.Forms.DataGridView();
            this.btnRefreshLogs = new System.Windows.Forms.Button();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.tabPageUserNotes = new System.Windows.Forms.TabPage();
            this.gridUsersForNotes = new System.Windows.Forms.DataGridView();
            this.txtUserNotes = new System.Windows.Forms.TextBox();
            this.btnSaveNotes = new System.Windows.Forms.Button();
            this.lblNotesUserName = new System.Windows.Forms.Label();
            this.tabPageSinav = new System.Windows.Forms.TabPage();
            this.tabMain.SuspendLayout();
            this.tabPageUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).BeginInit();
            this.tabPageSessions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSessions)).BeginInit();
            this.tabPageEEG.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridEEG)).BeginInit();
            this.tabPageAnalysis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyses)).BeginInit();
            this.tabPageExport.SuspendLayout();
            this.tabPageLogs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLogs)).BeginInit();
            this.tabPageUserNotes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsersForNotes)).BeginInit();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPageUsers);
            this.tabMain.Controls.Add(this.tabPageSessions);
            this.tabMain.Controls.Add(this.tabPageEEG);
            this.tabMain.Controls.Add(this.tabPageAnalysis);
            this.tabMain.Controls.Add(this.tabPageExport);
            this.tabMain.Controls.Add(this.tabPageLogs);
            this.tabMain.Controls.Add(this.tabPageUserNotes);
            this.tabMain.Controls.Add(this.tabPageSinav);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1000, 700);
            this.tabMain.TabIndex = 0;
            // 
            // tabPageUsers
            // 
            this.tabPageUsers.Controls.Add(this.gridUsers);
            this.tabPageUsers.Controls.Add(this.btnRefreshUsers);
            this.tabPageUsers.Controls.Add(this.btnAddUser);
            this.tabPageUsers.Controls.Add(this.btnEditUser);
            this.tabPageUsers.Controls.Add(this.btnDeleteUser);
            this.tabPageUsers.Controls.Add(this.btnResetPassword);
            this.tabPageUsers.Location = new System.Drawing.Point(4, 22);
            this.tabPageUsers.Name = "tabPageUsers";
            this.tabPageUsers.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUsers.Size = new System.Drawing.Size(992, 674);
            this.tabPageUsers.TabIndex = 0;
            this.tabPageUsers.Text = "Kullanıcılar";
            this.tabPageUsers.UseVisualStyleBackColor = true;
            // 
            // gridUsers
            // 
            this.gridUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridUsers.Location = new System.Drawing.Point(6, 6);
            this.gridUsers.Name = "gridUsers";
            this.gridUsers.Size = new System.Drawing.Size(800, 600);
            this.gridUsers.TabIndex = 0;
            // 
            // btnRefreshUsers
            // 
            this.btnRefreshUsers.Location = new System.Drawing.Point(820, 6);
            this.btnRefreshUsers.Name = "btnRefreshUsers";
            this.btnRefreshUsers.Size = new System.Drawing.Size(100, 23);
            this.btnRefreshUsers.TabIndex = 1;
            this.btnRefreshUsers.Text = "Yenile";
            this.btnRefreshUsers.UseVisualStyleBackColor = true;
            this.btnRefreshUsers.Click += new System.EventHandler(this.btnRefreshUsers_Click);
            // 
            // btnAddUser
            // 
            this.btnAddUser.Location = new System.Drawing.Point(820, 35);
            this.btnAddUser.Name = "btnAddUser";
            this.btnAddUser.Size = new System.Drawing.Size(100, 23);
            this.btnAddUser.TabIndex = 2;
            this.btnAddUser.Text = "Ekle";
            this.btnAddUser.UseVisualStyleBackColor = true;
            this.btnAddUser.Click += new System.EventHandler(this.btnAddUser_Click);
            // 
            // btnEditUser
            // 
            this.btnEditUser.Location = new System.Drawing.Point(820, 64);
            this.btnEditUser.Name = "btnEditUser";
            this.btnEditUser.Size = new System.Drawing.Size(100, 23);
            this.btnEditUser.TabIndex = 3;
            this.btnEditUser.Text = "Düzenle";
            this.btnEditUser.UseVisualStyleBackColor = true;
            this.btnEditUser.Click += new System.EventHandler(this.btnEditUser_Click);
            // 
            // btnDeleteUser
            // 
            this.btnDeleteUser.Location = new System.Drawing.Point(820, 93);
            this.btnDeleteUser.Name = "btnDeleteUser";
            this.btnDeleteUser.Size = new System.Drawing.Size(100, 23);
            this.btnDeleteUser.TabIndex = 4;
            this.btnDeleteUser.Text = "Sil";
            this.btnDeleteUser.UseVisualStyleBackColor = true;
            this.btnDeleteUser.Click += new System.EventHandler(this.btnDeleteUser_Click);
            // 
            // btnResetPassword
            // 
            this.btnResetPassword.Location = new System.Drawing.Point(820, 122);
            this.btnResetPassword.Name = "btnResetPassword";
            this.btnResetPassword.Size = new System.Drawing.Size(100, 23);
            this.btnResetPassword.TabIndex = 5;
            this.btnResetPassword.Text = "Şifre Sıfırla";
            this.btnResetPassword.UseVisualStyleBackColor = true;
            this.btnResetPassword.Click += new System.EventHandler(this.btnResetPassword_Click);
            // 
            // tabPageSessions
            // 
            this.tabPageSessions.Controls.Add(this.gridSessions);
            this.tabPageSessions.Controls.Add(this.btnRefreshSessions);
            this.tabPageSessions.Controls.Add(this.btnAddSession);
            this.tabPageSessions.Controls.Add(this.btnEditSession);
            this.tabPageSessions.Controls.Add(this.btnDeleteSession);
            this.tabPageSessions.Controls.Add(this.btnDeleteEegData);
            this.tabPageSessions.Controls.Add(this.btnManageExperimentTypes);
            this.tabPageSessions.Controls.Add(this.btnManageTimeLabels);
            this.tabPageSessions.Location = new System.Drawing.Point(4, 22);
            this.tabPageSessions.Name = "tabPageSessions";
            this.tabPageSessions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSessions.Size = new System.Drawing.Size(992, 674);
            this.tabPageSessions.TabIndex = 1;
            this.tabPageSessions.Text = "Oturumlar";
            this.tabPageSessions.UseVisualStyleBackColor = true;
            // 
            // gridSessions
            // 
            this.gridSessions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSessions.Location = new System.Drawing.Point(6, 6);
            this.gridSessions.Name = "gridSessions";
            this.gridSessions.Size = new System.Drawing.Size(800, 600);
            this.gridSessions.TabIndex = 0;
            // 
            // btnRefreshSessions
            // 
            this.btnRefreshSessions.Location = new System.Drawing.Point(820, 6);
            this.btnRefreshSessions.Name = "btnRefreshSessions";
            this.btnRefreshSessions.Size = new System.Drawing.Size(100, 23);
            this.btnRefreshSessions.TabIndex = 1;
            this.btnRefreshSessions.Text = "Yenile";
            this.btnRefreshSessions.UseVisualStyleBackColor = true;
            this.btnRefreshSessions.Click += new System.EventHandler(this.btnRefreshSessions_Click);
            // 
            // btnAddSession
            // 
            this.btnAddSession.Location = new System.Drawing.Point(820, 35);
            this.btnAddSession.Name = "btnAddSession";
            this.btnAddSession.Size = new System.Drawing.Size(100, 23);
            this.btnAddSession.TabIndex = 2;
            this.btnAddSession.Text = "Ekle";
            this.btnAddSession.UseVisualStyleBackColor = true;
            this.btnAddSession.Click += new System.EventHandler(this.btnAddSession_Click);
            // 
            // btnEditSession
            // 
            this.btnEditSession.Location = new System.Drawing.Point(820, 64);
            this.btnEditSession.Name = "btnEditSession";
            this.btnEditSession.Size = new System.Drawing.Size(100, 23);
            this.btnEditSession.TabIndex = 3;
            this.btnEditSession.Text = "Düzenle";
            this.btnEditSession.UseVisualStyleBackColor = true;
            this.btnEditSession.Click += new System.EventHandler(this.btnEditSession_Click);
            // 
            // btnDeleteSession
            // 
            this.btnDeleteSession.Location = new System.Drawing.Point(820, 93);
            this.btnDeleteSession.Name = "btnDeleteSession";
            this.btnDeleteSession.Size = new System.Drawing.Size(100, 23);
            this.btnDeleteSession.TabIndex = 4;
            this.btnDeleteSession.Text = "Sil";
            this.btnDeleteSession.UseVisualStyleBackColor = true;
            this.btnDeleteSession.Click += new System.EventHandler(this.btnDeleteSession_Click);
            // 
            // btnDeleteEegData
            // 
            this.btnDeleteEegData.Location = new System.Drawing.Point(820, 122);
            this.btnDeleteEegData.Name = "btnDeleteEegData";
            this.btnDeleteEegData.Size = new System.Drawing.Size(100, 23);
            this.btnDeleteEegData.TabIndex = 7;
            this.btnDeleteEegData.Text = "EEG Verisini Sil";
            this.btnDeleteEegData.UseVisualStyleBackColor = true;
            this.btnDeleteEegData.Click += new System.EventHandler(this.btnDeleteEegData_Click);
            // 
            // btnManageExperimentTypes
            // 
            this.btnManageExperimentTypes.Location = new System.Drawing.Point(820, 150);
            this.btnManageExperimentTypes.Name = "btnManageExperimentTypes";
            this.btnManageExperimentTypes.Size = new System.Drawing.Size(100, 23);
            this.btnManageExperimentTypes.TabIndex = 5;
            this.btnManageExperimentTypes.Text = "Deney Türleri";
            this.btnManageExperimentTypes.UseVisualStyleBackColor = true;
            this.btnManageExperimentTypes.Click += new System.EventHandler(this.btnManageExperimentTypes_Click);
            // 
            // btnManageTimeLabels
            // 
            this.btnManageTimeLabels.Location = new System.Drawing.Point(820, 179);
            this.btnManageTimeLabels.Name = "btnManageTimeLabels";
            this.btnManageTimeLabels.Size = new System.Drawing.Size(100, 23);
            this.btnManageTimeLabels.TabIndex = 6;
            this.btnManageTimeLabels.Text = "Zaman Etiketleri";
            this.btnManageTimeLabels.UseVisualStyleBackColor = true;
            this.btnManageTimeLabels.Click += new System.EventHandler(this.btnManageTimeLabels_Click);
            // 
            // tabPageEEG
            // 
            this.tabPageEEG.Controls.Add(this.gridEEG);
            this.tabPageEEG.Controls.Add(this.cmbEegSessions);
            this.tabPageEEG.Controls.Add(this.btnRefreshEEG);
            this.tabPageEEG.Controls.Add(this.btnStreamMonitor);
            this.tabPageEEG.Controls.Add(this.btnStopStream);
            this.tabPageEEG.Controls.Add(this.lblStreamStatus);
            this.tabPageEEG.Location = new System.Drawing.Point(4, 22);
            this.tabPageEEG.Name = "tabPageEEG";
            this.tabPageEEG.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEEG.Size = new System.Drawing.Size(992, 674);
            this.tabPageEEG.TabIndex = 2;
            this.tabPageEEG.Text = "EEG Verisi";
            this.tabPageEEG.UseVisualStyleBackColor = true;
            // 
            // gridEEG
            // 
            this.gridEEG.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridEEG.Location = new System.Drawing.Point(6, 40);
            this.gridEEG.Name = "gridEEG";
            this.gridEEG.Size = new System.Drawing.Size(800, 560);
            this.gridEEG.TabIndex = 0;
            // 
            // cmbEegSessions
            // 
            this.cmbEegSessions.FormattingEnabled = true;
            this.cmbEegSessions.Location = new System.Drawing.Point(6, 10);
            this.cmbEegSessions.Name = "cmbEegSessions";
            this.cmbEegSessions.Size = new System.Drawing.Size(200, 21);
            this.cmbEegSessions.TabIndex = 1;
            this.cmbEegSessions.SelectedIndexChanged += new System.EventHandler(this.cmbEegSessions_SelectedIndexChanged);
            // 
            // btnRefreshEEG
            // 
            this.btnRefreshEEG.Location = new System.Drawing.Point(220, 8);
            this.btnRefreshEEG.Name = "btnRefreshEEG";
            this.btnRefreshEEG.Size = new System.Drawing.Size(75, 23);
            this.btnRefreshEEG.TabIndex = 2;
            this.btnRefreshEEG.Text = "Yenile";
            this.btnRefreshEEG.UseVisualStyleBackColor = true;
            this.btnRefreshEEG.Click += new System.EventHandler(this.btnRefreshEEG_Click);
            // 
            // btnStreamMonitor
            // 
            this.btnStreamMonitor.Location = new System.Drawing.Point(310, 8);
            this.btnStreamMonitor.Name = "btnStreamMonitor";
            this.btnStreamMonitor.Size = new System.Drawing.Size(100, 23);
            this.btnStreamMonitor.TabIndex = 3;
            this.btnStreamMonitor.Text = "Canlı İzle";
            this.btnStreamMonitor.UseVisualStyleBackColor = true;
            this.btnStreamMonitor.Click += new System.EventHandler(this.btnStreamMonitor_Click);
            // 
            // btnStopStream
            // 
            this.btnStopStream.Location = new System.Drawing.Point(420, 8);
            this.btnStopStream.Name = "btnStopStream";
            this.btnStopStream.Size = new System.Drawing.Size(100, 23);
            this.btnStopStream.TabIndex = 4;
            this.btnStopStream.Text = "Durdur";
            this.btnStopStream.UseVisualStyleBackColor = true;
            this.btnStopStream.Click += new System.EventHandler(this.btnStopStream_Click);
            // 
            // lblStreamStatus
            // 
            this.lblStreamStatus.AutoSize = true;
            this.lblStreamStatus.Location = new System.Drawing.Point(540, 13);
            this.lblStreamStatus.Name = "lblStreamStatus";
            this.lblStreamStatus.Size = new System.Drawing.Size(38, 13);
            this.lblStreamStatus.TabIndex = 5;
            this.lblStreamStatus.Text = "Durum";
            // 
            // tabPageAnalysis
            // 
            this.tabPageAnalysis.Controls.Add(this.gridAnalyses);
            this.tabPageAnalysis.Controls.Add(this.btnRefreshAnalysis);
            this.tabPageAnalysis.Controls.Add(this.btnTriggerAnalysis);
            this.tabPageAnalysis.Controls.Add(this.btnBatchComparison);
            this.tabPageAnalysis.Controls.Add(this.btnViewMetrics);
            this.tabPageAnalysis.Controls.Add(this.btnDeleteAnalysis);
            this.tabPageAnalysis.Location = new System.Drawing.Point(4, 22);
            this.tabPageAnalysis.Name = "tabPageAnalysis";
            this.tabPageAnalysis.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAnalysis.Size = new System.Drawing.Size(992, 674);
            this.tabPageAnalysis.TabIndex = 3;
            this.tabPageAnalysis.Text = "Analiz";
            this.tabPageAnalysis.UseVisualStyleBackColor = true;
            // 
            // gridAnalyses
            // 
            this.gridAnalyses.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAnalyses.Location = new System.Drawing.Point(6, 6);
            this.gridAnalyses.Name = "gridAnalyses";
            this.gridAnalyses.Size = new System.Drawing.Size(800, 600);
            this.gridAnalyses.TabIndex = 0;
            this.gridAnalyses.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridAnalyses_CellContentClick);
            // 
            // btnRefreshAnalysis
            // 
            this.btnRefreshAnalysis.Location = new System.Drawing.Point(820, 6);
            this.btnRefreshAnalysis.Name = "btnRefreshAnalysis";
            this.btnRefreshAnalysis.Size = new System.Drawing.Size(100, 23);
            this.btnRefreshAnalysis.TabIndex = 1;
            this.btnRefreshAnalysis.Text = "Yenile";
            this.btnRefreshAnalysis.UseVisualStyleBackColor = true;
            this.btnRefreshAnalysis.Click += new System.EventHandler(this.btnRefreshAnalysis_Click);
            // 
            // btnTriggerAnalysis
            // 
            this.btnTriggerAnalysis.Location = new System.Drawing.Point(820, 35);
            this.btnTriggerAnalysis.Name = "btnTriggerAnalysis";
            this.btnTriggerAnalysis.Size = new System.Drawing.Size(100, 23);
            this.btnTriggerAnalysis.TabIndex = 2;
            this.btnTriggerAnalysis.Text = "Yeni Analiz";
            this.btnTriggerAnalysis.UseVisualStyleBackColor = true;
            this.btnTriggerAnalysis.Click += new System.EventHandler(this.btnTriggerAnalysis_Click);
            // 
            // btnBatchComparison
            // 
            this.btnBatchComparison.Location = new System.Drawing.Point(820, 64);
            this.btnBatchComparison.Name = "btnBatchComparison";
            this.btnBatchComparison.Size = new System.Drawing.Size(100, 23);
            this.btnBatchComparison.TabIndex = 3;
            this.btnBatchComparison.Text = "Toplu Karşılaştırma";
            this.btnBatchComparison.UseVisualStyleBackColor = true;
            this.btnBatchComparison.Click += new System.EventHandler(this.btnBatchComparison_Click);
            // 
            // btnViewMetrics
            // 
            this.btnViewMetrics.Location = new System.Drawing.Point(820, 93);
            this.btnViewMetrics.Name = "btnViewMetrics";
            this.btnViewMetrics.Size = new System.Drawing.Size(100, 23);
            this.btnViewMetrics.TabIndex = 4;
            this.btnViewMetrics.Text = "Detay Gör";
            this.btnViewMetrics.UseVisualStyleBackColor = true;
            this.btnViewMetrics.Click += new System.EventHandler(this.btnViewMetrics_Click);
            // 
            // btnDeleteAnalysis
            // 
            this.btnDeleteAnalysis.Location = new System.Drawing.Point(820, 122);
            this.btnDeleteAnalysis.Name = "btnDeleteAnalysis";
            this.btnDeleteAnalysis.Size = new System.Drawing.Size(100, 23);
            this.btnDeleteAnalysis.TabIndex = 5;
            this.btnDeleteAnalysis.Text = "Sil";
            this.btnDeleteAnalysis.UseVisualStyleBackColor = true;
            this.btnDeleteAnalysis.Click += new System.EventHandler(this.btnDeleteAnalysis_Click);
            // 
            // tabPageExport
            // 
            this.tabPageExport.Controls.Add(this.cmbExportUser);
            this.tabPageExport.Controls.Add(this.cmbExportScope);
            this.tabPageExport.Controls.Add(this.cmbExportSession);
            this.tabPageExport.Controls.Add(this.cmbExportExperiment);
            this.tabPageExport.Controls.Add(this.lstExportTimeLabels);
            this.tabPageExport.Controls.Add(this.chkAllTimeLabels);
            this.tabPageExport.Controls.Add(this.chkMultiUserSheets);
            this.tabPageExport.Controls.Add(this.btnExportExcel);
            this.tabPageExport.Controls.Add(this.btnExportJson);
            this.tabPageExport.Controls.Add(this.lblExportUser);
            this.tabPageExport.Controls.Add(this.lblExportScope);
            this.tabPageExport.Controls.Add(this.lblExportSession);
            this.tabPageExport.Location = new System.Drawing.Point(4, 22);
            this.tabPageExport.Name = "tabPageExport";
            this.tabPageExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageExport.Size = new System.Drawing.Size(992, 674);
            this.tabPageExport.TabIndex = 4;
            this.tabPageExport.Text = "Dışa Aktar";
            this.tabPageExport.UseVisualStyleBackColor = true;
            // 
            // cmbExportUser
            // 
            this.cmbExportUser.FormattingEnabled = true;
            this.cmbExportUser.Location = new System.Drawing.Point(100, 20);
            this.cmbExportUser.Name = "cmbExportUser";
            this.cmbExportUser.Size = new System.Drawing.Size(200, 21);
            this.cmbExportUser.TabIndex = 0;
            this.cmbExportUser.SelectedIndexChanged += new System.EventHandler(this.CmbExportUser_SelectedIndexChanged);
            // 
            // cmbExportScope
            // 
            this.cmbExportScope.FormattingEnabled = true;
            this.cmbExportScope.Location = new System.Drawing.Point(100, 50);
            this.cmbExportScope.Name = "cmbExportScope";
            this.cmbExportScope.Size = new System.Drawing.Size(200, 21);
            this.cmbExportScope.TabIndex = 1;
            this.cmbExportScope.SelectedIndexChanged += new System.EventHandler(this.CmbExportScope_SelectedIndexChanged);
            // 
            // cmbExportSession
            // 
            this.cmbExportSession.FormattingEnabled = true;
            this.cmbExportSession.Location = new System.Drawing.Point(100, 80);
            this.cmbExportSession.Name = "cmbExportSession";
            this.cmbExportSession.Size = new System.Drawing.Size(200, 21);
            this.cmbExportSession.TabIndex = 2;
            // 
            // cmbExportExperiment
            // 
            this.cmbExportExperiment.FormattingEnabled = true;
            this.cmbExportExperiment.Location = new System.Drawing.Point(100, 110);
            this.cmbExportExperiment.Name = "cmbExportExperiment";
            this.cmbExportExperiment.Size = new System.Drawing.Size(200, 21);
            this.cmbExportExperiment.TabIndex = 3;
            // 
            // lstExportTimeLabels
            // 
            this.lstExportTimeLabels.FormattingEnabled = true;
            this.lstExportTimeLabels.Location = new System.Drawing.Point(100, 140);
            this.lstExportTimeLabels.Name = "lstExportTimeLabels";
            this.lstExportTimeLabels.Size = new System.Drawing.Size(200, 94);
            this.lstExportTimeLabels.TabIndex = 4;
            // 
            // chkAllTimeLabels
            // 
            this.chkAllTimeLabels.AutoSize = true;
            this.chkAllTimeLabels.Location = new System.Drawing.Point(320, 140);
            this.chkAllTimeLabels.Name = "chkAllTimeLabels";
            this.chkAllTimeLabels.Size = new System.Drawing.Size(100, 17);
            this.chkAllTimeLabels.TabIndex = 5;
            this.chkAllTimeLabels.Text = "Tüm Etiketler";
            this.chkAllTimeLabels.UseVisualStyleBackColor = true;
            this.chkAllTimeLabels.CheckedChanged += new System.EventHandler(this.ChkAllTimeLabels_CheckedChanged);
            // 
            // chkMultiUserSheets
            // 
            this.chkMultiUserSheets.AutoSize = true;
            this.chkMultiUserSheets.Location = new System.Drawing.Point(320, 20);
            this.chkMultiUserSheets.Name = "chkMultiUserSheets";
            this.chkMultiUserSheets.Size = new System.Drawing.Size(150, 17);
            this.chkMultiUserSheets.TabIndex = 6;
            this.chkMultiUserSheets.Text = "Çoklu Kullanıcı";
            this.chkMultiUserSheets.UseVisualStyleBackColor = true;
            this.chkMultiUserSheets.CheckedChanged += new System.EventHandler(this.ChkMultiUserSheets_CheckedChanged);
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Location = new System.Drawing.Point(100, 250);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(100, 23);
            this.btnExportExcel.TabIndex = 7;
            this.btnExportExcel.Text = "Excel'e Aktar";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            // 
            // btnExportJson
            // 
            this.btnExportJson.Location = new System.Drawing.Point(210, 250);
            this.btnExportJson.Name = "btnExportJson";
            this.btnExportJson.Size = new System.Drawing.Size(100, 23);
            this.btnExportJson.TabIndex = 8;
            this.btnExportJson.Text = "JSON'a Aktar";
            this.btnExportJson.UseVisualStyleBackColor = true;
            this.btnExportJson.Click += new System.EventHandler(this.btnExportJson_Click);
            // 
            // lblExportUser
            // 
            this.lblExportUser.AutoSize = true;
            this.lblExportUser.Location = new System.Drawing.Point(20, 23);
            this.lblExportUser.Name = "lblExportUser";
            this.lblExportUser.Size = new System.Drawing.Size(49, 13);
            this.lblExportUser.TabIndex = 9;
            this.lblExportUser.Text = "Kullanıcı:";
            // 
            // lblExportScope
            // 
            this.lblExportScope.AutoSize = true;
            this.lblExportScope.Location = new System.Drawing.Point(20, 53);
            this.lblExportScope.Name = "lblExportScope";
            this.lblExportScope.Size = new System.Drawing.Size(53, 13);
            this.lblExportScope.TabIndex = 10;
            this.lblExportScope.Text = "Kapsam:";
            // 
            // lblExportSession
            // 
            this.lblExportSession.AutoSize = true;
            this.lblExportSession.Location = new System.Drawing.Point(20, 83);
            this.lblExportSession.Name = "lblExportSession";
            this.lblExportSession.Size = new System.Drawing.Size(46, 13);
            this.lblExportSession.TabIndex = 11;
            this.lblExportSession.Text = "Oturum:";
            // 
            // tabPageLogs
            // 
            this.tabPageLogs.Controls.Add(this.gridLogs);
            this.tabPageLogs.Controls.Add(this.btnRefreshLogs);
            this.tabPageLogs.Controls.Add(this.btnClearLogs);
            this.tabPageLogs.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogs.Name = "tabPageLogs";
            this.tabPageLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLogs.Size = new System.Drawing.Size(992, 674);
            this.tabPageLogs.TabIndex = 5;
            this.tabPageLogs.Text = "Günlükler";
            this.tabPageLogs.UseVisualStyleBackColor = true;
            // 
            // gridLogs
            // 
            this.gridLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridLogs.Location = new System.Drawing.Point(6, 6);
            this.gridLogs.Name = "gridLogs";
            this.gridLogs.Size = new System.Drawing.Size(800, 600);
            this.gridLogs.TabIndex = 0;
            // 
            // btnRefreshLogs
            // 
            this.btnRefreshLogs.Location = new System.Drawing.Point(820, 6);
            this.btnRefreshLogs.Name = "btnRefreshLogs";
            this.btnRefreshLogs.Size = new System.Drawing.Size(100, 23);
            this.btnRefreshLogs.TabIndex = 1;
            this.btnRefreshLogs.Text = "Yenile";
            this.btnRefreshLogs.UseVisualStyleBackColor = true;
            this.btnRefreshLogs.Click += new System.EventHandler(this.btnRefreshLogs_Click);
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Location = new System.Drawing.Point(820, 35);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(100, 23);
            this.btnClearLogs.TabIndex = 2;
            this.btnClearLogs.Text = "Temizle";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.btnClearLogs_Click);
            // 
            // tabPageUserNotes
            // 
            this.tabPageUserNotes.Controls.Add(this.gridUsersForNotes);
            this.tabPageUserNotes.Controls.Add(this.txtUserNotes);
            this.tabPageUserNotes.Controls.Add(this.btnSaveNotes);
            this.tabPageUserNotes.Controls.Add(this.lblNotesUserName);
            this.tabPageUserNotes.Location = new System.Drawing.Point(4, 22);
            this.tabPageUserNotes.Name = "tabPageUserNotes";
            this.tabPageUserNotes.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUserNotes.Size = new System.Drawing.Size(992, 674);
            this.tabPageUserNotes.TabIndex = 6;
            this.tabPageUserNotes.Text = "Kullanıcı Notları";
            this.tabPageUserNotes.UseVisualStyleBackColor = true;
            // 
            // gridUsersForNotes
            // 
            this.gridUsersForNotes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridUsersForNotes.Location = new System.Drawing.Point(6, 6);
            this.gridUsersForNotes.Name = "gridUsersForNotes";
            this.gridUsersForNotes.Size = new System.Drawing.Size(300, 600);
            this.gridUsersForNotes.TabIndex = 0;
            // 
            // txtUserNotes
            // 
            this.txtUserNotes.Location = new System.Drawing.Point(320, 40);
            this.txtUserNotes.Multiline = true;
            this.txtUserNotes.Name = "txtUserNotes";
            this.txtUserNotes.Size = new System.Drawing.Size(400, 500);
            this.txtUserNotes.TabIndex = 1;
            // 
            // btnSaveNotes
            // 
            this.btnSaveNotes.Location = new System.Drawing.Point(320, 550);
            this.btnSaveNotes.Name = "btnSaveNotes";
            this.btnSaveNotes.Size = new System.Drawing.Size(100, 23);
            this.btnSaveNotes.TabIndex = 2;
            this.btnSaveNotes.Text = "Kaydet";
            this.btnSaveNotes.UseVisualStyleBackColor = true;
            // 
            // lblNotesUserName
            // 
            this.lblNotesUserName.AutoSize = true;
            this.lblNotesUserName.Location = new System.Drawing.Point(320, 10);
            this.lblNotesUserName.Name = "lblNotesUserName";
            this.lblNotesUserName.Size = new System.Drawing.Size(80, 13);
            this.lblNotesUserName.TabIndex = 3;
            this.lblNotesUserName.Text = "Kullanıcı Seçiniz";
            // 
            // tabPageSinav
            // 
            this.tabPageSinav.Location = new System.Drawing.Point(4, 22);
            this.tabPageSinav.Name = "tabPageSinav";
            this.tabPageSinav.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSinav.Size = new System.Drawing.Size(992, 674);
            this.tabPageSinav.TabIndex = 7;
            this.tabPageSinav.Text = "Sınav";
            this.tabPageSinav.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.tabMain);
            this.Name = "Form1";
            this.Text = "EEG Yönetim Paneli";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.tabMain.ResumeLayout(false);
            this.tabPageUsers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).EndInit();
            this.tabPageSessions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSessions)).EndInit();
            this.tabPageEEG.ResumeLayout(false);
            this.tabPageEEG.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridEEG)).EndInit();
            this.tabPageAnalysis.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAnalyses)).EndInit();
            this.tabPageExport.ResumeLayout(false);
            this.tabPageExport.PerformLayout();
            this.tabPageLogs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridLogs)).EndInit();
            this.tabPageUserNotes.ResumeLayout(false);
            this.tabPageUserNotes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsersForNotes)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPageUsers;
        private System.Windows.Forms.TabPage tabPageSessions;
        private System.Windows.Forms.TabPage tabPageEEG;
        private System.Windows.Forms.TabPage tabPageAnalysis;
        private System.Windows.Forms.TabPage tabPageExport;
        private System.Windows.Forms.TabPage tabPageLogs;
        private System.Windows.Forms.TabPage tabPageUserNotes;
        private System.Windows.Forms.TabPage tabPageSinav;

        private System.Windows.Forms.DataGridView gridUsers;
        private System.Windows.Forms.Button btnRefreshUsers;
        private System.Windows.Forms.Button btnAddUser;
        private System.Windows.Forms.Button btnEditUser;
        private System.Windows.Forms.Button btnDeleteUser;
        private System.Windows.Forms.Button btnResetPassword;

        private System.Windows.Forms.DataGridView gridSessions;
        private System.Windows.Forms.Button btnRefreshSessions;
        private System.Windows.Forms.Button btnAddSession;
        private System.Windows.Forms.Button btnEditSession;
        private System.Windows.Forms.Button btnDeleteSession;
        private System.Windows.Forms.Button btnDeleteEegData;
        private System.Windows.Forms.Button btnManageExperimentTypes;
        private System.Windows.Forms.Button btnManageTimeLabels;

        private System.Windows.Forms.DataGridView gridEEG;
        private System.Windows.Forms.ComboBox cmbEegSessions;
        private System.Windows.Forms.Button btnRefreshEEG;
        private System.Windows.Forms.Button btnStreamMonitor;
        private System.Windows.Forms.Button btnStopStream;
        private System.Windows.Forms.Label lblStreamStatus;

        private System.Windows.Forms.DataGridView gridAnalyses;
        private System.Windows.Forms.Button btnRefreshAnalysis;
        private System.Windows.Forms.Button btnTriggerAnalysis;
        private System.Windows.Forms.Button btnBatchComparison;
        private System.Windows.Forms.Button btnViewMetrics;
        private System.Windows.Forms.Button btnDeleteAnalysis;

        private System.Windows.Forms.ComboBox cmbExportUser;
        private System.Windows.Forms.ComboBox cmbExportScope;
        private System.Windows.Forms.ComboBox cmbExportSession;
        private System.Windows.Forms.ComboBox cmbExportExperiment;
        private System.Windows.Forms.CheckedListBox lstExportTimeLabels;
        private System.Windows.Forms.CheckBox chkAllTimeLabels;
        private System.Windows.Forms.CheckBox chkMultiUserSheets;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnExportJson;
        private System.Windows.Forms.Label lblExportUser;
        private System.Windows.Forms.Label lblExportScope;
        private System.Windows.Forms.Label lblExportSession;

        private System.Windows.Forms.DataGridView gridLogs;
        private System.Windows.Forms.Button btnRefreshLogs;
        private System.Windows.Forms.Button btnClearLogs;

        private System.Windows.Forms.DataGridView gridUsersForNotes;
        private System.Windows.Forms.TextBox txtUserNotes;
        private System.Windows.Forms.Button btnSaveNotes;
        private System.Windows.Forms.Label lblNotesUserName;
    }
}

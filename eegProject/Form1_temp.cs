using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using eegProject.Forms;
using eegProject.Security;
using eegProject.Services;
using eegProject.Models;

namespace eegProject
{
    public partial class Form1 : Form
    {
        private readonly UserService _userService = new UserService();
        private readonly SessionService _sessionService = new SessionService();
        private readonly EegDataService _eegDataService = new EegDataService();
        private readonly LookupService _lookupService = new LookupService();
        private readonly MindwaveStreamService _mindwaveStreamService = new MindwaveStreamService();
        private readonly ExportService _exportService = new ExportService();
        private readonly AnalysisService _analysisService = new AnalysisService();
        private readonly AnalysisComputationService _analysisComputationService = new AnalysisComputationService();
        private readonly AuditLogService _auditLogService = new AuditLogService();
        private readonly ExamService _examService = new ExamService();
        private readonly ExamLoaderService _examLoaderService = new ExamLoaderService();

        private readonly BindingSource _userBindingSource = new BindingSource();
        private readonly BindingSource _sessionBindingSource = new BindingSource();
        private readonly BindingSource _eegBindingSource = new BindingSource();
        private readonly BindingSource _analysisBindingSource = new BindingSource();
        private readonly BindingSource _userNotesBindingSource = new BindingSource();
        private readonly BindingSource _logsBindingSource = new BindingSource();

        private BindingList<Kullanici> _users = new BindingList<Kullanici>();
        private BindingList<SessionRow> _sessions = new BindingList<SessionRow>();
        private BindingList<EEGVerisi> _eegSamples = new BindingList<EEGVerisi>();
        private BindingList<AnalysisRow> _analyses = new BindingList<AnalysisRow>();
        private BindingList<AuditLog> _logs = new BindingList<AuditLog>();
        private readonly BindingList<SessionRow> _streamSessionOptions = new BindingList<SessionRow>();

        private LookupData _lookupData = new LookupData();
        private bool _userBusy;
        private bool _sessionBusy;
        private bool _analysisBusy;

        private CancellationTokenSource _streamCts;
        private Task _streamTask;
        private int? _streamingSessionId;

        private const int MaxVisibleEegSamples = 200;

        // SÄ±nav modÃ¼lÃ¼ deÄŸiÅŸkenleri
        private ExamData _loadedExam;
        private Dictionary<int, string> _userAnswers = new Dictionary<int, string>();
        private int _currentQuestionIndex = 0;
        private DateTime _examStartTime;

        // GiriÅŸ yapan kullanÄ±cÄ± bilgileri
        private readonly int _currentUserId;
        private readonly string _currentUserRole;
        private readonly string _currentUserName;

        public Form1(int currentUserId, string currentUserRole, string currentUserName)
        {
            _currentUserId = currentUserId;
            _currentUserRole = currentUserRole ?? "Kullanici";
            _currentUserName = currentUserName ?? "Bilinmiyor";

            InitializeComponent();
            InitializeGrids();
            InitializeUserNotesTab();
            InitializeSinavTab();
            ConfigureUIByRole();
            UpdateStreamStatus("Hazir");
        }

        private void InitializeGrids()
        {
            InitializeUserGrid();
            InitializeSessionGrid();
            InitializeEegGrid();
            InitializeAnalysisGrid();
            InitializeLogsGrid();
        }

        private void InitializeLogsGrid()
        {
            gridLogs.AutoGenerateColumns = false;
            gridLogs.Columns.Clear();

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.LogID),
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });

            var dateColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.Tarih),
                HeaderText = "Tarih",
                Width = 160,
                ReadOnly = true
            };
            dateColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "G" };
            gridLogs.Columns.Add(dateColumn);

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.KullaniciAdi),
                HeaderText = "Kullanici",
                Width = 150,
                ReadOnly = true
            });

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.Islem),
                HeaderText = "Islem",
                Width = 180,
                ReadOnly = true
            });

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.Detay),
                HeaderText = "Detay",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AuditLog.Seviye),
                HeaderText = "Seviye",
                Width = 80,
                ReadOnly = true
            });

            _logsBindingSource.DataSource = _logs;
            gridLogs.DataSource = _logsBindingSource;
        }

        private void InitializeUserNotesTab()
        {
            // Sol taraf - KullanÄ±cÄ± listesi
            gridUsersForNotes.AutoGenerateColumns = false;
            gridUsersForNotes.Columns.Clear();

            gridUsersForNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.KullaniciID),
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });

            gridUsersForNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.AdSoyad),
                HeaderText = "Ad Soyad",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            gridUsersForNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.Rol),
                HeaderText = "Rol",
                Width = 100,
                ReadOnly = true
            });

            _userNotesBindingSource.DataSource = _users;
            gridUsersForNotes.DataSource = _userNotesBindingSource;
            gridUsersForNotes.SelectionChanged += GridUsersForNotes_SelectionChanged;

            // SaÄŸ taraf - Not alanÄ±
            txtUserNotes.Enabled = false;
            txtUserNotes.Leave += TxtUserNotes_Leave; // Otomatik kaydetme
            btnSaveNotes.Click += BtnSaveNotes_Click;
        }

        private Kullanici _currentEditingUser;

        private async void TxtUserNotes_Leave(object sender, EventArgs e)
        {
            // TextBox'tan Ã§Ä±karken otomatik kaydet
            await SaveCurrentUserNotesAsync();
        }

        private async void GridUsersForNotes_SelectionChanged(object sender, EventArgs e)
        {
            // Ã–nceki kullanÄ±cÄ±nÄ±n notlarÄ±nÄ± kaydet
            await SaveCurrentUserNotesAsync();

            var selectedUser = gridUsersForNotes.CurrentRow?.DataBoundItem as Kullanici;
            if (selectedUser == null)
            {
                _currentEditingUser = null;
                lblNotesUserName.Text = "Kullanici seciniz...";
                txtUserNotes.Text = string.Empty;
                txtUserNotes.Enabled = false;
                btnSaveNotes.Enabled = false;
                return;
            }

            _currentEditingUser = selectedUser;
            lblNotesUserName.Text = $"Notlar: {selectedUser.AdSoyad} (Otomatik Kaydediliyor)";
            txtUserNotes.Text = selectedUser.Notlar ?? string.Empty;
            txtUserNotes.Enabled = true;
            btnSaveNotes.Enabled = true;
        }

        private async Task SaveCurrentUserNotesAsync()
        {
            if (_currentEditingUser == null)
            {
                return;
            }

            // DeÄŸiÅŸiklik var mÄ± kontrol et
            var currentText = txtUserNotes.Text ?? string.Empty;
            var savedText = _currentEditingUser.Notlar ?? string.Empty;

            if (currentText == savedText)
            {
                return; // DeÄŸiÅŸiklik yok, kaydetmeye gerek yok
            }

            try
            {
                await _userService.UpdateUserNotesAsync(_currentEditingUser.KullaniciID, currentText);
                _currentEditingUser.Notlar = currentText;
            }
            catch (Exception ex)
            {
                ShowError("Notlar kaydedilirken hata olustu.", ex);
            }
        }

        private async void BtnSaveNotes_Click(object sender, EventArgs e)
        {
            // Manuel kaydetme - direkt kaydeder ve mesaj gÃ¶sterir
            if (_currentEditingUser == null)
            {
                return;
            }

            try
            {
                btnSaveNotes.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var currentText = txtUserNotes.Text ?? string.Empty;
                await _userService.UpdateUserNotesAsync(_currentEditingUser.KullaniciID, currentText);
                _currentEditingUser.Notlar = currentText;

                MessageBox.Show(this, "Notlar basariyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Notlar kaydedilirken hata olustu.", ex);
            }
            finally
            {
                btnSaveNotes.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void InitializeUserGrid()
        {
            gridUsers.AutoGenerateColumns = false;
            gridUsers.Columns.Clear();

            gridUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.KullaniciID),
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });

            gridUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.AdSoyad),
                HeaderText = "Ad Soyad",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            gridUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.Email),
                HeaderText = "Email",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            gridUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.Rol),
                HeaderText = "Rol",
                Width = 100,
                ReadOnly = true
            });

            var dateColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Kullanici.KayitTarihi),
                HeaderText = "Kayit Tarihi",
                Width = 150,
                ReadOnly = true
            };
            dateColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "g" };
            gridUsers.Columns.Add(dateColumn);

            _userBindingSource.DataSource = _users;
            gridUsers.DataSource = _userBindingSource;
            gridUsers.SelectionChanged += (sender, _) => UpdateUserActionButtons();
        }

        private void InitializeSessionGrid()
        {
            gridSessions.AutoGenerateColumns = false;
            gridSessions.Columns.Clear();

            gridSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.OturumID),
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });

            gridSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.KullaniciAd),
                HeaderText = "Kullanici",
                Width = 160,
                ReadOnly = true
            });

            gridSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.DeneyTuru),
                HeaderText = "Deney Turu",
                Width = 140,
                ReadOnly = true
            });

            gridSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.ZamanEtiketi),
                HeaderText = "Zaman Etiketi",
                Width = 140,
                ReadOnly = true
            });

            var startColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.KayitBaslangic),
                HeaderText = "Baslangic",
                Width = 150,
                ReadOnly = true
            };
            startColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "g" };
            gridSessions.Columns.Add(startColumn);

            var endColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.KayitBitis),
                HeaderText = "Bitis",
                Width = 150,
                ReadOnly = true
            };
            endColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "g" };
            gridSessions.Columns.Add(endColumn);

            gridSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SessionRow.Notlar),
                HeaderText = "Notlar",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            _sessionBindingSource.DataSource = _sessions;
            gridSessions.DataSource = _sessionBindingSource;
            gridSessions.SelectionChanged += (sender, _) => UpdateSessionActionButtons();
        }

        private void InitializeEegGrid()
        {
            gridEEG.AutoGenerateColumns = false;
            gridEEG.Columns.Clear();

            var timeColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EEGVerisi.KayitZamani),
                HeaderText = "Kayit Zamanï¿½",
                Width = 160,
                ReadOnly = true
            };
            timeColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "G" };
            gridEEG.Columns.Add(timeColumn);

            void AddBandColumn(string property, string header, int width = 90)
            {
                gridEEG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = property,
                    HeaderText = header,
                    Width = width,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" }
                });
            }

            AddBandColumn(nameof(EEGVerisi.Delta), "Delta");
            AddBandColumn(nameof(EEGVerisi.Theta), "Theta");
            AddBandColumn(nameof(EEGVerisi.LowAlpha), "LowAlpha");
            AddBandColumn(nameof(EEGVerisi.HighAlpha), "HighAlpha");
            AddBandColumn(nameof(EEGVerisi.LowBeta), "LowBeta");
            AddBandColumn(nameof(EEGVerisi.HighBeta), "HighBeta");
            AddBandColumn(nameof(EEGVerisi.LowGamma), "LowGamma");
            AddBandColumn(nameof(EEGVerisi.HighGamma), "HighGamma");

            gridEEG.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EEGVerisi.BlinkStrength),
                HeaderText = "Blink",
                Width = 70,
                ReadOnly = true
            });

            _eegBindingSource.DataSource = _eegSamples;
            gridEEG.DataSource = _eegBindingSource;

            cmbEegSessions.DisplayMember = nameof(SessionRow.DisplayName);
            cmbEegSessions.ValueMember = nameof(SessionRow.OturumID);
            cmbEegSessions.DataSource = _streamSessionOptions;
        }

        private void InitializeAnalysisGrid()
        {
            gridAnalyses.AutoGenerateColumns = false;
            gridAnalyses.Columns.Clear();

            gridAnalyses.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.AnalizID),
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });

            gridAnalyses.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.OturumBilgisi),
                HeaderText = "Oturum Bilgisi",
                Width = 250,
                ReadOnly = true
            });

            gridAnalyses.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.AnalizTipi),
                HeaderText = "Analiz Tipi",
                Width = 150,
                ReadOnly = true
            });

            gridAnalyses.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.MetrikOzeti),
                HeaderText = "Metrik Ã–zeti",
                Width = 200,
                ReadOnly = true
            });

            gridAnalyses.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.AiYorumu),
                HeaderText = "AI",
                Width = 50,
                ReadOnly = true
            });

            var dateColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AnalysisRow.AnalizTarihi),
                HeaderText = "Tarih",
                Width = 150,
                ReadOnly = true
            };
            dateColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "g" };
            gridAnalyses.Columns.Add(dateColumn);

            _analysisBindingSource.DataSource = _analyses;
            gridAnalyses.DataSource = _analysisBindingSource;
            gridAnalyses.SelectionChanged += (sender, _) => UpdateAnalysisActionButtons();
            gridAnalyses.DoubleClick += GridAnalyses_DoubleClick;
        }

        private void ConfigureUIByRole()
        {
            // Form baÅŸlÄ±ÄŸÄ±nda kullanÄ±cÄ± bilgisini gÃ¶ster
            this.Text = $"EEG YÃ¶netim Paneli - {_currentUserName} ({_currentUserRole})";

            bool isAdmin = string.Equals(_currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isYonetici = string.Equals(_currentUserRole, "Yonetici", StringComparison.OrdinalIgnoreCase);

            // KullanÄ±cÄ± (Ã¶ÄŸrenci) ise sadece EEG Verisi sekmesini gÃ¶ster
            if (!isAdmin && !isYonetici)
            {
                // TÃ¼m sekmeleri kaldÄ±r
                tabMain.TabPages.Clear();
                // Sadece EEG Verisi sekmesini ekle
                tabMain.TabPages.Add(tabPageEEG);

                // EEG sekmesinde sadece kendi oturumlarÄ±nÄ± gÃ¶rebilsin
                // Bu filtreleme RefreshSessionsAsync'te yapÄ±lacak
            }
            else
            {
                // YÃ¶netici iÃ§in tÃ¼m sekmeler gÃ¶rÃ¼nÃ¼r
                // KullanÄ±cÄ± NotlarÄ± sekmesi sadece yÃ¶neticilere gÃ¶rÃ¼nÃ¼r (zaten yÃ¶neticiyiz)
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadLookupAsync();
            
            bool isAdmin = string.Equals(_currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isYonetici = string.Equals(_currentUserRole, "Yonetici", StringComparison.OrdinalIgnoreCase);

            if (isAdmin || isYonetici)
            {
                // YÃ¶netici tÃ¼m kullanÄ±cÄ±larÄ± ve oturumlarÄ± gÃ¶rebilir
                await RefreshUsersAsync();
                await RefreshSessionsAsync();
                await RefreshAnalysesAsync();
                await RefreshLogsAsync();
                InitializeExportControls();
            }
            else
            {
                // KullanÄ±cÄ± sadece kendi oturumlarÄ±nÄ± gÃ¶rebilir
                await RefreshSessionsForCurrentUserAsync();
            }

            UpdateStreamControls(IsStreaming(), "Hazir");

            // GiriÅŸ logla
            await _auditLogService.LogAsync(
                "Giris", 
                $"{_currentUserRole} olarak sisteme giris yapildi", 
                _currentUserId, 
                _currentUserName, 
                "Info"
            );
        }

        private async Task RefreshSessionsForCurrentUserAsync()
        {
            SetSessionBusyState(true);
            try
            {
                var allSessions = await _sessionService.GetAllAsync();
                // Sadece mevcut kullanÄ±cÄ±nÄ±n oturumlarÄ±nÄ± filtrele
                var userSessions = allSessions.Where(s => s.KullaniciID == _currentUserId).ToList();
                _sessions = new BindingList<SessionRow>(userSessions.Select(ToSessionRow).ToList());
                _sessionBindingSource.DataSource = _sessions;
                PopulateEegSessionOptions();
            }
            catch (Exception ex)
            {
                ShowError("Oturum listesi yuklenirken hata olustu.", ex);
            }
            finally
            {
                SetSessionBusyState(false);
            }
        }

        private void InitializeExportControls()
        {
            cmbExportScope.Items.Add("Tum Oturumlar");
            cmbExportScope.Items.Add("Belirli Oturum");
            cmbExportScope.SelectedIndex = 0;

            cmbExportUser.DisplayMember = nameof(Kullanici.AdSoyad);
            cmbExportUser.ValueMember = nameof(Kullanici.KullaniciID);
            cmbExportUser.SelectedIndexChanged += CmbExportUser_SelectedIndexChanged;

            cmbExportScope.SelectedIndexChanged += CmbExportScope_SelectedIndexChanged;
            chkAllTimeLabels.CheckedChanged += ChkAllTimeLabels_CheckedChanged;
            chkMultiUserSheets.CheckedChanged += ChkMultiUserSheets_CheckedChanged;

            RefreshExportControls();
        }

        private void RefreshExportControls()
        {
            var userList = _userBindingSource.DataSource as BindingList<Kullanici> ?? _users;
            cmbExportUser.DataSource = userList;
            if (userList != null && userList.Count > 0 && cmbExportUser.SelectedIndex < 0)
            {
                cmbExportUser.SelectedIndex = 0;
            }

            var experimentOptions = new List<string> { "Tum Deney Turleri" };
            if (_lookupData?.ExperimentTypes != null)
            {
                experimentOptions.AddRange(_lookupData.ExperimentTypes.Where(v => !string.IsNullOrWhiteSpace(v)));
            }
            cmbExportExperiment.DataSource = experimentOptions;
            if (cmbExportExperiment.Items.Count > 0)
            {
                cmbExportExperiment.SelectedIndex = 0;
            }

            RefreshExportTimeLabels();
        }

        private void RefreshExportTimeLabels()
        {
            lstExportTimeLabels.Items.Clear();
            var timeLabels = BuildExportTimeLabels();
            foreach (var label in timeLabels)
            {
                var display = string.IsNullOrWhiteSpace(label) ? "Etiketsiz" : label;
                lstExportTimeLabels.Items.Add(display, true);
            }
        }

        private void RefreshExportSessionOptions()
        {
            var selectedUserId = cmbExportUser.SelectedValue as int?;
            cmbExportSession.Items.Clear();
            
            if (selectedUserId.HasValue)
            {
                var userSessions = _sessions?.Where(s => s.KullaniciID == selectedUserId.Value).ToList();
                if (userSessions != null && userSessions.Count > 0)
                {
                    foreach (var session in userSessions.OrderByDescending(s => s.KayitBaslangic ?? DateTime.MinValue))
                    {
                        cmbExportSession.Items.Add(session);
                    }
                }
            }

            cmbExportSession.DisplayMember = nameof(SessionRow.DisplayName);
            cmbExportSession.ValueMember = nameof(SessionRow.OturumID);
            
            if (cmbExportSession.Items.Count > 0)
            {
                cmbExportSession.SelectedIndex = 0;
            }
        }

        private void CmbExportUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshExportSessionOptions();
        }

        private void CmbExportScope_SelectedIndexChanged(object sender, EventArgs e)
        {
            var isSingleSession = cmbExportScope.SelectedIndex == 1;
            cmbExportSession.Enabled = isSingleSession;
            lblExportSession.Enabled = isSingleSession;
        }

        private void ChkAllTimeLabels_CheckedChanged(object sender, EventArgs e)
        {
            lstExportTimeLabels.Enabled = !chkAllTimeLabels.Checked;
        }

        private void ChkMultiUserSheets_CheckedChanged(object sender, EventArgs e)
        {
            var isMultiUser = chkMultiUserSheets.Checked;
            cmbExportUser.Enabled = !isMultiUser;
            lblExportUser.Enabled = !isMultiUser;
            cmbExportScope.Enabled = !isMultiUser;
            lblExportScope.Enabled = !isMultiUser;
            cmbExportSession.Enabled = !isMultiUser;
            lblExportSession.Enabled = !isMultiUser;
        }

        private async Task LoadLookupAsync()
        {
            try
            {
                _lookupData = await _lookupService.GetAsync();
            }
            catch (Exception ex)
            {
                _lookupData = new LookupData();
                ShowError("Lookup verisi yuklenirken hata olustu.", ex);
            }
        }

        private async Task RefreshUsersAsync()
        {
            SetUserBusyState(true);
            try
            {
                var users = await _userService.GetAllAsync();
                _users = new BindingList<Kullanici>(users);
                _userBindingSource.DataSource = _users;
                _userNotesBindingSource.DataSource = _users; // KullanÄ±cÄ± NotlarÄ± sekmesi iÃ§in
                UpdateUserActionButtons();
                UpdateSessionActionButtons();
                RefreshExportControls();
            }
            catch (Exception ex)
            {
                ShowError("Kullanici listesi yuklenirken hata olustu.", ex);
            }
            finally
            {
                SetUserBusyState(false);
            }
        }

        private async Task RefreshSessionsAsync()
        {
            SetSessionBusyState(true);
            try
            {
                var sessions = await _sessionService.GetAllAsync();
                _sessions = new BindingList<SessionRow>(sessions.Select(ToSessionRow).ToList());
                _sessionBindingSource.DataSource = _sessions;
                UpdateSessionActionButtons();
                PopulateEegSessionOptions();
                RefreshExportTimeLabels();
                RefreshExportSessionOptions();
            }
            catch (Exception ex)
            {
                ShowError("Oturum listesi yuklenirken hata olustu.", ex);
            }
            finally
            {
                SetSessionBusyState(false);
            }
        }

        private void PopulateEegSessionOptions()
        {
            var selectedId = _streamingSessionId ?? (cmbEegSessions.SelectedItem as SessionRow)?.OturumID;

            _streamSessionOptions.RaiseListChangedEvents = false;
            _streamSessionOptions.Clear();
            foreach (var session in _sessions.OrderByDescending(s => s.KayitBaslangic ?? DateTime.MinValue))
            {
                _streamSessionOptions.Add(session);
            }
            _streamSessionOptions.RaiseListChangedEvents = true;
            _streamSessionOptions.ResetBindings();

            if (selectedId.HasValue)
            {
                var match = _streamSessionOptions.FirstOrDefault(s => s.OturumID == selectedId.Value);
                if (match != null)
                {
                    cmbEegSessions.SelectedItem = match;
                }
            }

            if (cmbEegSessions.SelectedItem == null && _streamSessionOptions.Count > 0)
            {
                cmbEegSessions.SelectedIndex = 0;
            }

            UpdateStreamControls(IsStreaming());
        }

        private List<string> BuildExportTimeLabels()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var set = new HashSet<string>(comparer);
            var result = new List<string>();
            var includeNull = false;

            void AddRange(IEnumerable<string> source)
            {
                if (source == null)
                {
                    return;
                }

                foreach (var label in source)
                {
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        includeNull = true;
                        continue;
                    }

                    var trimmed = label.Trim();
                    if (set.Add(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            AddRange(_lookupData?.TimeLabels);
            AddRange(_sessions?.Select(s => s.ZamanEtiketi));

            result.Sort(comparer);

            if (includeNull)
            {
                result.Insert(0, null);
            }

            return result;
        }
        private static string BuildExportFileName(string userName, string experimentType)
        {
            var baseName = string.IsNullOrWhiteSpace(userName) ? "eeg_export" : userName.Trim();

            if (!string.IsNullOrWhiteSpace(experimentType))
            {
                baseName += "_" + experimentType.Trim();
            }

            baseName += "_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalidChar, '_');
            }

            return baseName + ".xlsx";
        }
        private void SetUserBusyState(bool busy)
        {
            _userBusy = busy;
            btnRefreshUsers.Enabled = !busy;
            btnAddUser.Enabled = !busy;
            UpdateUserActionButtons();
        }

        private void SetSessionBusyState(bool busy)
        {
            _sessionBusy = busy;
            btnRefreshSessions.Enabled = !busy;
            btnAddSession.Enabled = !busy && _users.Count > 0;
            btnManageExperimentTypes.Enabled = !busy;
            btnManageTimeLabels.Enabled = !busy;
            UpdateSessionActionButtons();
            UpdateStreamControls(IsStreaming());
        }

        private void UpdateUserActionButtons()
        {
            if (_userBusy)
            {
                btnEditUser.Enabled = false;
                btnDeleteUser.Enabled = false;
                btnResetPassword.Enabled = false;
                return;
            }

            var hasSelection = GetSelectedUser() != null;
            btnEditUser.Enabled = hasSelection;
            btnDeleteUser.Enabled = hasSelection;
            btnResetPassword.Enabled = hasSelection;
        }

        private void UpdateSessionActionButtons()
        {
            if (_sessionBusy)
            {
                btnEditSession.Enabled = false;
                btnDeleteSession.Enabled = false;
                return;
            }

            var hasSelection = GetSelectedSession() != null;
            btnAddSession.Enabled = _users.Count > 0 && !_sessionBusy;
            btnEditSession.Enabled = hasSelection;
            btnDeleteSession.Enabled = hasSelection;
        }

        private void UpdateStreamControls(bool streaming, string status = null)
        {
            var hasSession = cmbEegSessions.Items.Count > 0;
            btnStreamMonitor.Enabled = !streaming && hasSession && !_sessionBusy;
            btnStopStream.Enabled = streaming;
            btnRefreshEEG.Enabled = !streaming;
            cmbEegSessions.Enabled = !streaming;

            if (!string.IsNullOrWhiteSpace(status))
            {
                UpdateStreamStatus(status);
            }
            else if (!IsStreaming())
            {
                UpdateStreamStatus("Hazir");
            }
        }

        private void UpdateStreamStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            void Apply()
            {
                lblStreamStatus.Text = $"Durum: {message}";
            }

            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(Apply));
                }
                catch (ObjectDisposedException)
                {
                    // ignored
                }
            }
            else
            {
                Apply();
            }
        }

        private bool IsStreaming()
        {
            return _streamTask != null && !_streamTask.IsCompleted;
        }

        private Kullanici GetSelectedUser()
        {
            return gridUsers.CurrentRow?.DataBoundItem as Kullanici;
        }

        private SessionRow GetSelectedSession()
        {
            return gridSessions.CurrentRow?.DataBoundItem as SessionRow;
        }

        private SessionRow GetSelectedEegSession()
        {
            return cmbEegSessions.SelectedItem as SessionRow;
        }

        private SessionRow ToSessionRow(Oturum session)
        {
            if (session == null)
            {
                return new SessionRow();
            }

            var userName = session.Kullanici?.AdSoyad;
            if (string.IsNullOrWhiteSpace(userName))
            {
                var user = _users.FirstOrDefault(u => u.KullaniciID == session.KullaniciID);
                userName = user?.AdSoyad ?? "(Bilinmiyor)";
            }

            return new SessionRow
            {
                OturumID = session.OturumID,
                KullaniciID = session.KullaniciID,
                KullaniciAd = string.IsNullOrWhiteSpace(userName) ? "(Bilinmiyor)" : userName,
                DeneyTuru = session.DeneyTuru ?? string.Empty,
                ZamanEtiketi = session.ZamanEtiketi ?? string.Empty,
                KayitBaslangic = session.KayitBaslangic,
                KayitBitis = session.KayitBitis,
                Notlar = session.Notlar ?? string.Empty
            };
        }

        private async void btnRefreshUsers_Click(object sender, EventArgs e)
        {
            await RefreshUsersAsync();
        }

        private async void btnAddUser_Click(object sender, EventArgs e)
        {
            using (var dialog = new UserEditForm("Yeni Kullanici", true))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    SetUserBusyState(true);
                    var passwordHash = PasswordHasher.HashPassword(dialog.Password);
                    var created = await _userService.CreateAsync(dialog.UserName, dialog.Email, passwordHash, dialog.Role);
                    _users.Add(created);
                    gridUsers.ClearSelection();
                    var index = _users.IndexOf(created);
                    if (index >= 0 && index < gridUsers.Rows.Count)
                    {
                        gridUsers.Rows[index].Selected = true;
                    }
                    UpdateSessionActionButtons();

                    // Log
                    await _auditLogService.LogAsync("KullaniciEklendi", $"Yeni kullanici: {created.AdSoyad} ({created.Rol})", _currentUserId, _currentUserName);
                }
                catch (Exception ex)
                {
                    ShowError("Kullanici olusturulurken hata olustu.", ex);
                    await _auditLogService.LogAsync("KullaniciEklemeHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
                }
                finally
                {
                    SetUserBusyState(false);
                }
            }
        }

        private async void btnEditUser_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedUser();
            if (selected == null)
            {
                return;
            }

            using (var dialog = new UserEditForm("Kullanici Duzenle", false, null, selected))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    SetUserBusyState(true);
                    await _userService.UpdateAsync(selected.KullaniciID, dialog.UserName, dialog.Email, dialog.Role);
                    selected.AdSoyad = dialog.UserName;
                    selected.Email = dialog.Email;
                    selected.Rol = dialog.Role;
                    gridUsers.Refresh();
                    UpdateSessionUserNames(selected.KullaniciID, selected.AdSoyad);

                    if (!string.IsNullOrWhiteSpace(dialog.Password))
                    {
                        var passwordHash = PasswordHasher.HashPassword(dialog.Password);
                        await _userService.ResetPasswordAsync(selected.KullaniciID, passwordHash);
                        selected.SifreHash = passwordHash;
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Kullanici guncellenirken hata olustu.", ex);
                }
                finally
                {
                    SetUserBusyState(false);
                }
            }
        }

        private void UpdateSessionUserNames(int userId, string userName)
        {
            var target = _sessions.Where(s => s.KullaniciID == userId).ToList();
            if (target.Count == 0)
            {
                return;
            }

            foreach (var row in target)
            {
                row.KullaniciAd = string.IsNullOrWhiteSpace(userName) ? "(Bilinmiyor)" : userName;
            }

            gridSessions.Refresh();
            PopulateEegSessionOptions();
        }

        private async void btnDeleteUser_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedUser();
            if (selected == null)
            {
                return;
            }

            var result = MessageBox.Show(this,
                selected.AdSoyad + " kullanicisini ve iliskili tum verileri kalici olarak silmek istiyor musunuz?",
                "Kalici Silme Onayi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SetUserBusyState(true);
                await _userService.DeleteAsync(selected.KullaniciID);
                _users.Remove(selected);
                await RefreshSessionsAsync();

                // Log
                await _auditLogService.LogAsync("KullaniciSilindi", $"Kullanici silindi: {selected.AdSoyad} (ID:{selected.KullaniciID})", _currentUserId, _currentUserName, "Warning");
            }
            catch (Exception ex)
            {
                ShowError("Kullanici silinirken hata olustu.", ex);
                await _auditLogService.LogAsync("KullaniciSilmeHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
            }
            finally
            {
                SetUserBusyState(false);
            }
        }

        private async void btnResetPassword_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedUser();
            if (selected == null)
            {
                return;
            }

            if (!TryPromptForPassword(out var newPassword))
            {
                return;
            }

            try
            {
                SetUserBusyState(true);
                var passwordHash = PasswordHasher.HashPassword(newPassword);
                await _userService.ResetPasswordAsync(selected.KullaniciID, passwordHash);
                selected.SifreHash = passwordHash;
            }
            catch (Exception ex)
            {
                ShowError("Parola sifirlanirken hata olustu.", ex);
            }
            finally
            {
                SetUserBusyState(false);
            }
        }

        private bool TryPromptForPassword(out string password)
        {
            password = null;

            using (var dialog = new Form
            {
                Text = "Parola Sifirla",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(360, 170)
            })
            {
                var lblPassword = new Label { Text = "Yeni Parola", Left = 15, Top = 20, Width = 120 };
                var txtPassword = new TextBox { Left = 150, Top = 18, Width = 180, UseSystemPasswordChar = true };

                var lblPasswordConfirm = new Label { Text = "Parola Tekrar", Left = 15, Top = 60, Width = 120 };
                var txtPasswordConfirm = new TextBox { Left = 150, Top = 58, Width = 180, UseSystemPasswordChar = true };

                var btnOk = new Button { Text = "Kaydet", Left = 170, Width = 75, Top = 110, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Vazgec", Left = 255, Width = 75, Top = 110, DialogResult = DialogResult.Cancel };

                btnOk.Click += (sender, _) =>
                {
                    if (txtPassword.Text.Length < 6)
                    {
                        MessageBox.Show(dialog, "Parola en az 6 karakter olmali", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dialog.DialogResult = DialogResult.None;
                        return;
                    }

                    if (!string.Equals(txtPassword.Text, txtPasswordConfirm.Text, StringComparison.Ordinal))
                    {
                        MessageBox.Show(dialog, "Parolalar uyusmuyor", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dialog.DialogResult = DialogResult.None;
                    }
                };

                dialog.Controls.AddRange(new Control[] { lblPassword, txtPassword, lblPasswordConfirm, txtPasswordConfirm, btnOk, btnCancel });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                password = txtPassword.Text;
                return true;
            }
        }

        private void ShowError(string message, Exception ex)
        {
            var errorText = ex?.Message ?? string.Empty;
            MessageBox.Show(this, $"{message}{Environment.NewLine}{errorText}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void btnRefreshSessions_Click(object sender, EventArgs e)
        {
            await RefreshSessionsAsync();
            await LoadEegSamplesForSelectedAsync();
        }

        private async void btnAddSession_Click(object sender, EventArgs e)
        {
            if (_users.Count == 0)
            {
                MessageBox.Show(this, "Oturum olusturmak icin once kullanici ekleyiniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SessionEditForm("Yeni Oturum", _users, _lookupData.ExperimentTypes, _lookupData.TimeLabels))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var model = dialog.BuildSessionModel();
                    var created = await _sessionService.CreateAsync(model);
                    await EnsureLookupValuesAsync(dialog.SelectedExperimentType, dialog.SelectedTimeLabel);
                    await RefreshSessionsAsync();
                    SelectSessionRow(created.OturumID);

                    // Log
                    var user = _users.FirstOrDefault(u => u.KullaniciID == created.KullaniciID);
                    await _auditLogService.LogAsync("OturumOlusturuldu", $"Yeni oturum: {user?.AdSoyad} - {created.DeneyTuru}", _currentUserId, _currentUserName);
                }
                catch (Exception ex)
                {
                    ShowError("Oturum olusturulurken hata olustu.", ex);
                    await _auditLogService.LogAsync("OturumOlusturmaHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
                }
            }
        }

        private async void btnEditSession_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedSession();
            if (selected == null)
            {
                return;
            }

            var existing = new Oturum
            {
                OturumID = selected.OturumID,
                KullaniciID = selected.KullaniciID,
                DeneyTuru = selected.DeneyTuru,
                ZamanEtiketi = selected.ZamanEtiketi,
                KayitBaslangic = selected.KayitBaslangic,
                KayitBitis = selected.KayitBitis,
                Notlar = selected.Notlar
            };

            using (var dialog = new SessionEditForm("Oturum Duzenle", _users, _lookupData.ExperimentTypes, _lookupData.TimeLabels, existing))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var model = dialog.BuildSessionModel();
                    model.OturumID = selected.OturumID;
                    await _sessionService.UpdateAsync(model);
                    await EnsureLookupValuesAsync(dialog.SelectedExperimentType, dialog.SelectedTimeLabel);
                    await RefreshSessionsAsync();
                    SelectSessionRow(model.OturumID);
                }
                catch (Exception ex)
                {
                    ShowError("Oturum guncellenirken hata olustu.", ex);
                }
            }
        }

        private async void btnDeleteSession_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedSession();
            if (selected == null)
            {
                return;
            }

            var confirmation = MessageBox.Show(this,
                "Secilen oturumu kalici olarak silmek istiyor musunuz?",
                "Silme Onayi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                await _sessionService.DeleteAsync(selected.OturumID);
                await RefreshSessionsAsync();

                // Log
                await _auditLogService.LogAsync("OturumSilindi", $"Oturum silindi: {selected.KullaniciAd} - {selected.DeneyTuru} (ID:{selected.OturumID})", _currentUserId, _currentUserName, "Warning");
            }
            catch (Exception ex)
            {
                ShowError("Oturum silinirken hata olustu.", ex);
                await _auditLogService.LogAsync("OturumSilmeHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
            }
        }

        private async void btnManageExperimentTypes_Click(object sender, EventArgs e)
        {
            using (var dialog = new LookupManageForm("Deney Turleri", _lookupData.ExperimentTypes, "Deney turu"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _lookupData.ExperimentTypes = dialog.Values.ToList();
                _lookupData.Normalize();
                await _lookupService.SaveAsync(_lookupData);
                MessageBox.Show(this, "Deney turu listesi guncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnManageTimeLabels_Click(object sender, EventArgs e)
        {
            using (var dialog = new LookupManageForm("Zaman Etiketleri", _lookupData.TimeLabels, "Zaman etiketi"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _lookupData.TimeLabels = dialog.Values.ToList();
                _lookupData.Normalize();
                await _lookupService.SaveAsync(_lookupData);
                MessageBox.Show(this, "Zaman etiketi listesi guncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task<bool> EnsureLookupValuesAsync(string experimentType, string timeLabel)
        {
            var changed = false;

            if (!string.IsNullOrWhiteSpace(experimentType) &&
                !_lookupData.ExperimentTypes.Any(v => string.Equals(v, experimentType, StringComparison.OrdinalIgnoreCase)))
            {
                _lookupData.ExperimentTypes.Add(experimentType.Trim());
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(timeLabel) &&
                !_lookupData.TimeLabels.Any(v => string.Equals(v, timeLabel, StringComparison.OrdinalIgnoreCase)))
            {
                _lookupData.TimeLabels.Add(timeLabel.Trim());
                changed = true;
            }

            if (changed)
            {
                _lookupData.Normalize();
                await _lookupService.SaveAsync(_lookupData);
            }

            return changed;
        }

        private void SelectSessionRow(int sessionId)
        {
            if (sessionId <= 0 || gridSessions.Rows.Count == 0)
            {
                return;
            }

            for (var i = 0; i < gridSessions.Rows.Count; i++)
            {
                var row = gridSessions.Rows[i];
                if (row.DataBoundItem is SessionRow data && data.OturumID == sessionId)
                {
                    gridSessions.ClearSelection();
                    row.Selected = true;
                    gridSessions.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }
        }

        private async void cmbEegSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadEegSamplesForSelectedAsync();
        }

        private async void btnRefreshEEG_Click(object sender, EventArgs e)
        {
            await LoadEegSamplesForSelectedAsync();
        }

        private async Task LoadEegSamplesForSelectedAsync()
        {
            var session = GetSelectedEegSession();
            if (session == null)
            {
                _eegSamples = new BindingList<EEGVerisi>();
                _eegBindingSource.DataSource = _eegSamples;
                return;
            }

            await LoadEegSamplesAsync(session.OturumID);
        }

        private async Task LoadEegSamplesAsync(int sessionId)
        {
            try
            {
                var samples = await _eegDataService.GetRecentBySessionAsync(sessionId, MaxVisibleEegSamples);
                var list = samples.Select(CloneEegRow).ToList();

                void Apply()
                {
                    _eegSamples = new BindingList<EEGVerisi>(list);
                    _eegBindingSource.DataSource = _eegSamples;
                    gridEEG.Refresh();
                }

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(Apply));
                }
                else
                {
                    Apply();
                }
            }
            catch (Exception ex)
            {
                ShowError("EEG verisi yuklenirken hata olustu.", ex);
            }
        }

        private static EEGVerisi CloneEegRow(EEGVerisi source)
        {
            return new EEGVerisi
            {
                EEGID = source.EEGID,
                OturumID = source.OturumID,
                KullaniciID = source.KullaniciID,
                Delta = source.Delta,
                Theta = source.Theta,
                LowAlpha = source.LowAlpha,
                HighAlpha = source.HighAlpha,
                LowBeta = source.LowBeta,
                HighBeta = source.HighBeta,
                LowGamma = source.LowGamma,
                HighGamma = source.HighGamma,
                BlinkStrength = source.BlinkStrength,
                KayitZamani = source.KayitZamani.Kind == DateTimeKind.Utc ? source.KayitZamani.ToLocalTime() : source.KayitZamani
            };
        }

        private void AddEegSampleToBinding(EEGVerisi sample)
        {
            if (sample == null)
            {
                return;
            }

            void Apply()
            {
                var clone = CloneEegRow(sample);
                _eegSamples.Add(clone);
                while (_eegSamples.Count > MaxVisibleEegSamples)
                {
                    _eegSamples.RemoveAt(0);
                }
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(Apply));
                }
                catch (ObjectDisposedException)
                {
                    // ignored
                }
            }
            else
            {
                Apply();
            }
        }

        private void btnStreamMonitor_Click(object sender, EventArgs e)
        {
            var session = GetSelectedEegSession();
            if (session == null)
            {
                MessageBox.Show(this, "Once oturum seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (IsStreaming())
            {
                MessageBox.Show(this, "Zaten bir EEG akisi calisiyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _streamCts = new CancellationTokenSource();
            _streamingSessionId = session.OturumID;
            UpdateStreamControls(true, "Baglaniyor...");

            try
            {
                _streamTask = _mindwaveStreamService.StartAsync(
                    sample => HandleEegSampleAsync(session, sample),
                    UpdateStreamStatus,
                    _streamCts.Token);

                // Fire-and-forget continuation
                _ = _streamTask.ContinueWith(t => OnStreamCompleted(t, session.OturumID), TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                UpdateStreamControls(false, "Hata");
                ShowError("EEG akisi baslatilirken hata olustu.", ex);
            }
        }

        private async void btnStopStream_Click(object sender, EventArgs e)
        {
            await StopStreamAsync("Durduruluyor...");
        }

        private async Task StopStreamAsync(string statusMessage)
        {
            if (!IsStreaming() || _streamTask == null)
            {
                UpdateStreamControls(false, "Hazir");
                return;
            }

            UpdateStreamStatus(statusMessage);
            try
            {
                _streamCts?.Cancel();
                await _streamTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation
            }
            catch (Exception ex)
            {
                ShowError("EEG akisi durdurulurken hata olustu.", ex);
            }
        }

        private void OnStreamCompleted(Task streamTask, int sessionId)
        {
            string status;
            if (streamTask.IsCanceled)
            {
                status = "Akis durduruldu";
            }
            else if (streamTask.IsFaulted)
            {
                var ex = streamTask.Exception?.GetBaseException() ?? streamTask.Exception;
                status = "Hata";
                ShowError("EEG akisi sirasinda hata olustu.", ex);
            }
            else
            {
                status = "Akis sonlandi";
            }

            _streamTask = null;
            _streamCts?.Dispose();
            _streamCts = null;
            _streamingSessionId = null;

            UpdateStreamControls(false, status);
            _ = LoadEegSamplesAsync(sessionId);
        }

        private async Task HandleEegSampleAsync(SessionRow session, MindwaveSample sample)
        {
            if (session == null || sample == null)
            {
                return;
            }

            var entity = new EEGVerisi
            {
                OturumID = session.OturumID,
                KullaniciID = session.KullaniciID,
                Delta = sample.Delta,
                Theta = sample.Theta,
                LowAlpha = sample.LowAlpha,
                HighAlpha = sample.HighAlpha,
                LowBeta = sample.LowBeta,
                HighBeta = sample.HighBeta,
                LowGamma = sample.LowGamma,
                HighGamma = sample.HighGamma,
                BlinkStrength = sample.BlinkStrength,
                KayitZamani = DateTime.UtcNow
            };

            try
            {
                var persisted = await _eegDataService.InsertAsync(entity);
                AddEegSampleToBinding(persisted);
            }
            catch (Exception ex)
            {
                UpdateStreamStatus("Kayit hatasi");
                ShowError("EEG verisi kaydedilirken hata olustu.", ex);
            }
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // AÃ§Ä±k notlarÄ± kaydet
            await SaveCurrentUserNotesAsync();
            
            await StopStreamAsync("Kapatiliyor");
        }

        private async void btnRefreshAnalysis_Click(object sender, EventArgs e)
        {
            await RefreshAnalysesAsync();
        }

        private async void btnTriggerAnalysis_Click(object sender, EventArgs e)
        {
            await TriggerNewAnalysisAsync();
        }

        private async void btnBatchComparison_Click(object sender, EventArgs e)
        {
            await TriggerBatchComparisonAsync();
        }

        private void btnViewMetrics_Click(object sender, EventArgs e)
        {
            ShowAnalysisDetails();
        }

        private async void btnDeleteAnalysis_Click(object sender, EventArgs e)
        {
            await DeleteSelectedAnalysisAsync();
        }

        private async void btnExportExcel_Click(object sender, EventArgs e)
        {
            var userList = _userBindingSource.DataSource as BindingList<Kullanici> ?? _users;
            if (userList == null || userList.Count == 0)
            {
                MessageBox.Show(this, "Excel aktarimi icin en az bir kullanici gereklidir.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (chkMultiUserSheets.Checked)
            {
                await ExportMultiUserExcelAsync();
            }
            else
            {
                await ExportSingleUserExcelAsync();
            }
        }

        private async Task ExportSingleUserExcelAsync()
        {
            var selectedUser = cmbExportUser.SelectedItem as Kullanici;
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Lutfen bir kullanici seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedExperiment = cmbExportExperiment.SelectedIndex == 0 ? null : cmbExportExperiment.SelectedItem?.ToString();
            
            IReadOnlyList<string> selectedTimeLabels = null;
            if (!chkAllTimeLabels.Checked)
            {
                var checkedLabels = lstExportTimeLabels.CheckedItems.Cast<string>()
                    .Select(display => display == "Etiketsiz" ? null : display)
                    .ToList();
                
                if (checkedLabels.Count == 0)
                {
                    MessageBox.Show(this, "En az bir zaman etiketi seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedTimeLabels = checkedLabels;
            }

            int? selectedSessionId = null;
            if (cmbExportScope.SelectedIndex == 1)
            {
                var selectedSession = cmbExportSession.SelectedItem as SessionRow;
                if (selectedSession == null)
                {
                    MessageBox.Show(this, "Lutfen bir oturum seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedSessionId = selectedSession.OturumID;
            }

            var exportRequest = new ExportRequest(selectedUser.KullaniciID, selectedExperiment, selectedTimeLabels, selectedSessionId);
            var suggestedName = BuildExportFileName(selectedUser.AdSoyad, selectedExperiment);

            using (var saveDialog = new SaveFileDialog
            {
                Title = "Excel Dosyasi Kaydet",
                Filter = "Excel Dosyalari (*.xlsx)|*.xlsx",
                FileName = suggestedName
            })
            {
                if (saveDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    btnExportExcel.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    await _exportService.ExportToExcelAsync(exportRequest, saveDialog.FileName);

                    // Log
                    await _auditLogService.LogAsync("ExcelExport", $"Kullanici: {selectedUser.AdSoyad}, Dosya: {System.IO.Path.GetFileName(saveDialog.FileName)}", _currentUserId, _currentUserName);

                    MessageBox.Show(this, "Excel dosyasi basariyla olusturuldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, ex.Message, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("Excel aktarimi sirasinda hata olustu.", ex);
                    await _auditLogService.LogAsync("ExportHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
                }
                finally
                {
                    btnExportExcel.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }

        private async Task ExportMultiUserExcelAsync()
        {
            var selectedExperiment = cmbExportExperiment.SelectedIndex == 0 ? null : cmbExportExperiment.SelectedItem?.ToString();
            
            IReadOnlyList<string> selectedTimeLabels = null;
            if (!chkAllTimeLabels.Checked)
            {
                var checkedLabels = lstExportTimeLabels.CheckedItems.Cast<string>()
                    .Select(display => display == "Etiketsiz" ? null : display)
                    .ToList();
                
                if (checkedLabels.Count == 0)
                {
                    MessageBox.Show(this, "En az bir zaman etiketi seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedTimeLabels = checkedLabels;
            }

            var suggestedName = BuildExportFileName("TumKullanicilar", selectedExperiment);

            using (var saveDialog = new SaveFileDialog
            {
                Title = "Excel Dosyasi Kaydet",
                Filter = "Excel Dosyalari (*.xlsx)|*.xlsx",
                FileName = suggestedName
            })
            {
                if (saveDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    btnExportExcel.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    var userList = _userBindingSource.DataSource as BindingList<Kullanici> ?? _users;
                    var userIds = userList.Select(u => u.KullaniciID).ToList();
                    
                    await _exportService.ExportMultipleUsersToExcelAsync(userIds, selectedExperiment, selectedTimeLabels, saveDialog.FileName);

                    MessageBox.Show(this, "Excel dosyasi basariyla olusturuldu (tum kullanicilar).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, ex.Message, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("Excel aktarimi sirasinda hata olustu.", ex);
                }
                finally
                {
                    btnExportExcel.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }

        private async void btnExportJson_Click(object sender, EventArgs e)
        {
            var userList = _userBindingSource.DataSource as BindingList<Kullanici> ?? _users;
            if (userList == null || userList.Count == 0)
            {
                MessageBox.Show(this, "JSON aktarimi icin en az bir kullanici gereklidir.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (chkMultiUserSheets.Checked)
            {
                await ExportMultiUserJsonAsync();
            }
            else
            {
                await ExportSingleUserJsonAsync();
            }
        }

        private async Task ExportSingleUserJsonAsync()
        {
            var selectedUser = cmbExportUser.SelectedItem as Kullanici;
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Lutfen bir kullanici seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedExperiment = cmbExportExperiment.SelectedIndex == 0 ? null : cmbExportExperiment.SelectedItem?.ToString();
            
            IReadOnlyList<string> selectedTimeLabels = null;
            if (!chkAllTimeLabels.Checked)
            {
                var checkedLabels = lstExportTimeLabels.CheckedItems.Cast<string>()
                    .Select(display => display == "Etiketsiz" ? null : display)
                    .ToList();
                
                if (checkedLabels.Count == 0)
                {
                    MessageBox.Show(this, "En az bir zaman etiketi seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedTimeLabels = checkedLabels;
            }

            int? selectedSessionId = null;
            if (cmbExportScope.SelectedIndex == 1)
            {
                var selectedSession = cmbExportSession.SelectedItem as SessionRow;
                if (selectedSession == null)
                {
                    MessageBox.Show(this, "Lutfen bir oturum seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedSessionId = selectedSession.OturumID;
            }

            var exportRequest = new ExportRequest(selectedUser.KullaniciID, selectedExperiment, selectedTimeLabels, selectedSessionId);
            var suggestedName = BuildExportFileName(selectedUser.AdSoyad, selectedExperiment).Replace(".xlsx", ".json");

            using (var saveDialog = new SaveFileDialog
            {
                Title = "JSON Dosyasi Kaydet",
                Filter = "JSON Dosyalari (*.json)|*.json",
                FileName = suggestedName
            })
            {
                if (saveDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    btnExportJson.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    await _exportService.ExportToJsonAsync(exportRequest, saveDialog.FileName);

                    MessageBox.Show(this, "JSON dosyasi basariyla olusturuldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, ex.Message, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("JSON aktarimi sirasinda hata olustu.", ex);
                }
                finally
                {
                    btnExportJson.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }

        private async Task ExportMultiUserJsonAsync()
        {
            var selectedExperiment = cmbExportExperiment.SelectedIndex == 0 ? null : cmbExportExperiment.SelectedItem?.ToString();
            
            IReadOnlyList<string> selectedTimeLabels = null;
            if (!chkAllTimeLabels.Checked)
            {
                var checkedLabels = lstExportTimeLabels.CheckedItems.Cast<string>()
                    .Select(display => display == "Etiketsiz" ? null : display)
                    .ToList();
                
                if (checkedLabels.Count == 0)
                {
                    MessageBox.Show(this, "En az bir zaman etiketi seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedTimeLabels = checkedLabels;
            }

            var suggestedName = BuildExportFileName("TumKullanicilar", selectedExperiment).Replace(".xlsx", ".json");

            using (var saveDialog = new SaveFileDialog
            {
                Title = "JSON Dosyasi Kaydet",
                Filter = "JSON Dosyalari (*.json)|*.json",
                FileName = suggestedName
            })
            {
                if (saveDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    btnExportJson.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    var userList = _userBindingSource.DataSource as BindingList<Kullanici> ?? _users;
                    var userIds = userList.Select(u => u.KullaniciID).ToList();
                    
                    await _exportService.ExportMultipleUsersToJsonAsync(userIds, selectedExperiment, selectedTimeLabels, saveDialog.FileName);

                    MessageBox.Show(this, "JSON dosyasi basariyla olusturuldu (tum kullanicilar).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, ex.Message, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("JSON aktarimi sirasinda hata olustu.", ex);
                }
                finally
                {
                    btnExportJson.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }

        private async Task RefreshAnalysesAsync()
        {
            SetAnalysisBusyState(true);
            try
            {
                var analyses = await _analysisService.GetRecentAsync(200);
                var rows = analyses.Select(ToAnalysisRow).ToList();
                _analyses = new BindingList<AnalysisRow>(rows);
                _analysisBindingSource.DataSource = _analyses;
                UpdateAnalysisActionButtons();
            }
            catch (Exception ex)
            {
                ShowError("Analiz listesi yuklenirken hata olustu.", ex);
            }
            finally
            {
                SetAnalysisBusyState(false);
            }
        }

        private AnalysisRow ToAnalysisRow(AnalizSonucu analiz)
        {
            if (analiz == null)
            {
                return new AnalysisRow();
            }

            // OturumBilgisi oluÅŸtur
            string oturumBilgisi;
            if (analiz.OturumID.HasValue && analiz.Oturum != null)
            {
                var kullaniciAd = analiz.Oturum.Kullanici?.AdSoyad ?? "Bilinmiyor";
                var deneyTuru = analiz.Oturum.DeneyTuru ?? "-";
                var zamanEtiketi = analiz.Oturum.ZamanEtiketi ?? "-";
                oturumBilgisi = $"{kullaniciAd} - {deneyTuru} ({zamanEtiketi})";
            }
            else
            {
                oturumBilgisi = "Ã‡oklu Oturum";
            }

            // MetricsJSON'dan Ã¶zet Ã§Ä±kar
            string metrikOzeti = "N/A";
            try
            {
                if (!string.IsNullOrWhiteSpace(analiz.MetricsJSON))
                {
                    dynamic metrics = JsonConvert.DeserializeObject(analiz.MetricsJSON);
                    if (metrics != null)
                    {
                        if (metrics.RahatlamaIndeksi != null)
                        {
                            metrikOzeti = $"Ä°ndeks: {metrics.RahatlamaIndeksi} | Samples: {metrics.SampleCount}";
                        }
                        else if (metrics.DikkatSkoru != null)
                        {
                            metrikOzeti = $"Skor: {metrics.DikkatSkoru} | Samples: {metrics.SampleCount}";
                        }
                        else if (metrics.EngagementIndex != null)
                        {
                            metrikOzeti = $"Ä°ndeks: {metrics.EngagementIndex} | Samples: {metrics.SampleCount}";
                        }
                    }
                }
            }
            catch
            {
                metrikOzeti = "Parse hatasÄ±";
            }

            // AI yorumu var mÄ±?
            bool aiYorumu = analiz.Metodoloji?.Contains("_AI") ?? false;

            return new AnalysisRow
            {
                AnalizID = analiz.AnalizID,
                OturumID = analiz.OturumID,
                OturumBilgisi = oturumBilgisi,
                AnalizTipi = GetAnalysisTypeDisplayName(analiz.AnalizTipi),
                MetrikOzeti = metrikOzeti,
                AiYorumu = aiYorumu,
                AnalizTarihi = analiz.AnalizTarihi,
                Summary = analiz.Summary,
                MetricsJSON = analiz.MetricsJSON
            };
        }

        private string GetAnalysisTypeDisplayName(string analizTipi)
        {
            if (string.IsNullOrWhiteSpace(analizTipi))
            {
                return "Bilinmiyor";
            }

            switch (analizTipi)
            {
                case "RahatlamaAnalizi":
                    return "Rahatlama";
                case "DikkatAnalizi":
                    return "Dikkat";
                case "EngagementAnalizi":
                    return "Engagement";
                default:
                    return analizTipi;
            }
        }

        private async Task TriggerNewAnalysisAsync()
        {
            if (_sessions == null || _sessions.Count == 0)
            {
                MessageBox.Show(this, "Analiz yapmak icin en az bir oturum gereklidir.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Analiz dialog'u gÃ¶ster
            using (var dialog = new Form
            {
                Text = "Yeni Analiz",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(450, 280)
            })
            {
                var lblSession = new Label { Text = "Oturum:", Left = 20, Top = 20, Width = 100 };
                var cmbSession = new ComboBox
                {
                    Left = 130,
                    Top = 18,
                    Width = 300,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = nameof(SessionRow.DisplayName),
                    ValueMember = nameof(SessionRow.OturumID),
                    DataSource = _sessions.OrderByDescending(s => s.KayitBaslangic ?? DateTime.MinValue).ToList()
                };

                var lblAnalysisType = new Label { Text = "Analiz Tipi:", Left = 20, Top = 60, Width = 100 };
                var cmbAnalysisType = new ComboBox
                {
                    Left = 130,
                    Top = 58,
                    Width = 300,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbAnalysisType.Items.Add("Rahatlama Analizi");
                cmbAnalysisType.Items.Add("Dikkat Analizi");
                cmbAnalysisType.Items.Add("Engagement Analizi");
                cmbAnalysisType.SelectedIndex = 0;

                var isAiAvailable = _analysisComputationService.IsAiAvailable;
                
                var chkAI = new CheckBox
                {
                    Text = isAiAvailable ? "AI Yorumu Ekle (~0.03 TL)" : "AI Yorumu Ekle (API key eksik)",
                    Left = 130,
                    Top = 100,
                    Width = 300,
                    Enabled = isAiAvailable,
                    Checked = false
                };

                var lblNote = new Label
                {
                    Text = isAiAvailable 
                        ? "Not: AI yorumu ChatGPT ile olusturulacaktir.\nMaliyet: ~0.03 TL/analiz (GPT-3.5)"
                        : "Not: AI ozelligi icin App.config'e\nOpenAI_ApiKey eklemeniz gerekiyor.",
                    Left = 130,
                    Top = 130,
                    Width = 300,
                    Height = 40,
                    ForeColor = isAiAvailable ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed
                };

                var btnOk = new Button { Text = "Analiz Et", Left = 260, Width = 80, Top = 220, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "VazgeÃ§", Left = 350, Width = 80, Top = 220, DialogResult = DialogResult.Cancel };

                dialog.Controls.AddRange(new Control[] { lblSession, cmbSession, lblAnalysisType, cmbAnalysisType, chkAI, lblNote, btnOk, btnCancel });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var selectedSession = cmbSession.SelectedItem as SessionRow;
                if (selectedSession == null)
                {
                    MessageBox.Show(this, "Lutfen bir oturum seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var analysisTypeIndex = cmbAnalysisType.SelectedIndex;
                var useAI = chkAI.Checked;

                try
                {
                    SetAnalysisBusyState(true);
                    Cursor = Cursors.WaitCursor;

                    AnalizSonucu result;
                    switch (analysisTypeIndex)
                    {
                        case 0: // Rahatlama
                            result = await _analysisComputationService.ComputeRahatlamaAnaliziAsync(selectedSession.OturumID, useAI);
                            break;
                        case 1: // Dikkat
                            result = await _analysisComputationService.ComputeDikkatAnaliziAsync(selectedSession.OturumID, useAI);
                            break;
                        case 2: // Engagement
                            result = await _analysisComputationService.ComputeEngagementAnaliziAsync(selectedSession.OturumID, useAI);
                            break;
                        default:
                            throw new InvalidOperationException("Gecersiz analiz tipi");
                    }

                    // VeritabanÄ±na kaydet
                    var saved = await _analysisService.CreateAsync(result);

                    // Grid'i yenile
                    await RefreshAnalysesAsync();

                    // Log
                    await _auditLogService.LogAsync("AnalizTamamlandi", $"Analiz: {analysisTypeIndex switch { 0 => "Rahatlama", 1 => "Dikkat", 2 => "Engagement", _ => "Bilinmiyor" }} - Oturum:{selectedSession.OturumID}", _currentUserId, _currentUserName);

                    MessageBox.Show(this, "Analiz basariyla tamamlandi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("Analiz yapilirken hata olustu.", ex);
                    await _auditLogService.LogAsync("AnalizHatasi", ex.Message, _currentUserId, _currentUserName, "Error");
                }
                finally
                {
                    SetAnalysisBusyState(false);
                    Cursor = Cursors.Default;
                }
            }
        }

        private void ShowAnalysisDetails()
        {
            var selected = GetSelectedAnalysis();
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir analiz seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Detay dialog'u gÃ¶ster
            using (var dialog = new Form
            {
                Text = $"Analiz Detayi #{selected.AnalizID}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                Size = new System.Drawing.Size(700, 500)
            })
            {
                var tabControl = new TabControl { Dock = DockStyle.Fill };

                // Summary tab
                var tabSummary = new TabPage("Summary");
                var txtSummary = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    Text = selected.Summary ?? "Summary mevcut degil.",
                    Font = new System.Drawing.Font("Segoe UI", 10)
                };
                tabSummary.Controls.Add(txtSummary);

                // MetricsJSON tab
                var tabMetrics = new TabPage("MetricsJSON");
                var txtMetrics = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    Text = selected.MetricsJSON ?? "MetricsJSON mevcut degil.",
                    Font = new System.Drawing.Font("Consolas", 9)
                };
                tabMetrics.Controls.Add(txtMetrics);

                tabControl.TabPages.Add(tabSummary);
                tabControl.TabPages.Add(tabMetrics);

                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
                var btnClose = new Button
                {
                    Text = "Kapat",
                    DialogResult = DialogResult.OK,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    Location = new System.Drawing.Point(panelBottom.Width - 100, 10),
                    Size = new System.Drawing.Size(80, 30)
                };
                panelBottom.Controls.Add(btnClose);

                dialog.Controls.Add(tabControl);
                dialog.Controls.Add(panelBottom);

                dialog.ShowDialog(this);
            }
        }

        private async Task DeleteSelectedAnalysisAsync()
        {
            var selected = GetSelectedAnalysis();
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir analiz seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmation = MessageBox.Show(this,
                "Secilen analizi silmek istiyor musunuz?",
                "Silme Onayi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SetAnalysisBusyState(true);
                await _analysisService.DeleteAsync(selected.AnalizID);
                await RefreshAnalysesAsync();
                MessageBox.Show(this, "Analiz basariyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Analiz silinirken hata olustu.", ex);
            }
            finally
            {
                SetAnalysisBusyState(false);
            }
        }

        private void GridAnalyses_DoubleClick(object sender, EventArgs e)
        {
            ShowAnalysisDetails();
        }

        private AnalysisRow GetSelectedAnalysis()
        {
            return gridAnalyses.CurrentRow?.DataBoundItem as AnalysisRow;
        }

        private void UpdateAnalysisActionButtons()
        {
            if (_analysisBusy)
            {
                btnViewMetrics.Enabled = false;
                btnDeleteAnalysis.Enabled = false;
                return;
            }

            var hasSelection = GetSelectedAnalysis() != null;
            btnViewMetrics.Enabled = hasSelection;
            btnDeleteAnalysis.Enabled = hasSelection;
        }

        private void SetAnalysisBusyState(bool busy)
        {
            _analysisBusy = busy;
            btnRefreshAnalysis.Enabled = !busy;
            btnTriggerAnalysis.Enabled = !busy && _sessions.Count > 0;
            btnBatchComparison.Enabled = !busy && _sessions.Count > 1; // En az 2 oturum gerekli
            UpdateAnalysisActionButtons();
        }

        private async Task TriggerBatchComparisonAsync()
        {
            if (_sessions == null || _sessions.Count < 2)
            {
                MessageBox.Show(this, "Toplu karsilastirma icin en az 2 oturum gereklidir.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Toplu karÅŸÄ±laÅŸtÄ±rma dialog'u gÃ¶ster
            using (var dialog = new Form
            {
                Text = "Toplu Oturum Karsilastirmasi",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(500, 550),
                AutoScroll = true
            })
            {
                // KullanÄ±cÄ± seÃ§imi
                var lblUser = new Label { Text = "Kullanici:", Left = 20, Top = 20, Width = 100 };
                var cmbUser = new ComboBox
                {
                    Left = 130,
                    Top = 18,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = nameof(Kullanici.AdSoyad),
                    ValueMember = nameof(Kullanici.KullaniciID),
                    DataSource = _users.ToList()
                };

                // Deney tÃ¼rÃ¼ seÃ§imi
                var lblExperiment = new Label { Text = "Deney Turu:", Left = 20, Top = 60, Width = 100 };
                var cmbExperiment = new ComboBox
                {
                    Left = 130,
                    Top = 58,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbExperiment.Items.Add("Tum Deney Turleri");
                if (_lookupData?.ExperimentTypes != null)
                {
                    foreach (var exp in _lookupData.ExperimentTypes.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        cmbExperiment.Items.Add(exp);
                    }
                }
                cmbExperiment.SelectedIndex = 0;

                // Analiz tipi seÃ§imi
                var lblAnalysisType = new Label { Text = "Analiz Tipi:", Left = 20, Top = 100, Width = 100 };
                var cmbAnalysisType = new ComboBox
                {
                    Left = 130,
                    Top = 98,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbAnalysisType.Items.AddRange(new object[] { "Rahatlama", "Dikkat", "Engagement" });
                cmbAnalysisType.SelectedIndex = 0;

                // Oturum seÃ§imi (CheckedListBox)
                var lblSessions = new Label { Text = "Oturumlar:", Left = 20, Top = 140, Width = 460, Height = 20 };
                var lstSessions = new CheckedListBox
                {
                    Left = 20,
                    Top = 165,
                    Width = 460,
                    Height = 200,
                    CheckOnClick = true
                };

                // KullanÄ±cÄ± deÄŸiÅŸtiÄŸinde oturumlarÄ± gÃ¼ncelle
                Action updateSessions = () =>
                {
                    lstSessions.Items.Clear();
                    var selectedUserId = cmbUser.SelectedValue as int?;
                    if (!selectedUserId.HasValue) return;

                    var selectedExperiment = cmbExperiment.SelectedIndex == 0 ? null : cmbExperiment.SelectedItem?.ToString();

                    var userSessions = _sessions
                        .Where(s => s.KullaniciID == selectedUserId.Value)
                        .Where(s => selectedExperiment == null || s.DeneyTuru == selectedExperiment)
                        .OrderBy(s => s.ZamanEtiketi ?? "")
                        .ThenBy(s => s.KayitBaslangic ?? DateTime.MinValue)
                        .ToList();

                    foreach (var session in userSessions)
                    {
                        lstSessions.Items.Add(session, true); // VarsayÄ±lan olarak iÅŸaretli
                    }
                };

                cmbUser.SelectedIndexChanged += (s, e) => updateSessions();
                cmbExperiment.SelectedIndexChanged += (s, e) => updateSessions();
                updateSessions();

                // AI checkbox
                var isAiAvailable = _analysisComputationService.IsAiAvailable;
                var chkAI = new CheckBox
                {
                    Text = isAiAvailable ? "AI Karsilastirmali Yorum (~0.06 TL)" : "AI Yorum (API key eksik)",
                    Left = 20,
                    Top = 380,
                    Width = 460,
                    Enabled = isAiAvailable,
                    Checked = false
                };

                var lblNote = new Label
                {
                    Text = isAiAvailable
                        ? "Not: AI en yuksek/dusuk degerleri ve trendi yorumlayacak."
                        : "Not: AI ozelligi icin App.config'e OpenAI_ApiKey ekleyin.",
                    Left = 20,
                    Top = 410,
                    Width = 460,
                    Height = 40,
                    ForeColor = isAiAvailable ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed
                };

                // Butonlar
                var btnOk = new Button { Text = "Karsilastir", Left = 310, Width = 80, Top = 470, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Vazgec", Left = 400, Width = 80, Top = 470, DialogResult = DialogResult.Cancel };

                dialog.Controls.AddRange(new Control[]
                {
                    lblUser, cmbUser,
                    lblExperiment, cmbExperiment,
                    lblAnalysisType, cmbAnalysisType,
                    lblSessions, lstSessions,
                    chkAI, lblNote,
                    btnOk, btnCancel
                });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                // SeÃ§ilen oturumlarÄ± topla
                var selectedSessions = lstSessions.CheckedItems.Cast<SessionRow>().ToList();
                if (selectedSessions.Count < 2)
                {
                    MessageBox.Show(this, "En az 2 oturum secmelisiniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedUserId = (int)cmbUser.SelectedValue;
                var selectedExperiment = cmbExperiment.SelectedIndex == 0 ? null : cmbExperiment.SelectedItem?.ToString();
                var analysisType = cmbAnalysisType.SelectedItem?.ToString();
                var useAI = chkAI.Checked;
                var sessionIds = selectedSessions.Select(s => s.OturumID).ToList();

                try
                {
                    SetAnalysisBusyState(true);
                    Cursor = Cursors.WaitCursor;

                    var result = await _analysisComputationService.ComputeBatchComparisonAsync(
                        selectedUserId,
                        selectedExperiment,
                        sessionIds,
                        analysisType,
                        useAI);

                    // VeritabanÄ±na kaydet
                    var saved = await _analysisService.CreateAsync(result);

                    // Grid'i yenile
                    await RefreshAnalysesAsync();

                    MessageBox.Show(this,
                        $"Toplu karsilastirma tamamlandi!\n{selectedSessions.Count} oturum analiz edildi.",
                        "Basarili",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("Toplu karsilastirma sirasinda hata olustu.", ex);
                }
                finally
                {
                    SetAnalysisBusyState(false);
                    Cursor = Cursors.Default;
                }
            }
        }

        private async void btnRefreshLogs_Click(object sender, EventArgs e)
        {
            await RefreshLogsAsync();
        }

        private async void btnClearLogs_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                this,
                "Tum audit loglarini kalici olarak silmek istediginizden emin misiniz?\n\nBu islem geri alinamaz!",
                "Loglari Temizle",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                btnClearLogs.Enabled = false;
                btnRefreshLogs.Enabled = false;
                Cursor = Cursors.WaitCursor;

                await _auditLogService.DeleteAllAsync();
                
                // Log grid'i temizle
                _logs.Clear();
                
                // Temizleme iÅŸlemini logla
                await _auditLogService.LogAsync("LoglarTemizlendi", "Tum audit loglari temizlendi", _currentUserId, _currentUserName, "Warning");
                
                // Grid'i yenile
                await RefreshLogsAsync();
                
                MessageBox.Show(this, "Tum loglar basariyla temizlendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Loglar temizlenirken hata olustu.", ex);
            }
            finally
            {
                btnClearLogs.Enabled = true;
                btnRefreshLogs.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private async Task RefreshLogsAsync()
        {
            try
            {
                btnRefreshLogs.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var logs = await _auditLogService.GetRecentAsync(500);
                _logs = new BindingList<AuditLog>(logs.Select(ConvertLogToLocalTime).ToList());
                _logsBindingSource.DataSource = _logs;
            }
            catch (Exception ex)
            {
                ShowError("Loglar yuklenirken hata olustu.", ex);
            }
            finally
            {
                btnRefreshLogs.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private AuditLog ConvertLogToLocalTime(AuditLog log)
        {
            return new AuditLog
            {
                LogID = log.LogID,
                Tarih = log.Tarih.Kind == DateTimeKind.Utc ? log.Tarih.ToLocalTime() : log.Tarih,
                KullaniciID = log.KullaniciID,
                KullaniciAdi = log.KullaniciAdi ?? "-",
                Islem = log.Islem,
                Detay = log.Detay,
                Seviye = log.Seviye
            };
        }

        private sealed class SessionRow
        {
            public int OturumID { get; set; }
            public int KullaniciID { get; set; }
            public string KullaniciAd { get; set; }
            public string DeneyTuru { get; set; }
            public string ZamanEtiketi { get; set; }
            public DateTime? KayitBaslangic { get; set; }
            public DateTime? KayitBitis { get; set; }
            public string Notlar { get; set; }

            public string DisplayName
            {
                get
                {
                    var timeLabel = string.IsNullOrWhiteSpace(ZamanEtiketi) ? "-" : ZamanEtiketi;
                    var experiment = string.IsNullOrWhiteSpace(DeneyTuru) ? "-" : DeneyTuru;
                    return $"{OturumID} - {KullaniciAd} ({experiment}/{timeLabel})";
                }
            }
        }

        #region SÄ±nav ModÃ¼lÃ¼

        private void InitializeSinavTab()
        {
            // KAYIT DURUMU PANEL
            var pnlRecordStatus = new Panel
            {
                Name = "pnlRecordStatus",
                Left = 20,
                Top = 20,
                Width = 700,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblRecordStatus = new Label
            {
                Name = "lblRecordStatus",
                Text = "ğŸ”´ EEG KaydÄ± YOK - Ã–nce EEG Verisi sekmesinden kayÄ±t baÅŸlatÄ±n",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.Red
            };
            pnlRecordStatus.Controls.Add(lblRecordStatus);

            // JSON YÃœKLEME
            var lblLoadExam = new Label
            {
                Text = "SÄ±nav JSON DosyasÄ±:",
                Left = 20,
                Top = 120,
                Width = 150
            };

            var btnLoadExam = new Button
            {
                Name = "btnLoadExam",
                Text = "JSON YÃ¼kle",
                Left = 180,
                Top = 115,
                Width = 120,
                Height = 30
            };
            btnLoadExam.Click += BtnLoadExam_Click;

            var lblExamInfo = new Label
            {
                Name = "lblExamInfo",
                Text = "HenÃ¼z sÄ±nav yÃ¼klenmedi",
                Left = 310,
                Top = 120,
                Width = 400,
                ForeColor = System.Drawing.Color.Gray
            };

            // SINAV BAÅLATMA
            var btnStartExam = new Button
            {
                Name = "btnStartExam",
                Text = "SÄ±nava BaÅŸla",
                Left = 20,
                Top = 160,
                Width = 150,
                Height = 40,
                BackColor = System.Drawing.Color.LightGreen,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Enabled = false
            };
            btnStartExam.Click += BtnStartExam_Click;

            var btnShowSample = new Button
            {
                Text = "JSON Format Ã–rneÄŸi",
                Left = 180,
                Top = 160,
                Width = 150,
                Height = 40
            };
            btnShowSample.Click += BtnShowSampleJson_Click;

            // SINAV PANELÄ° (baÅŸlangÄ±Ã§ta gizli)
            var pnlExam = new Panel
            {
                Name = "pnlExam",
                Left = 20,
                Top = 220,
                Width = 800,
                Height = 350,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            tabPageSinav.Controls.AddRange(new Control[]
            {
                pnlRecordStatus,
                lblLoadExam, btnLoadExam, lblExamInfo,
                btnStartExam, btnShowSample,
                pnlExam
            });

            // Timer ile kayÄ±t durumunu kontrol et
            var recordCheckTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            recordCheckTimer.Tick += (s, e) => UpdateExamRecordStatus();
            recordCheckTimer.Start();
        }

        private void UpdateExamRecordStatus()
        {
            var lblStatus = tabPageSinav.Controls.Find("lblRecordStatus", true).FirstOrDefault() as Label;
            if (lblStatus == null) return;

            if (_streamingSessionId.HasValue)
            {
                lblStatus.Text = $"ğŸŸ¢ EEG KaydÄ± AKTÄ°F - Oturum #{_streamingSessionId.Value}";
                lblStatus.ForeColor = System.Drawing.Color.Green;
                
                var btnStart = tabPageSinav.Controls.Find("btnStartExam", true).FirstOrDefault() as Button;
                if (btnStart != null)
                    btnStart.Enabled = _loadedExam != null;
            }
            else
            {
                lblStatus.Text = "ğŸ”´ EEG KaydÄ± YOK - Ã–nce EEG Verisi sekmesinden kayÄ±t baÅŸlatÄ±n";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                
                var btnStart = tabPageSinav.Controls.Find("btnStartExam", true).FirstOrDefault() as Button;
                if (btnStart != null)
                    btnStart.Enabled = false;
            }
        }

        private void BtnLoadExam_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog
            {
                Title = "SÄ±nav JSON DosyasÄ± SeÃ§",
                Filter = "JSON (*.json)|*.json"
            })
            {
                if (openDialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    _loadedExam = _examLoaderService.LoadFromJson(openDialog.FileName);

                    var lblInfo = tabPageSinav.Controls.Find("lblExamInfo", true).FirstOrDefault() as Label;
                    if (lblInfo != null)
                    {
                        lblInfo.Text = $"âœ“ {_loadedExam.SinavTuru} ({_loadedExam.Sorular.Count} soru)";
                        lblInfo.ForeColor = System.Drawing.Color.Green;
                    }

                    UpdateExamRecordStatus(); // Buton durumunu gÃ¼ncelle
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"SÄ±nav yÃ¼klenemedi: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnStartExam_Click(object sender, EventArgs e)
        {
            if (!_streamingSessionId.HasValue)
            {
                MessageBox.Show(this, "Ã–nce EEG Verisi sekmesinden kayÄ±t baÅŸlatÄ±n!", 
                    "UyarÄ±", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_loadedExam == null)
            {
                MessageBox.Show(this, "Ã–nce bir sÄ±nav yÃ¼kleyin!", 
                    "UyarÄ±", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // SÄ±nav baÅŸlat
            _examStartTime = DateTime.UtcNow;
            _userAnswers.Clear();
            _currentQuestionIndex = 0;
            
            ShowExamPanel();
            LoadExamQuestion(_currentQuestionIndex);
        }

        private void ShowExamPanel()
        {
            var pnlExam = tabPageSinav.Controls.Find("pnlExam", true).FirstOrDefault() as Panel;
            if (pnlExam == null) return;
            
            pnlExam.Controls.Clear();
            pnlExam.Visible = true;

            // Soru paneli
            var pnlQuestion = new Panel
            {
                Name = "pnlQuestion",
                Dock = DockStyle.Fill
            };

            // Alt panel (butonlar)
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var btnPrev = new Button
            {
                Name = "btnPrev",
                Text = "â—€ Ã–nceki",
                Left = 10,
                Top = 5,
                Width = 100,
                Height = 40
            };
            btnPrev.Click += (s, e) =>
            {
                if (_currentQuestionIndex > 0)
                    LoadExamQuestion(_currentQuestionIndex - 1);
            };

            var lblProgress = new Label
            {
                Name = "lblProgress",
                Text = "Soru 1 / " + _loadedExam.Sorular.Count,
                Left = 120,
                Top = 15,
                Width = 200,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            var btnNext = new Button
            {
                Name = "btnNext",
                Text = "Sonraki â–¶",
                Left = 330,
                Top = 5,
                Width = 100,
                Height = 40
            };
            btnNext.Click += (s, e) =>
            {
                if (_currentQuestionIndex < _loadedExam.Sorular.Count - 1)
                    LoadExamQuestion(_currentQuestionIndex + 1);
            };

            var btnFinish = new Button
            {
                Name = "btnFinish",
                Text = "SÄ±navÄ± Bitir",
                Left = 650,
                Top = 5,
                Width = 120,
                Height = 40,
                BackColor = System.Drawing.Color.LightCoral,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            btnFinish.Click += BtnFinishExam_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnPrev, lblProgress, btnNext, btnFinish });
            pnlExam.Controls.Add(pnlQuestion);
            pnlExam.Controls.Add(pnlBottom);
        }

        private void LoadExamQuestion(int index)
        {
            if (index < 0 || index >= _loadedExam.Sorular.Count)
                return;

            _currentQuestionIndex = index;
            var question = _loadedExam.Sorular[index];

            var pnlExam = tabPageSinav.Controls.Find("pnlExam", true).FirstOrDefault() as Panel;
            var pnlQuestion = pnlExam?.Controls.Find("pnlQuestion", true).FirstOrDefault() as Panel;
            if (pnlQuestion == null) return;

            pnlQuestion.Controls.Clear();

            // Soru metni
            var lblQuestion = new Label
            {
                Text = $"Soru {question.SoruNo}: {question.SoruMetni}",
                Left = 20,
                Top = 20,
                Width = 750,
                Height = 60,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold)
            };
            pnlQuestion.Controls.Add(lblQuestion);

            // ÅÄ±klar
            var options = new[] { "A", "B", "C", "D" };
            var yPos = 90;

            for (int i = 0; i < question.Siklar.Count && i < 4; i++)
            {
                var rb = new RadioButton
                {
                    Name = $"rb{options[i]}",
                    Text = $"{options[i]}) {question.Siklar[i]}",
                    Left = 40,
                    Top = yPos,
                    Width = 700,
                    Height = 35,
                    Font = new System.Drawing.Font("Segoe UI", 10),
                    Tag = options[i]
                };
                rb.CheckedChanged += (s, e) =>
                {
                    if (rb.Checked)
                        _userAnswers[question.SoruNo] = rb.Tag.ToString();
                };

                if (_userAnswers.TryGetValue(question.SoruNo, out var answer) && answer == options[i])
                    rb.Checked = true;

                pnlQuestion.Controls.Add(rb);
                yPos += 45;
            }

            // Progress gÃ¼ncelle
            var lblProgress = pnlExam.Controls.Find("lblProgress", true).FirstOrDefault() as Label;
            if (lblProgress != null)
                lblProgress.Text = $"Soru {index + 1} / {_loadedExam.Sorular.Count}";

            // Buton durumlarÄ±
            var btnPrev = pnlExam.Controls.Find("btnPrev", true).FirstOrDefault() as Button;
            var btnNext = pnlExam.Controls.Find("btnNext", true).FirstOrDefault() as Button;
            if (btnPrev != null) btnPrev.Enabled = index > 0;
            if (btnNext != null) btnNext.Enabled = index < _loadedExam.Sorular.Count - 1;
        }

        private async void BtnFinishExam_Click(object sender, EventArgs e)
        {
            var unanswered = _loadedExam.Sorular.Count - _userAnswers.Count;
            if (unanswered > 0)
            {
                var result = MessageBox.Show(this,
                    $"{unanswered} soru cevaplanmadi. Yine de bitirmek istiyor musunuz?",
                    "Uyari", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                // SonuÃ§larÄ± hesapla
                var dogruSayisi = 0;
                var yanlisSayisi = 0;
                var answers = new List<ExamAnswer>();

                foreach (var question in _loadedExam.Sorular)
                {
                    var userAnswer = _userAnswers.ContainsKey(question.SoruNo)
                        ? _userAnswers[question.SoruNo] : null;

                    var answer = new ExamAnswer
                    {
                        SoruNo = question.SoruNo,
                        VerilenCevap = userAnswer,
                        DogruCevap = question.DogruCevap
                    };
                    answers.Add(answer);

                    if (answer.Dogru)
                        dogruSayisi++;
                    else if (userAnswer != null)
                        yanlisSayisi++;
                }

                // VeritabanÄ±na kaydet
                var examResult = new SinavSonucu
                {
                    OturumID = _streamingSessionId.Value,
                    SinavTuru = _loadedExam.SinavTuru,
                    ToplamSoru = _loadedExam.Sorular.Count,
                    DogruSayisi = dogruSayisi,
                    YanlisSayisi = yanlisSayisi,
                    BaslamaTarihi = _examStartTime,
                    BitisTarihi = DateTime.UtcNow,
                    Sure = ((int)(DateTime.UtcNow - _examStartTime).TotalMinutes).ToString()
                };

                await _examService.CreateAsync(examResult);

                // âœ… OTOMATIK KAYIT DURDUR
                if (_streamingSessionId.HasValue)
                {
                    await StopStreamAsync("Sinav tamamlandi");
                }

                // SonuÃ§larÄ± gÃ¶ster
                ShowExamResults(answers, dogruSayisi, yanlisSayisi);

                // SÄ±nav panelini gizle
                var pnlExam = tabPageSinav.Controls.Find("pnlExam", true).FirstOrDefault() as Panel;
                if (pnlExam != null)
                    pnlExam.Visible = false;

                MessageBox.Show(this, 
                    "SÄ±nav tamamlandÄ± ve EEG kaydÄ± durduruldu!", 
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowExamResults(List<ExamAnswer> answers, int dogru, int yanlis)
        {
            var form = new Form
            {
                Text = "SÄ±nav SonuÃ§larÄ±",
                Size = new System.Drawing.Size(600, 500),
                StartPosition = FormStartPosition.CenterParent
            };

            var txt = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 10)
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
            sb.AppendLine("           SINAV SONUÃ‡LARI");
            sb.AppendLine("â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
            sb.AppendLine();
            sb.AppendLine($"Toplam Soru: {_loadedExam.Sorular.Count}");
            sb.AppendLine($"DoÄŸru: {dogru}");
            sb.AppendLine($"YanlÄ±ÅŸ: {yanlis}");
            sb.AppendLine($"BoÅŸ: {_loadedExam.Sorular.Count - dogru - yanlis}");
            sb.AppendLine($"BaÅŸarÄ±: %{(dogru * 100.0 / _loadedExam.Sorular.Count):F1}");
            sb.AppendLine();
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            sb.AppendLine("DETAYLI SONUÃ‡LAR:");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            foreach (var answer in answers)
            {
                var status = answer.VerilenCevap == null ? "BOS" :
                            answer.Dogru ? "DOGRU" : "YANLIS";
                var icon = answer.VerilenCevap == null ? "âšª" :
                          answer.Dogru ? "âœ“" : "âœ—";

                sb.AppendLine($"{icon} Soru {answer.SoruNo}: {status}");
                if (answer.VerilenCevap != null)
                    sb.AppendLine($"  CevabÄ±nÄ±z: {answer.VerilenCevap} | DoÄŸru: {answer.DogruCevap}");
            }

            txt.Text = sb.ToString();

            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 40,
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(txt);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }

        private void BtnShowSampleJson_Click(object sender, EventArgs e)
        {
            var sample = _examLoaderService.GetSampleJsonFormat();

            var form = new Form
            {
                Text = "JSON Format Ã–rneÄŸi",
                Size = new System.Drawing.Size(600, 500),
                StartPosition = FormStartPosition.CenterParent
            };

            var txt = new TextBox
            {
                Text = sample,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9),
                ReadOnly = true
            };

            var btnSave = new Button
            {
                Text = "Dosyaya Kaydet",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            btnSave.Click += (s, args) =>
            {
                using (var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON (*.json)|*.json",
                    FileName = "ornek_sinav.json"
                })
                {
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(saveDialog.FileName, sample);
                        MessageBox.Show("Kaydedildi!");
                    }
                }
            };

            form.Controls.Add(txt);
            form.Controls.Add(btnSave);
            form.ShowDialog(this);
        }

        #endregion

        private sealed class AnalysisRow
        {
            public int AnalizID { get; set; }
            public int? OturumID { get; set; }
            public string OturumBilgisi { get; set; }
            public string AnalizTipi { get; set; }
            public string MetrikOzeti { get; set; }
            public bool AiYorumu { get; set; }
            public DateTime AnalizTarihi { get; set; }
            public string Summary { get; set; }
            public string MetricsJSON { get; set; }
        }
    }
}











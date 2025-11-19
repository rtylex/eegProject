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
    /// <summary>
    /// EEG Yönetim Paneli - Ana Form
    /// Kullanıcı, Oturum, EEG Verisi, Analiz, Sınav Modülü ve Denetim Günlüklerini yönetir
    /// </summary>
    public partial class Form1 : Form
    {
        #region FIELDS - Servisler, Binding'ler ve State Değişkenleri

        // ===== SERVICES =====
        private readonly UserService _userService = new UserService();
        private readonly SessionService _sessionService = new SessionService();
        private readonly EegDataService _eegDataService = new EegDataService();
        private readonly DeneyTuruService _deneyTuruService = new DeneyTuruService();
        private readonly ZamanEtiketiService _zamanEtiketiService = new ZamanEtiketiService();
        private readonly MindwaveStreamService _mindwaveStreamService = new MindwaveStreamService();
        private readonly ExportService _exportService = new ExportService();
        private readonly AnalysisService _analysisService = new AnalysisService();
        private readonly AnalysisComputationService _analysisComputationService = new AnalysisComputationService();
        private readonly AuditLogService _auditLogService = new AuditLogService();
        private readonly ExamService _examService = new ExamService();
        private readonly ExamLoaderService _examLoaderService = new ExamLoaderService();
        private readonly ModulYetkisiService _modulYetkisiService = new ModulYetkisiService();
        private readonly SinavAtamaService _sinavAtamaService = new SinavAtamaService();
        private readonly SinavCevapService _sinavCevapService = new SinavCevapService();

        // ===== BINDING SOURCES =====
        private readonly BindingSource _userBindingSource = new BindingSource();
        private readonly BindingSource _sessionBindingSource = new BindingSource();
        private readonly BindingSource _eegBindingSource = new BindingSource();
        private readonly BindingSource _analysisBindingSource = new BindingSource();
        private readonly BindingSource _userNotesBindingSource = new BindingSource();
        private readonly BindingSource _logsBindingSource = new BindingSource();

        // ===== DATA COLLECTIONS =====
        private BindingList<Kullanici> _users = new BindingList<Kullanici>();
        private BindingList<SessionRow> _sessions = new BindingList<SessionRow>();
        private BindingList<EEGVerisi> _eegSamples = new BindingList<EEGVerisi>();
        private BindingList<AnalysisRow> _analyses = new BindingList<AnalysisRow>();
        private BindingList<AuditLog> _logs = new BindingList<AuditLog>();
        private readonly BindingList<SessionRow> _streamSessionOptions = new BindingList<SessionRow>();
        private List<DeneyTuru> _deneyTurleri = new List<DeneyTuru>();
        private List<ZamanEtiketi> _zamanEtiketleri = new List<ZamanEtiketi>();

        // ===== UI STATE =====
        private bool _userBusy;
        private bool _sessionBusy;
        private bool _analysisBusy;

        // ===== STREAMING STATE =====
        private CancellationTokenSource _streamCts;
        private Task _streamTask;
        private int? _streamingSessionId;
        private const int MaxVisibleEegSamples = 200;

        // ===== EXAM MODULE STATE =====
        private ExamData _loadedExam;
        private SinavAtama _currentAtama; // Atanmış sınav
        private Dictionary<int, string> _userAnswers = new Dictionary<int, string>();
        private Dictionary<int, int> _questionTimes = new Dictionary<int, int>(); // Soru bazlı süreler (saniye)
        private System.Diagnostics.Stopwatch _questionStopwatch = new System.Diagnostics.Stopwatch(); // Soru kronometresi
        private int _currentQuestionIndex = 0;
        private DateTime _examStartTime;

        // ===== CURRENT USER INFO =====
        private readonly int _currentUserId;
        private readonly string _currentUserRole;
        private readonly string _currentUserName;

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Form constructor - Giriş yapan kullanıcı bilgileriyle başlatılır
        /// </summary>
        public Form1(int currentUserId, string currentUserRole, string currentUserName)
        {
            _currentUserId = currentUserId;
            _currentUserRole = currentUserRole ?? "Kullanici";
            _currentUserName = currentUserName ?? "Bilinmiyor";
            InitializeComponent();
            InitializeGrids();
            InitializeUserNotesTab();
            InitializeSinavTab();
            InitializeModulYetkisiTab();
            UpdateStreamStatus("Hazir");
        }

        #endregion

        #region INITIALIZATION - Form Load ve Grid Hazırlama

        private void InitializeGrids()
        {
            InitializeUserGrid();
            InitializeSessionGrid();
            InitializeEegGrid();
            InitializeAnalysisGrid();
            InitializeLogsGrid();
        }
        private async Task ConfigureUIByRoleAsync()
        {
            // Form ba�l���nda kullan�c� bilgisini g�ster
            this.Text = $"EEG Yonetim Paneli - {_currentUserName} ({_currentUserRole})";
            bool isAdmin = string.Equals(_currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isYonetici = string.Equals(_currentUserRole, "Yonetici", StringComparison.OrdinalIgnoreCase);
            // ==========================================
            // NORMAL KULLANICI: Sadece EEG Verisi + S�nav Mod�l� (yetkisi varsa)
            // ==========================================
            if (!isAdmin && !isYonetici)
            {
                // T�m sekmeleri kald�r
                tabMain.TabPages.Clear();
                
                // EEG Verisi sekmesini ekle (her kullan�c� g�rebilir)
                tabMain.TabPages.Add(tabPageEEG);
                // S�nav Mod�l� yetkisi var m� kontrol et
                bool hasSinavAccess = await _modulYetkisiService.HasModuleAccessAsync(_currentUserId, "SinavModulu");
                if (hasSinavAccess)
                {
                    // S�nav Mod�l� sekmesini ekle
                    tabMain.TabPages.Add(tabPageSinav);
                }
                // EEG sekmesinde sadece kendi oturumlar�n� g�rebilsin
                // Bu filtreleme RefreshSessionsAsync'te yap�lacak
            }
            // ==========================================
            // ADMIN/Y�NET�C�: T�m sekmeler g�r�n�r
            // ==========================================
            else
            {
                // Y�netici i�in t�m sekmeler g�r�n�r (default zaten t�m� var)
                // Mod�l Yetkileri sekmesi de g�r�n�r (zaten admin/y�neticiyiz)
            }
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
            // Sol taraf - Kullan�c� listesi
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
            // Sa� taraf - Not alan�
            txtUserNotes.Enabled = false;
            txtUserNotes.Leave += TxtUserNotes_Leave; // Otomatik kaydetme
            btnSaveNotes.Click += BtnSaveNotes_Click;
        }
        private Kullanici _currentEditingUser;
        private async void TxtUserNotes_Leave(object sender, EventArgs e)
        {
            // TextBox'tan ��karken otomatik kaydet
            await SaveCurrentUserNotesAsync();
        }
        private async void GridUsersForNotes_SelectionChanged(object sender, EventArgs e)
        {
            // �nceki kullan�c�n�n notlar�n� kaydet
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
            // De�i�iklik var m� kontrol et
            var currentText = txtUserNotes.Text ?? string.Empty;
            var savedText = _currentEditingUser.Notlar ?? string.Empty;
            if (currentText == savedText)
            {
                return; // De�i�iklik yok, kaydetmeye gerek yok
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
            // Manuel kaydetme - direkt kaydeder ve mesaj g�sterir
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
                HeaderText = "Kayit Zaman?",
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
                HeaderText = "Metrik �zeti",
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
        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadLookupAsync();
            
            // Rol bazl� UI yap�land�rmas� (mod�l yetkileri dahil)
            await ConfigureUIByRoleAsync();
            
            bool isAdmin = string.Equals(_currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isYonetici = string.Equals(_currentUserRole, "Yonetici", StringComparison.OrdinalIgnoreCase);
            if (isAdmin || isYonetici)
            {
                // Y�netici t�m kullan�c�lar� ve oturumlar� g�rebilir
                await RefreshUsersAsync();
                await RefreshSessionsAsync();
                await RefreshAnalysesAsync();
                await RefreshLogsAsync();
                InitializeExportControls();
                await RefreshModulYetkisiAsync(); // Mod�l yetkileri verilerini y�kle
            }
            else
            {
                // Kullan�c� sadece kendi oturumlar�n� g�rebilir
                await RefreshSessionsForCurrentUserAsync();
            }
            UpdateStreamControls(IsStreaming(), "Hazir");
            // Giri� logla
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
                // Sadece mevcut kullan�c�n�n oturumlar�n� filtrele
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
            if (_deneyTurleri != null && _deneyTurleri.Count > 0)
            {
                experimentOptions.AddRange(
                    _deneyTurleri
                        .Where(d => !string.IsNullOrWhiteSpace(d?.TurAdi))
                        .Select(d => d.TurAdi.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
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
                _deneyTurleri = await _deneyTuruService.GetActiveAsync();
                _zamanEtiketleri = await _zamanEtiketiService.GetActiveAsync();
            }
            catch (Exception ex)
            {
                _deneyTurleri = new List<DeneyTuru>();
                _zamanEtiketleri = new List<ZamanEtiketi>();
                ShowError("Lookup verisi yuklenirken hata olustu.", ex);
            }
            RefreshExportControls();
        }

        #endregion

        #region USER MANAGEMENT - Kullanıcı CRUD İşlemleri

        private async Task RefreshUsersAsync()
        {
            SetUserBusyState(true);
            try
            {
                var users = await _userService.GetAllAsync();
                _users = new BindingList<Kullanici>(users);
                _userBindingSource.DataSource = _users;
                _userNotesBindingSource.DataSource = _users; // Kullan�c� Notlar� sekmesi i�in
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
            AddRange(_zamanEtiketleri?.Select(z => z?.EtiketAdi));
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

        #endregion

        #region SESSION MANAGEMENT - Oturum Yönetimi

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
            using (var dialog = new SessionEditForm("Yeni Oturum", _users, 
                _deneyTurleri.Select(d => d.TurAdi).ToList(), 
                _zamanEtiketleri.Select(z => z.EtiketAdi).ToList()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    var model = dialog.BuildSessionModel();
                    var created = await _sessionService.CreateAsync(model);
                    // Lookup verileri art�k SessionService taraf�ndan otomatik ekleniyor
                    await LoadLookupAsync(); // Listeyi yenile
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
            using (var dialog = new SessionEditForm("Oturum Duzenle", _users, 
                _deneyTurleri.Select(d => d.TurAdi).ToList(), 
                _zamanEtiketleri.Select(z => z.EtiketAdi).ToList(), existing))
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
                    // Lookup verileri art�k SessionService taraf�ndan otomatik ekleniyor
                    await LoadLookupAsync(); // Listeyi yenile
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

        #endregion

        #region EEG DATA & STREAMING - EEG Veri Gösterimi ve Streaming

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
            await StopStreamAsync("Kullanıcı durdurdu");
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
            // A��k notlar� kaydet
            await SaveCurrentUserNotesAsync();
            
            await StopStreamAsync("Kapatiliyor");
        }

        #endregion

        #region ANALYSIS - Analiz Hesaplama ve Görüntüleme

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

        #endregion

        #region EXPORT - Excel ve JSON Export İşlemleri

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
            // OturumBilgisi olu�tur
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
                oturumBilgisi = "Çoklu Oturum";
            }
            // MetricsJSON'dan �zet ��kar
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
                            metrikOzeti = $"İndeks: {metrics.RahatlamaIndeksi} | Samples: {metrics.SampleCount}";
                        }
                        else if (metrics.DikkatSkoru != null)
                        {
                            metrikOzeti = $"Skor: {metrics.DikkatSkoru} | Samples: {metrics.SampleCount}";
                        }
                        else if (metrics.EngagementIndex != null)
                        {
                            metrikOzeti = $"İndeks: {metrics.EngagementIndex} | Samples: {metrics.SampleCount}";
                        }
                    }
                }
            }
            catch
            {
                metrikOzeti = "Parse hatasI";
            }
            // AI yorumu var m�?
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
                case "StresAnalizi":
                    return "Stres";
                case "YorgunlukAnalizi":
                    return "Yorgunluk";
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
            // Analiz dialog'u g�ster
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
                cmbAnalysisType.Items.Add("Stres Analizi");
                cmbAnalysisType.Items.Add("Yorgunluk Analizi");
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
                var btnCancel = new Button { Text = "VazgeÇ", Left = 350, Width = 80, Top = 220, DialogResult = DialogResult.Cancel };
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
                var selectedAnalysis = cmbAnalysisType.SelectedItem?.ToString();
                var useAI = chkAI.Checked;
                try
                {
                    SetAnalysisBusyState(true);
                    Cursor = Cursors.WaitCursor;
                    AnalizSonucu result;
                string analysisLabel;
                switch (selectedAnalysis)
                {
                    case "Rahatlama Analizi":
                        result = await _analysisComputationService.ComputeRahatlamaAnaliziAsync(selectedSession.OturumID, useAI);
                        analysisLabel = "Rahatlama";
                        break;
                    case "Dikkat Analizi":
                        result = await _analysisComputationService.ComputeDikkatAnaliziAsync(selectedSession.OturumID, useAI);
                        analysisLabel = "Dikkat";
                        break;
                    case "Engagement Analizi":
                        result = await _analysisComputationService.ComputeEngagementAnaliziAsync(selectedSession.OturumID, useAI);
                        analysisLabel = "Engagement";
                        break;
                    case "Stres Analizi":
                        result = await _analysisComputationService.ComputeStresAnaliziAsync(selectedSession.OturumID, useAI);
                        analysisLabel = "Stres";
                        break;
                    case "Yorgunluk Analizi":
                        result = await _analysisComputationService.ComputeYorgunlukAnaliziAsync(selectedSession.OturumID, useAI);
                        analysisLabel = "Yorgunluk";
                        break;
                    default:
                        throw new InvalidOperationException("Gecersiz analiz tipi secildi.");
                }
                    // Veritaban�na kaydet
                    var saved = await _analysisService.CreateAsync(result);
                    // Grid'i yenile
                    await RefreshAnalysesAsync();
                    // Log
                    await _auditLogService.LogAsync("AnalizTamamlandi", $"Analiz: {analysisLabel} - Oturum:{selectedSession.OturumID}", _currentUserId, _currentUserName);
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
            // Detay dialog'u g�ster
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
            // Toplu kar��la�t�rma dialog'u g�ster (BAZAL REFERANSLI)
            using (var dialog = new Form
            {
                Text = "Toplu Oturum Karsilastirmasi (Bazal Referansli)",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(500, 620),
                AutoScroll = true
            })
            {
                // Kullan�c� se�imi
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
                // Deney t�r� se�imi
                var lblExperiment = new Label { Text = "Deney Turu:", Left = 20, Top = 60, Width = 100 };
                var cmbExperiment = new ComboBox
                {
                    Left = 130,
                    Top = 58,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbExperiment.Items.Add("Tum Deney Turleri");
                if (_deneyTurleri != null && _deneyTurleri.Count > 0)
                {
                    foreach (var exp in _deneyTurleri
                        .Where(d => !string.IsNullOrWhiteSpace(d?.TurAdi))
                        .Select(d => d.TurAdi.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                    {
                        cmbExperiment.Items.Add(exp);
                    }
                }
                cmbExperiment.SelectedIndex = 0;
                // Analiz tipi se�imi
                var lblAnalysisType = new Label { Text = "Analiz Tipi:", Left = 20, Top = 100, Width = 100 };
                var cmbAnalysisType = new ComboBox
                {
                    Left = 130,
                    Top = 98,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbAnalysisType.Items.AddRange(new object[] { "Rahatlama", "Dikkat", "Engagement", "Stres", "Yorgunluk" });
                cmbAnalysisType.SelectedIndex = 0;
                // Oturum se�imi (CheckedListBox)
                var lblSessions = new Label { Text = "Oturumlar:", Left = 20, Top = 140, Width = 460, Height = 20 };
                var lstSessions = new CheckedListBox
                {
                    Left = 20,
                    Top = 165,
                    Width = 460,
                    Height = 200,
                    CheckOnClick = true,
                    DisplayMember = nameof(SessionRow.DisplayName)
                };
                // Kullan�c� de�i�ti�inde oturumlar� g�ncelle
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
                        lstSessions.Items.Add(session, true); // Varsay�lan olarak i�aretli
                    }
                };
                cmbUser.SelectedIndexChanged += (s, e) => updateSessions();
                cmbExperiment.SelectedIndexChanged += (s, e) => updateSessions();
                updateSessions();
                // BAZAL OTURUM SE��M� (ZORUNLU)
                var lblBazal = new Label { Text = "BAZAL Oturum:", Left = 20, Top = 380, Width = 100, Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold) };
                var cmbBazal = new ComboBox
                {
                    Left = 130,
                    Top = 378,
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                // lstSessions de�i�ti�inde bazal combobox'� g�ncelle
                EventHandler updateBazal = (s, e) =>
                {
                    cmbBazal.DataSource = null;
                    var checkedSessions = lstSessions.CheckedItems.Cast<SessionRow>().ToList();
                    if (checkedSessions.Count > 0)
                    {
                        cmbBazal.DataSource = checkedSessions;
                        cmbBazal.DisplayMember = nameof(SessionRow.DisplayName);
                        cmbBazal.ValueMember = nameof(SessionRow.OturumID);
                        cmbBazal.SelectedIndex = 0; // �lk oturumu varsay�lan bazal yap
                    }
                };
                lstSessions.ItemCheck += (s, e) => {
                    // ItemCheck event'i hemen tetiklendi�i i�in BeginInvoke ile gecikmeli �a��r
                    BeginInvoke((Action)(() => updateBazal(s, e)));
                };
                updateBazal(null, null); // �lk y�kleme
                var lblBazalInfo = new Label
                {
                    Text = "?? Tüm karşılaştırmalar BAZAL oturuma göre yapılacak!",
                    Left = 130,
                    Top = 408,
                    Width = 350,
                    Height = 30,
                    ForeColor = System.Drawing.Color.DarkOrange,
                    Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Italic)
                };
                // AI checkbox
                var isAiAvailable = _analysisComputationService.IsAiAvailable;
                var chkAI = new CheckBox
                {
                    Text = isAiAvailable ? "AI Karsilastirmali Yorum " : "AI Yorum (API key eksik)",
                    Left = 20,
                    Top = 450,
                    Width = 460,
                    Enabled = isAiAvailable,
                    Checked = false
                };
                var lblNote = new Label
                {
                    Text = isAiAvailable
                        ? "Not: AI BAZAL referanslI yorumlama yapacak."
                        : "Not: AI ozelligi icin App.config'e OpenAI_ApiKey ekleyin.",
                    Left = 20,
                    Top = 480,
                    Width = 460,
                    Height = 40,
                    ForeColor = isAiAvailable ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed
                };
                // Butonlar
                var btnOk = new Button { Text = "Karsilastir", Left = 310, Width = 80, Top = 540, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Vazgec", Left = 400, Width = 80, Top = 540, DialogResult = DialogResult.Cancel };
                dialog.Controls.AddRange(new Control[]
                {
                    lblUser, cmbUser,
                    lblExperiment, cmbExperiment,
                    lblAnalysisType, cmbAnalysisType,
                    lblSessions, lstSessions,
                    lblBazal, cmbBazal, lblBazalInfo,
                    chkAI, lblNote,
                    btnOk, btnCancel
                });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                // Se�ilen oturumlar� topla
                var selectedSessions = lstSessions.CheckedItems.Cast<SessionRow>().ToList();
                if (selectedSessions.Count < 2)
                {
                    MessageBox.Show(this, "En az 2 oturum secmelisiniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // BAZAL OTURUM KONTROL�
                var baselineSession = cmbBazal.SelectedItem as SessionRow;
                if (baselineSession == null)
                {
                    MessageBox.Show(this, "Lutfen BAZAL oturum seciniz!", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var selectedUserId = (int)cmbUser.SelectedValue;
                var selectedExperiment = cmbExperiment.SelectedIndex == 0 ? null : cmbExperiment.SelectedItem?.ToString();
                var analysisType = cmbAnalysisType.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(analysisType))
                {
                    MessageBox.Show(this, "Lutfen analiz tipini seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var useAI = chkAI.Checked;
                var sessionIds = selectedSessions.Select(s => s.OturumID).ToList();
                var baselineSessionId = baselineSession.OturumID;
                try
                {
                    SetAnalysisBusyState(true);
                    Cursor = Cursors.WaitCursor;
                    var result = await _analysisComputationService.ComputeBatchComparisonAsync(
                        selectedUserId,
                        selectedExperiment,
                        sessionIds,
                        analysisType,
                        baselineSessionId, // BAZAL OTURUM ID
                        useAI);
                    // Veritaban�na kaydet
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

        #endregion

        #region AUDIT LOGS - Denetim Günlükleri

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
                
                // Temizleme i�lemini logla
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

        #endregion

        #region HELPER CLASSES - SessionRow

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

        #endregion

        #region Sinav Modulu - Gelismis Versiyon

        private void InitializeSinavTab()
        {
            int currentTop = 20;

            // YÖNETİCİ İŞLEMLERİ PANEL (sadece Admin/Yönetici için)
            bool isAdmin = string.Equals(_currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isYonetici = string.Equals(_currentUserRole, "Yonetici", StringComparison.OrdinalIgnoreCase);

            if (isAdmin || isYonetici)
            {
                var pnlYonetici = new Panel
                {
                    Name = "pnlYoneticiIslemleri",
                    Left = 20,
                    Top = currentTop,
                    Width = 800,
                    Height = 80,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = System.Drawing.Color.LightYellow
                };

                var lblYonetici = new Label
                {
                    Text = "👨‍💼 Yönetici İşlemleri",
                    Left = 10,
                    Top = 10,
                    Width = 200,
                    Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.DarkBlue
                };

                var btnSinavAta = new Button
                {
                    Name = "btnSinavAta",
                    Text = "📝 Sınav Ata",
                    Left = 10,
                    Top = 40,
                    Width = 150,
                    Height = 35,
                    BackColor = System.Drawing.Color.LightBlue,
                    Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
                };
                btnSinavAta.Click += BtnSinavAta_Click;

                var btnAtamalariGor = new Button
                {
                    Name = "btnAtamalariGor",
                    Text = "📋 Atamaları Görüntüle",
                    Left = 170,
                    Top = 40,
                    Width = 180,
                    Height = 35,
                    BackColor = System.Drawing.Color.LightGreen,
                    Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
                };
                btnAtamalariGor.Click += BtnAtamalariGor_Click;

                pnlYonetici.Controls.Add(lblYonetici);
                pnlYonetici.Controls.Add(btnSinavAta);
                pnlYonetici.Controls.Add(btnAtamalariGor);

                tabPageSinav.Controls.Add(pnlYonetici);
                currentTop += 90;
            }

            // ATANMIŞ SINAVLAR PANEL
            var pnlAtamalar = new Panel
            {
                Name = "pnlAtamalar",
                Left = 20,
                Top = currentTop,
                Width = 800,
                Height = 150,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblAtamalar = new Label
            {
                Text = "Atanmış Sınavlarım:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Padding = new Padding(5)
            };

            var listAtamalar = new ListBox
            {
                Name = "listAtamalar",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            listAtamalar.SelectedIndexChanged += ListAtamalar_SelectedIndexChanged;

            var btnYenile = new Button
            {
                Name = "btnYenileAtamalar",
                Text = "Yenile",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            btnYenile.Click += async (s, e) => await LoadAtananSinavlarAsync();

            pnlAtamalar.Controls.Add(listAtamalar);
            pnlAtamalar.Controls.Add(lblAtamalar);
            pnlAtamalar.Controls.Add(btnYenile);

            currentTop += 160;

            // SINAV BİLGİSİ
            var lblExamInfo = new Label
            {
                Name = "lblExamInfo",
                Text = "👆 Yukarıdan bir sınav seçin",
                Left = 20,
                Top = currentTop + 5,
                Width = 800,
                Height = 30,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkBlue,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            currentTop += 45;

            // KAYIT DURUMU
            var pnlRecordStatus = new Panel
            {
                Name = "pnlRecordStatus",
                Left = 20,
                Top = currentTop,
                Width = 800,
                Height = 60,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblRecordStatus = new Label
            {
                Name = "lblRecordStatus",
                Text = "⚠️ EEG kaydı YOK - Önce EEG Verisi sekmesinden kayıt başlatın",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.Red
            };
            pnlRecordStatus.Controls.Add(lblRecordStatus);

            currentTop += 75;

            // SINAV BASLATMA
            var btnStartExam = new Button
            {
                Name = "btnStartExam",
                Text = "Sınava Başla",
                Left = 20,
                Top = currentTop,
                Width = 150,
                Height = 45,
                BackColor = System.Drawing.Color.LightGreen,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                Enabled = false
            };
            btnStartExam.Click += BtnStartExam_Click;

            currentTop += 60;

            // SINAV PANELI (başlangıçta gizli)
            var pnlExam = new Panel
            {
                Name = "pnlExam",
                Left = 20,
                Top = currentTop,
                Width = 850,
                Height = 400,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            tabPageSinav.Controls.AddRange(new Control[]
            {
                pnlAtamalar,
                lblExamInfo,
                pnlRecordStatus,
                btnStartExam,
                pnlExam
            });

            // Timer ile kayıt durumunu kontrol et
            var recordCheckTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            recordCheckTimer.Tick += (s, e) => UpdateExamRecordStatus();
            recordCheckTimer.Start();

            // Atanmış sınavları yükle
            Task.Run(async () => await LoadAtananSinavlarAsync());
        }

        /// <summary>
        /// Yönetici - Kullanıcıya sınav atama formu
        /// </summary>
        private void BtnSinavAta_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new SinavAtamaForm(_currentUserId))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        MessageBox.Show(this, 
                            "Sınav başarıyla atandı!",
                            "Başarılı", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);

                        // Audit log
                        Task.Run(async () => await _auditLogService.LogAsync(
                            islem: "SinavAtandi",
                            detay: $"Yönetici tarafından sınav atandı",
                            kullaniciId: _currentUserId,
                            kullaniciAdi: _currentUserName
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Sınav atanırken hata oluştu:\n{ex.Message}",
                    "Hata", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Yönetici - Tüm sınav atamalarını görüntüle
        /// </summary>
        private void BtnAtamalariGor_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new SinavAtamaListForm(_currentUserId))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Atamalar görüntülenirken hata oluştu:\n{ex.Message}",
                    "Hata", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private async Task LoadAtananSinavlarAsync()
        {
            try
            {
                var atamalar = await _sinavAtamaService.GetPendingByUserAsync(_currentUserId);

                var listAtamalar = tabPageSinav.Controls.Find("listAtamalar", true).FirstOrDefault() as ListBox;
                if (listAtamalar != null)
                {
                    listAtamalar.Invoke((MethodInvoker)delegate
                    {
                        listAtamalar.Items.Clear();
                        listAtamalar.DisplayMember = "Display";
                        listAtamalar.ValueMember = "Value";

                        foreach (var atama in atamalar)
                        {
                            var display = $"{atama.SinavAdi} - {atama.SinavAciklama ?? ""}";
                            if (atama.SonGecerlilikTarihi.HasValue)
                            {
                                display += $" (Son: {atama.SonGecerlilikTarihi.Value:dd.MM.yyyy})";
                            }

                            listAtamalar.Items.Add(new { Display = display, Value = atama });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Atamalar yüklenirken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListAtamalar_SelectedIndexChanged(object sender, EventArgs e)
        {
            var list = sender as ListBox;
            if (list?.SelectedItem == null) return;

            dynamic item = list.SelectedItem;
            _currentAtama = item.Value as SinavAtama;

            if (_currentAtama != null)
            {
                try
                {
                    // JSON içerikten veya dosyadan yükle
                    if (!string.IsNullOrWhiteSpace(_currentAtama.SinavJsonContent))
                    {
                        _loadedExam = JsonConvert.DeserializeObject<ExamData>(_currentAtama.SinavJsonContent);
                    }
                    else if (!string.IsNullOrWhiteSpace(_currentAtama.SinavJsonPath))
                    {
                        _loadedExam = _examLoaderService.LoadFromJson(_currentAtama.SinavJsonPath);
                    }

                    var lblInfo = tabPageSinav.Controls.Find("lblExamInfo", true).FirstOrDefault() as Label;
                    if (lblInfo != null)
                    {
                        lblInfo.Text = $"✓ {_currentAtama.SinavAdi} ({_loadedExam.Sorular.Count} soru)";
                        lblInfo.ForeColor = System.Drawing.Color.Green;
                    }

                    UpdateExamRecordStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Sınav yüklenirken hata: {ex.Message}",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateExamRecordStatus()
        {
            var lblStatus = tabPageSinav.Controls.Find("lblRecordStatus", true).FirstOrDefault() as Label;
            var btnStart = tabPageSinav.Controls.Find("btnStartExam", true).FirstOrDefault() as Button;

            if (lblStatus == null || btnStart == null) return;

            if (_streamingSessionId.HasValue)
            {
                lblStatus.Text = $"✅ EEG kaydı AKTIF - Oturum #{_streamingSessionId.Value}";
                lblStatus.ForeColor = System.Drawing.Color.Green;
                btnStart.Enabled = _loadedExam != null;
            }
            else
            {
                lblStatus.Text = "⚠️ EEG kaydı YOK - Önce EEG Verisi sekmesinden kayıt başlatın";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                btnStart.Enabled = false;
            }
        }

        private void BtnStartExam_Click(object sender, EventArgs e)
        {
            if (!_streamingSessionId.HasValue)
            {
                MessageBox.Show(this, "Önce EEG Verisi sekmesinden kayıt başlatın!",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_loadedExam == null)
            {
                MessageBox.Show(this, "Önce yukarıdan bir sınav seçin!",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sınav başlat
            _examStartTime = DateTime.UtcNow;
            _currentQuestionIndex = 0;
            _userAnswers = new Dictionary<int, string>();
            _questionTimes = new Dictionary<int, int>();

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

            // Alt navigation panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = System.Drawing.Color.WhiteSmoke
            };

            var btnPrev = new Button
            {
                Name = "btnPrev",
                Text = "◀ Önceki",
                Left = 10,
                Top = 10,
                Width = 120,
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
                Text = $"Soru 1 / {_loadedExam.Sorular.Count}",
                Left = 150,
                Top = 20,
                Width = 200,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            var lblTimer = new Label
            {
                Name = "lblTimer",
                Text = "Süre: 0s",
                Left = 370,
                Top = 20,
                Width = 150,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Blue
            };

            var btnNext = new Button
            {
                Name = "btnNext",
                Text = "Sonraki ▶",
                Left = 540,
                Top = 10,
                Width = 120,
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
                Text = "Sınavı Bitir",
                Left = 680,
                Top = 10,
                Width = 130,
                Height = 40,
                BackColor = System.Drawing.Color.LightCoral,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            btnFinish.Click += BtnFinishExam_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnPrev, lblProgress, lblTimer, btnNext, btnFinish });

            pnlExam.Controls.Add(pnlQuestion);
            pnlExam.Controls.Add(pnlBottom);
        }

        private void LoadExamQuestion(int index)
        {
            if (_loadedExam == null || index < 0 || index >= _loadedExam.Sorular.Count)
                return;

            // Önceki sorunun süresini kaydet
            if (_questionStopwatch.IsRunning)
            {
                _questionStopwatch.Stop();
                var previousQuestion = _loadedExam.Sorular[_currentQuestionIndex];
                _questionTimes[previousQuestion.SoruNo] = (int)_questionStopwatch.Elapsed.TotalSeconds;
            }

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
                Width = 780,
                Height = 60,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold)
            };
            pnlQuestion.Controls.Add(lblQuestion);

            // Soru tipine göre input
            int yPos = 100;

            if (question.SoruTipi == "CokSeçmeli")
            {
                // Çoktan seçmeli
                var options = new[] { "A", "B", "C", "D" };
                for (int i = 0; i < question.Siklar?.Count && i < 4; i++)
                {
                    var rb = new RadioButton
                    {
                        Name = $"rb{options[i]}",
                        Text = $"{options[i]}) {question.Siklar[i]}",
                        Left = 40,
                        Top = yPos,
                        Width = 750,
                        Height = 30,
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
                    yPos += 40;
                }
            }
            else if (question.SoruTipi == "DogruYanlis")
            {
                // Doğru-Yanlış
                var rbDogru = new RadioButton
                {
                    Name = "rbDogru",
                    Text = "✓ Doğru",
                    Left = 40,
                    Top = yPos,
                    Width = 150,
                    Height = 30,
                    Font = new System.Drawing.Font("Segoe UI", 10),
                    Tag = "Dogru"
                };
                rbDogru.CheckedChanged += (s, e) =>
                {
                    if (rbDogru.Checked)
                        _userAnswers[question.SoruNo] = "Dogru";
                };

                var rbYanlis = new RadioButton
                {
                    Name = "rbYanlis",
                    Text = "✗ Yanlış",
                    Left = 220,
                    Top = yPos,
                    Width = 150,
                    Height = 30,
                    Font = new System.Drawing.Font("Segoe UI", 10),
                    Tag = "Yanlis"
                };
                rbYanlis.CheckedChanged += (s, e) =>
                {
                    if (rbYanlis.Checked)
                        _userAnswers[question.SoruNo] = "Yanlis";
                };

                if (_userAnswers.TryGetValue(question.SoruNo, out var answer))
                {
                    if (answer == "Dogru") rbDogru.Checked = true;
                    if (answer == "Yanlis") rbYanlis.Checked = true;
                }

                pnlQuestion.Controls.Add(rbDogru);
                pnlQuestion.Controls.Add(rbYanlis);
            }
            else if (question.SoruTipi == "Klasik")
            {
                // Klasik (açık uçlu)
                var txtCevap = new TextBox
                {
                    Name = "txtKlasikCevap",
                    Left = 40,
                    Top = yPos,
                    Width = 750,
                    Height = 120,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new System.Drawing.Font("Segoe UI", 10)
                };
                txtCevap.TextChanged += (s, e) =>
                {
                    _userAnswers[question.SoruNo] = txtCevap.Text;
                };

                if (_userAnswers.TryGetValue(question.SoruNo, out var answer))
                    txtCevap.Text = answer;

                var lblHint = new Label
                {
                    Text = "💡 Anahtar kelimeler: " + string.Join(", ", question.AnahtarKelimeler ?? new List<string>()),
                    Left = 40,
                    Top = yPos + 130,
                    Width = 750,
                    Height = 30,
                    ForeColor = System.Drawing.Color.Gray,
                    Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Italic)
                };

                pnlQuestion.Controls.Add(txtCevap);
                pnlQuestion.Controls.Add(lblHint);
            }

            // Progress güncelle
            var lblProgress = pnlExam.Controls.Find("lblProgress", true).FirstOrDefault() as Label;
            if (lblProgress != null)
                lblProgress.Text = $"Soru {index + 1} / {_loadedExam.Sorular.Count}";

            // Buton durumları
            var btnPrev = pnlExam.Controls.Find("btnPrev", true).FirstOrDefault() as Button;
            var btnNext = pnlExam.Controls.Find("btnNext", true).FirstOrDefault() as Button;
            if (btnPrev != null) btnPrev.Enabled = index > 0;
            if (btnNext != null) btnNext.Enabled = index < _loadedExam.Sorular.Count - 1;

            // Soru süresini başlat
            _questionStopwatch.Restart();

            // Süre göstergesini güncelle
            var lblTimer = pnlExam.Controls.Find("lblTimer", true).FirstOrDefault() as Label;
            if (lblTimer != null)
            {
                var timerUpdate = new System.Windows.Forms.Timer { Interval = 1000 };
                timerUpdate.Tick += (s, e) =>
                {
                    if (_questionStopwatch.IsRunning)
                    {
                        var elapsed = (int)_questionStopwatch.Elapsed.TotalSeconds;
                        lblTimer.Text = $"Süre: {elapsed}s";

                        // Maksimum süre kontrolü
                        if (question.MaxSure.HasValue && elapsed >= question.MaxSure.Value)
                        {
                            lblTimer.ForeColor = System.Drawing.Color.Red;
                            lblTimer.Text = $"Süre: {elapsed}s ⚠️ (Maks: {question.MaxSure.Value}s)";
                        }
                    }
                };
                timerUpdate.Start();
            }
        }

        private async void BtnFinishExam_Click(object sender, EventArgs e)
        {
            if (_loadedExam == null) return;

            // Son sorunun süresini kaydet
            if (_questionStopwatch.IsRunning)
            {
                _questionStopwatch.Stop();
                var lastQuestion = _loadedExam.Sorular[_currentQuestionIndex];
                _questionTimes[lastQuestion.SoruNo] = (int)_questionStopwatch.Elapsed.TotalSeconds;
            }

            var unanswered = _loadedExam.Sorular.Count - _userAnswers.Count;
            if (unanswered > 0)
            {
                var result = MessageBox.Show(this,
                    $"{unanswered} soru cevaplanmadı. Yine de bitirmek istiyor musunuz?",
                    "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;

                // İstatistikleri hesapla
                var stats = new SinavCevapStatistics
                {
                    ToplamSoru = _loadedExam.Sorular.Count,
                    DogruSayisi = 0,
                    YanlisSayisi = 0,
                    BosSayisi = 0,
                    ToplamPuan = _loadedExam.Sorular.Sum(s => s.ToplamPuan),
                    AlinanPuan = 0,
                    CokSeçmeliSayisi = _loadedExam.Sorular.Count(s => s.SoruTipi == "CokSeçmeli"),
                    DogruYanlisSayisi = _loadedExam.Sorular.Count(s => s.SoruTipi == "DogruYanlis"),
                    KlasikSayisi = _loadedExam.Sorular.Count(s => s.SoruTipi == "Klasik")
                };

                // SinavSonucu oluştur
                var sinavSonucu = await _examService.CreateFromStatisticsAsync(
                    _streamingSessionId.Value,
                    _currentAtama?.AtamaID,
                    _loadedExam.SinavTuru,
                    stats,
                    _examStartTime,
                    DateTime.UtcNow,
                    analizeEkle: true
                );

                // Her soru için detaylı cevap kaydet
                foreach (var question in _loadedExam.Sorular)
                {
                    _userAnswers.TryGetValue(question.SoruNo, out var userAnswer);
                    _questionTimes.TryGetValue(question.SoruNo, out var time);

                    var cevap = await _sinavCevapService.CreateAsync(
                        sinavSonucu.SinavSonucuID,
                        question.SoruNo,
                        question.SoruTipi,
                        question.SoruMetni,
                        question.DogruCevap,
                        userAnswer,
                        time > 0 ? time : (int?)null,
                        question.ToplamPuan,
                        question.AnahtarKelimeler
                    );

                    // İstatistikleri güncelle
                    if (cevap.DogruMu)
                        stats.DogruSayisi++;
                    else if (!string.IsNullOrWhiteSpace(userAnswer))
                        stats.YanlisSayisi++;
                    else
                        stats.BosSayisi++;

                    if (cevap.AlinanPuan.HasValue)
                        stats.AlinanPuan += cevap.AlinanPuan.Value;
                }

                // İstatistikleri hesapla ve güncelle
                stats.BasariYuzdesi = stats.ToplamSoru > 0 ? (stats.DogruSayisi * 100.0 / stats.ToplamSoru) : 0;
                stats.OrtalamaCevapSuresi = _questionTimes.Count > 0 ? _questionTimes.Values.Average() : 0;

                // SinavSonucu'yu güncelle
                sinavSonucu.DogruSayisi = stats.DogruSayisi;
                sinavSonucu.YanlisSayisi = stats.YanlisSayisi;
                sinavSonucu.ToplamPuan = stats.ToplamPuan;
                sinavSonucu.AlinanPuan = stats.AlinanPuan;
                sinavSonucu.BasariYuzdesi = stats.BasariYuzdesi;
                sinavSonucu.OrtalamaCevapSuresi = stats.OrtalamaCevapSuresi;
                sinavSonucu.CokSeçmeliSayisi = stats.CokSeçmeliSayisi;
                sinavSonucu.KlasikSoruSayisi = stats.KlasikSayisi;

                // Atamayı tamamlandı işaretle
                if (_currentAtama != null)
                {
                    await _sinavAtamaService.MarkAsCompletedAsync(_currentAtama.AtamaID);
                }

                // EEG kaydını durdur
                if (_streamingSessionId.HasValue)
                {
                    await StopStreamAsync("Sınav tamamlandı");
                }

                this.Cursor = Cursors.Default;

                // Sonuçları göster
                var cevaplar = await _sinavCevapService.GetByExamResultAsync(sinavSonucu.SinavSonucuID);
                ShowExamResults(cevaplar, stats);

                // Sınav panelini gizle
                var pnlExam = tabPageSinav.Controls.Find("pnlExam", true).FirstOrDefault() as Panel;
                if (pnlExam != null)
                    pnlExam.Visible = false;

                // Atamaları yenile
                await LoadAtananSinavlarAsync();

                MessageBox.Show(this,
                    "Sınav tamamlandı ve EEG kaydı durduruldu!\n\n" +
                    $"Başarı: %{stats.BasariYuzdesi:F1}\n" +
                    $"Alınan Puan: {stats.AlinanPuan:F1} / {stats.ToplamPuan}",
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, $"Sınav kaydedilirken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowExamResults(List<SinavCevap> cevaplar, SinavCevapStatistics stats)
        {
            var form = new Form
            {
                Text = "Sınav Sonuçları",
                Size = new System.Drawing.Size(700, 600),
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
            sb.AppendLine("╔" + new string('═', 60) + "╗");
            sb.AppendLine("║" + "           SINAV SONUÇLARI".PadLeft(35).PadRight(60) + "║");
            sb.AppendLine("╚" + new string('═', 60) + "╝");
            sb.AppendLine();
            sb.AppendLine($"Sınav: {_loadedExam.SinavTuru}");
            sb.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────────────");
            sb.AppendLine($"Toplam Soru   : {stats.ToplamSoru}");
            sb.AppendLine($"Doğru         : {stats.DogruSayisi}");
            sb.AppendLine($"Yanlış        : {stats.YanlisSayisi}");
            sb.AppendLine($"Boş           : {stats.BosSayisi}");
            sb.AppendLine();
            sb.AppendLine($"Başarı Oranı  : %{stats.BasariYuzdesi:F1}");
            sb.AppendLine($"Alınan Puan   : {stats.AlinanPuan:F1} / {stats.ToplamPuan}");
            sb.AppendLine($"Ort. Süre     : {stats.OrtalamaCevapSuresi:F0} saniye/soru");
            sb.AppendLine("─────────────────────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine("DETAYLI SONUÇLAR:");
            sb.AppendLine();

            foreach (var cevap in cevaplar.OrderBy(c => c.SoruNo))
            {
                string icon = cevap.DogruMu ? "[✓]" : string.IsNullOrWhiteSpace(cevap.VerilenCevap) ? "[ ]" : "[✗]";
                string status = cevap.DogruMu ? "DOĞRU" : string.IsNullOrWhiteSpace(cevap.VerilenCevap) ? "BOŞ" : "YANLIŞ";

                sb.AppendLine($"{icon} Soru {cevap.SoruNo} ({cevap.SoruTipi}): {status}");

                if (cevap.SoruTipi == "Klasik" && cevap.EslesmeYuzdesi.HasValue)
                {
                    sb.AppendLine($"    Eşleşme: %{cevap.EslesmeYuzdesi:F0} - Puan: {cevap.AlinanPuan:F1}/{cevap.ToplamPuan}");
                }
                else if (!string.IsNullOrWhiteSpace(cevap.VerilenCevap))
                {
                    sb.AppendLine($"    Cevabınız: {cevap.VerilenCevap} | Doğru: {cevap.DogruCevap}");
                }

                if (cevap.CevaplamaSuresi.HasValue)
                {
                    sb.AppendLine($"    Süre: {cevap.CevaplamaSuresi.Value} saniye");
                }

                sb.AppendLine();
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
                Text = "JSON Format Örneği",
                Size = new System.Drawing.Size(700, 600),
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

        /// <summary>
        /// EEG streaming'i durdurur ve oturum kaydını günceller
        /// </summary>
        private async Task StopStreamAsync(string reason = null)
        {
            // Cancel streaming task
            if (_streamCts != null)
            {
                _streamCts.Cancel();
                if (_streamTask != null)
                {
                    try
                    {
                        await _streamTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected cancellation
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw
                        System.Diagnostics.Debug.WriteLine($"Stream task error: {ex.Message}");
                    }
                }
                _streamCts?.Dispose();
                _streamCts = null;
                _streamTask = null;
            }

            // Update session record
            if (_streamingSessionId.HasValue)
            {
                try
                {
                    await _sessionService.UpdateRecordEndAsync(_streamingSessionId.Value, DateTime.UtcNow);
                    await _auditLogService.LogAsync(
                        islem: $"EEG kaydı durduruldu (Oturum #{_streamingSessionId.Value})" + 
                               (reason != null ? $" - {reason}" : ""),
                        kullaniciId: _currentUserId
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Oturum güncellenirken hata: {ex.Message}",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                _streamingSessionId = null;
            }
        }

        #endregion

        #region MODULE PERMISSIONS - Modül Yetkileri Yönetimi

        private DataGridView gridModulYetkisi;
        private Button btnSaveModulYetkisi;

        private void InitializeModulYetkisiTab()
        {
            // Yeni tab olu�tur
            var tabPageModulYetkisi = new TabPage
            {
                Name = "tabPageModulYetkisi",
                Text = "Modul Yetkileri",
                Padding = new Padding(3)
            };
            // Grid olu�tur
            gridModulYetkisi = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            // Kolonlar
            gridModulYetkisi.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "KullaniciID",
                HeaderText = "ID",
                Width = 60,
                ReadOnly = true
            });
            gridModulYetkisi.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AdSoyad",
                HeaderText = "Ad Soyad",
                Width = 200,
                ReadOnly = true
            });
            gridModulYetkisi.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Email",
                HeaderText = "Email",
                Width = 200,
                ReadOnly = true
            });
            gridModulYetkisi.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "SinavModuluErisimi",
                HeaderText = "Sinav Modulu",
                Width = 120,
                ReadOnly = false
            });
            // Alt panel - kaydet butonu
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };
            btnSaveModulYetkisi = new Button
            {
                Text = "Yetkileri Kaydet",
                Width = 150,
                Height = 35,
                Left = 20,
                Top = 12,
                BackColor = System.Drawing.Color.LightGreen,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            btnSaveModulYetkisi.Click += BtnSaveModulYetkisi_Click;
            var lblInfo = new Label
            {
                Text = "Kullanıcılara Sınav Modülü erişimi tanımlayın. Admin/Yönetici rolündekiler zaten erişebilir.",
                Left = 180,
                Top = 15,
                Width = 600,
                Height = 40,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            panelBottom.Controls.Add(btnSaveModulYetkisi);
            panelBottom.Controls.Add(lblInfo);
            tabPageModulYetkisi.Controls.Add(gridModulYetkisi);
            tabPageModulYetkisi.Controls.Add(panelBottom);
            // Tab'� ana kontrol'e ekle
            tabMain.TabPages.Add(tabPageModulYetkisi);
        }
        private async Task RefreshModulYetkisiAsync()
        {
            try
            {
                btnSaveModulYetkisi.Enabled = false;
                Cursor = Cursors.WaitCursor;
                // T�m kullan�c�lar� al
                var users = await _userService.GetAllAsync();
                // S�nav Mod�l� yetkilerini al
                var sinavYetkileri = await _modulYetkisiService.GetModuleAccessForAllUsersAsync("SinavModulu");
                // DataTable olu�tur
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("KullaniciID", typeof(int));
                dataTable.Columns.Add("AdSoyad", typeof(string));
                dataTable.Columns.Add("Email", typeof(string));
                dataTable.Columns.Add("Rol", typeof(string));
                dataTable.Columns.Add("SinavModuluErisimi", typeof(bool));
                foreach (var user in users)
                {
                    bool hasAccess = sinavYetkileri.ContainsKey(user.KullaniciID);
                    dataTable.Rows.Add(
                        user.KullaniciID,
                        user.AdSoyad,
                        user.Email,
                        user.Rol,
                        hasAccess
                    );
                }
                gridModulYetkisi.DataSource = dataTable;
            }
            catch (Exception ex)
            {
                ShowError("Model yetkileri yüklenirken hata oluştu.", ex);
            }
            finally
            {
                btnSaveModulYetkisi.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
        private async void BtnSaveModulYetkisi_Click(object sender, EventArgs e)
        {
            try
            {
                btnSaveModulYetkisi.Enabled = false;
                Cursor = Cursors.WaitCursor;
                var dataTable = gridModulYetkisi.DataSource as System.Data.DataTable;
                if (dataTable == null) return;
                int updatedCount = 0;
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    var kullaniciId = (int)row["KullaniciID"];
                    var hasAccess = (bool)row["SinavModuluErisimi"];
                    await _modulYetkisiService.SetModuleAccessAsync(
                        kullaniciId,
                        "SinavModulu",
                        hasAccess,
                        _currentUserId
                    );
                    updatedCount++;
                }
                // Log
                await _auditLogService.LogAsync(
                    "ModulYetkisiGuncellendi",
                    $"{updatedCount} kullanıcının Sınav Modülü yetkisi güncellendi",
                    _currentUserId,
                    _currentUserName
                );
                MessageBox.Show(this,
                    $"{updatedCount} kullanıcının yetkileri başarıyla kaydedildi.",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Yetkiler kaydedilirken hata olu�tu.", ex);
            }
            finally
            {
                btnSaveModulYetkisi.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region HELPER CLASSES - Yardımcı Data Transfer Objeler

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

        #endregion

        #region UI HELPERS - UI State ve Helper Metodlar

        private void ShowError(string message, Exception ex)
        {
            var errorText = ex?.Message ?? string.Empty;
            MessageBox.Show(this, $"{message}{Environment.NewLine}{errorText}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void panelUsersActions_Paint(object sender, PaintEventArgs e)
        {
        }

        private async void btnManageExperimentTypes_Click(object sender, EventArgs e)
        {
            using (var dialog = new DeneyTuruManageForm())
            {
                dialog.ShowDialog(this);
            }
            await LoadLookupAsync();
        }
        private async void btnManageTimeLabels_Click(object sender, EventArgs e)
        {
            using (var dialog = new ZamanEtiketiManageForm())
            {
                dialog.ShowDialog(this);
            }
            await LoadLookupAsync();
        }

        private void gridAnalyses_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void panelAnalysisActions_Paint(object sender, PaintEventArgs e)
        {

        }

        private void gridUsers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        #endregion

    }
}

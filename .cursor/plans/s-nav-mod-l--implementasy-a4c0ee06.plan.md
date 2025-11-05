<!-- a4c0ee06-bcc7-4be3-836d-8ef89092cd81 a0cd2364-6494-4426-b30d-3055845a4e14 -->
# Sınav Modülü Implementasyon Planı

## 1. Veritabanı Değişiklikleri (KULLANICI YAPACAK)

### Yeni Tablo: SinavSonucu

```sql
CREATE TABLE dbo.SinavSonucu (
  SinavSonucuID INT IDENTITY(1,1) PRIMARY KEY,
  OturumID INT NOT NULL,
  SinavTuru NVARCHAR(100) NULL,
  ToplamSoru INT NOT NULL,
  DogruSayisi INT NOT NULL,
  YanlisSayisi INT NOT NULL,
  BaslamaTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  BitisTarihi DATETIME2 NULL,
  SureDakika INT NULL,
  CONSTRAINT FK_SinavSonucu_Oturum FOREIGN KEY (OturumID)
    REFERENCES dbo.Oturum(OturumID) ON DELETE CASCADE
);
```

**Not:** Kullanıcı bu SQL'i SSMS'de çalıştıracak.

---

## 2. Entity Model Eklemeleri

### Dosya: `eegProject/SinavSonucu.cs` (YENİ)

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eegProject
{
    [Table("SinavSonucu")]
    public partial class SinavSonucu
    {
        [Key]
        public int SinavSonucuID { get; set; }
        
        public int OturumID { get; set; }
        
        [StringLength(100)]
        public string SinavTuru { get; set; }
        
        public int ToplamSoru { get; set; }
        public int DogruSayisi { get; set; }
        public int YanlisSayisi { get; set; }
        
        public DateTime BaslamaTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int? SureDakika { get; set; }
        
        [ForeignKey("OturumID")]
        public virtual Oturum Oturum { get; set; }
    }
}
```

### Dosya: `eegProject/Model1.Context.cs`

`DbSet<SinavSonucu>` eklenecek:

```csharp
public virtual DbSet<SinavSonucu> SinavSonucu { get; set; }
```

---

## 3. Sınav Veri Modelleri

### Dosya: `eegProject/Models/ExamData.cs` (YENİ KLASÖR + DOSYA)

```csharp
using System.Collections.Generic;

namespace eegProject.Models
{
    public class ExamData
    {
        public string SinavTuru { get; set; }
        public string Aciklama { get; set; }
        public List<ExamQuestion> Sorular { get; set; }
    }

    public class ExamQuestion
    {
        public int SoruNo { get; set; }
        public string SoruMetni { get; set; }
        public List<string> Siklar { get; set; } // A, B, C, D şıkları
        public string DogruCevap { get; set; } // "A", "B", "C", "D"
    }

    public class ExamAnswer
    {
        public int SoruNo { get; set; }
        public string VerilenCevap { get; set; }
        public string DogruCevap { get; set; }
        public bool Dogru => VerilenCevap == DogruCevap;
    }
}
```

---

## 4. Sınav Servisleri

### Dosya: `eegProject/Services/ExamService.cs` (YENİ)

```csharp
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class ExamService
    {
        public async Task<SinavSonucu> CreateAsync(SinavSonucu result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            using (var context = DbContextFactory.Create())
            {
                context.SinavSonucu.Add(result);
                await context.SaveChangesAsync();
                return result;
            }
        }

        public async Task<List<SinavSonucu>> GetBySessionAsync(int sessionId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Where(s => s.OturumID == sessionId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task<List<SinavSonucu>> GetByUserAsync(int userId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Include(s => s.Oturum)
                    .Where(s => s.Oturum.KullaniciID == userId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task DeleteAsync(int examResultId)
        {
            using (var context = DbContextFactory.Create())
            {
                var result = await context.SinavSonucu
                    .FirstOrDefaultAsync(s => s.SinavSonucuID == examResultId);
                if (result == null) return;

                context.SinavSonucu.Remove(result);
                await context.SaveChangesAsync();
            }
        }
    }
}
```

### Dosya: `eegProject/Services/ExamLoaderService.cs` (YENİ)

```csharp
using System;
using System.IO;
using Newtonsoft.Json;
using eegProject.Models;

namespace eegProject.Services
{
    internal sealed class ExamLoaderService
    {
        public ExamData LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Sinav dosyasi bulunamadi", filePath);

            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var examData = JsonConvert.DeserializeObject<ExamData>(json);

            // Validasyon
            if (examData == null || examData.Sorular == null || examData.Sorular.Count == 0)
                throw new InvalidOperationException("Sinav dosyasi gecersiz veya bos");

            // Soru numaralarını otomatik ata
            for (int i = 0; i < examData.Sorular.Count; i++)
            {
                if (examData.Sorular[i].SoruNo == 0)
                    examData.Sorular[i].SoruNo = i + 1;
            }

            return examData;
        }

        public string GetSampleJsonFormat()
        {
            var sample = new ExamData
            {
                SinavTuru = "Matematik",
                Aciklama = "Temel matematik sorulari",
                Sorular = new System.Collections.Generic.List<ExamQuestion>
                {
                    new ExamQuestion
                    {
                        SoruNo = 1,
                        SoruMetni = "2 + 2 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "2", "3", "4", "5" },
                        DogruCevap = "C"
                    },
                    new ExamQuestion
                    {
                        SoruNo = 2,
                        SoruMetni = "5 x 3 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "8", "15", "20", "25" },
                        DogruCevap = "B"
                    }
                }
            };

            return JsonConvert.SerializeObject(sample, Formatting.Indented);
        }
    }
}
```

---

## 5. Sınav Formu

### Dosya: `eegProject/Forms/ExamForm.cs` (YENİ)

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Models;
using eegProject.Services;

namespace eegProject.Forms
{
    public partial class ExamForm : Form
    {
        private readonly int _sessionId;
        private readonly ExamData _examData;
        private readonly MindwaveStreamService _streamService;
        private readonly ExamService _examService;
        private readonly EegDataService _eegDataService;
        
        private Dictionary<int, string> _userAnswers = new Dictionary<int, string>();
        private int _currentQuestionIndex = 0;
        private DateTime _examStartTime;
        private CancellationTokenSource _streamCts;
        private Task _streamTask;
        private bool _isRecording = false;

        public ExamForm(int sessionId, ExamData examData)
        {
            _sessionId = sessionId;
            _examData = examData ?? throw new ArgumentNullException(nameof(examData));
            _streamService = new MindwaveStreamService();
            _examService = new ExamService();
            _eegDataService = new EegDataService();
            
            InitializeComponent();
            InitializeExamUI();
        }

        private void InitializeExamUI()
        {
            this.Text = $"Sınav - {_examData.SinavTuru ?? "Genel"}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Sinyal göstergesi (üst)
            var pnlSignal = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightGray };
            var lblSignalStatus = new Label
            {
                Name = "lblSignalStatus",
                Text = "🔴 EEG Sinyal Bekleniyor...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            pnlSignal.Controls.Add(lblSignalStatus);
            this.Controls.Add(pnlSignal);

            // Soru paneli (orta)
            var pnlQuestion = new Panel { Name = "pnlQuestion", Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(pnlQuestion);

            // Alt panel (butonlar)
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };
            
            var btnPrevious = new Button
            {
                Name = "btnPrevious",
                Text = "◀ Önceki",
                Width = 100,
                Height = 40,
                Location = new Point(10, 10)
            };
            btnPrevious.Click += BtnPrevious_Click;

            var lblProgress = new Label
            {
                Name = "lblProgress",
                Text = "Soru 1 / " + _examData.Sorular.Count,
                Width = 150,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(120, 10),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var btnNext = new Button
            {
                Name = "btnNext",
                Text = "Sonraki ▶",
                Width = 100,
                Height = 40,
                Location = new Point(280, 10)
            };
            btnNext.Click += BtnNext_Click;

            var btnFinish = new Button
            {
                Name = "btnFinish",
                Text = "Sınavı Bitir",
                Width = 120,
                Height = 40,
                Location = new Point(pnlBottom.Width - 140, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.LightGreen
            };
            btnFinish.Click += BtnFinish_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnPrevious, lblProgress, btnNext, btnFinish });
            this.Controls.Add(pnlBottom);

            LoadQuestion(_currentQuestionIndex);
        }

        private void LoadQuestion(int index)
        {
            if (index < 0 || index >= _examData.Sorular.Count)
                return;

            _currentQuestionIndex = index;
            var question = _examData.Sorular[index];

            var pnlQuestion = this.Controls.Find("pnlQuestion", true)[0] as Panel;
            pnlQuestion.Controls.Clear();

            // Soru metni
            var lblQuestion = new Label
            {
                Text = $"Soru {question.SoruNo}: {question.SoruMetni}",
                AutoSize = false,
                Width = pnlQuestion.Width - 40,
                Height = 80,
                Location = new Point(0, 20),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            pnlQuestion.Controls.Add(lblQuestion);

            // Şıklar (RadioButton)
            var options = new[] { "A", "B", "C", "D" };
            var yPos = 120;

            for (int i = 0; i < question.Siklar.Count && i < 4; i++)
            {
                var rb = new RadioButton
                {
                    Name = $"rb{options[i]}",
                    Text = $"{options[i]}) {question.Siklar[i]}",
                    AutoSize = false,
                    Width = pnlQuestion.Width - 40,
                    Height = 40,
                    Location = new Point(20, yPos),
                    Font = new Font("Segoe UI", 11),
                    Tag = options[i]
                };
                rb.CheckedChanged += RadioButton_CheckedChanged;

                // Önceden verilen cevabı işaretle
                if (_userAnswers.TryGetValue(question.SoruNo, out var answer) && answer == options[i])
                {
                    rb.Checked = true;
                }

                pnlQuestion.Controls.Add(rb);
                yPos += 50;
            }

            // Progress label güncelle
            var lblProgress = this.Controls.Find("lblProgress", true)[0] as Label;
            lblProgress.Text = $"Soru {index + 1} / {_examData.Sorular.Count}";

            // Buton durumları
            var btnPrevious = this.Controls.Find("btnPrevious", true)[0] as Button;
            var btnNext = this.Controls.Find("btnNext", true)[0] as Button;
            btnPrevious.Enabled = index > 0;
            btnNext.Enabled = index < _examData.Sorular.Count - 1;
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb?.Checked == true)
            {
                var currentQuestion = _examData.Sorular[_currentQuestionIndex];
                _userAnswers[currentQuestion.SoruNo] = rb.Tag.ToString();
            }
        }

        private void BtnPrevious_Click(object sender, EventArgs e)
        {
            if (_currentQuestionIndex > 0)
                LoadQuestion(_currentQuestionIndex - 1);
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_currentQuestionIndex < _examData.Sorular.Count - 1)
                LoadQuestion(_currentQuestionIndex + 1);
        }

        private async void BtnFinish_Click(object sender, EventArgs e)
        {
            // Eksik cevap kontrolü
            var unanswered = _examData.Sorular.Count - _userAnswers.Count;
            if (unanswered > 0)
            {
                var result = MessageBox.Show(
                    this,
                    $"{unanswered} soru cevaplanmadi. Yine de bitirmek istiyor musunuz?",
                    "Uyari",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            await FinishExamAsync();
        }

        private async Task FinishExamAsync()
        {
            try
            {
                // EEG kaydını durdur
                await StopRecordingAsync();

                // Sonuçları hesapla
                var answers = new List<ExamAnswer>();
                var dogruSayisi = 0;
                var yanlisSayisi = 0;

                foreach (var question in _examData.Sorular)
                {
                    var userAnswer = _userAnswers.ContainsKey(question.SoruNo)
                        ? _userAnswers[question.SoruNo]
                        : null;

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

                // Veritabanına kaydet
                var examResult = new SinavSonucu
                {
                    OturumID = _sessionId,
                    SinavTuru = _examData.SinavTuru,
                    ToplamSoru = _examData.Sorular.Count,
                    DogruSayisi = dogruSayisi,
                    YanlisSayisi = yanlisSayisi,
                    BaslamaTarihi = _examStartTime,
                    BitisTarihi = DateTime.UtcNow,
                    SureDakika = (int)(DateTime.UtcNow - _examStartTime).TotalMinutes
                };

                await _examService.CreateAsync(examResult);

                // Detaylı sonuç göster
                ShowDetailedResults(answers, dogruSayisi, yanlisSayisi);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowDetailedResults(List<ExamAnswer> answers, int dogru, int yanlis)
        {
            var form = new Form
            {
                Text = "Sınav Sonuçları",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent
            };

            var txtResults = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("           SINAV SONUÇLARI");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Toplam Soru: {_examData.Sorular.Count}");
            sb.AppendLine($"Doğru: {dogru}");
            sb.AppendLine($"Yanlış: {yanlis}");
            sb.AppendLine($"Boş: {_examData.Sorular.Count - dogru - yanlis}");
            sb.AppendLine($"Başarı Oranı: %{(dogru * 100.0 / _examData.Sorular.Count):F1}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────");
            sb.AppendLine("DETAYLI SONUÇLAR:");
            sb.AppendLine("─────────────────────────────────────");

            foreach (var answer in answers)
            {
                var status = answer.VerilenCevap == null ? "BOS" :
                            answer.Dogru ? "DOGRU" : "YANLIS";
                var icon = answer.VerilenCevap == null ? "⚪" :
                          answer.Dogru ? "✓" : "✗";

                sb.AppendLine($"{icon} Soru {answer.SoruNo}: {status}");
                if (answer.VerilenCevap != null)
                    sb.AppendLine($"  Cevabınız: {answer.VerilenCevap} | Doğru: {answer.DogruCevap}");
            }

            txtResults.Text = sb.ToString();

            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 40,
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(txtResults);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _examStartTime = DateTime.UtcNow;
            await StartRecordingAsync();
        }

        private async Task StartRecordingAsync()
        {
            try
            {
                _streamCts = new CancellationTokenSource();
                _isRecording = true;

                _streamTask = _streamService.StartAsync(
                    async sample => await OnEegSampleReceivedAsync(sample),
                    status => UpdateSignalStatus(status),
                    _streamCts.Token);

                await Task.Delay(100); // Başlama için kısa bekleme
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"EEG baglantisi baslatılamadı: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task StopRecordingAsync()
        {
            if (_streamCts != null && !_streamCts.IsCancellationRequested)
            {
                _streamCts.Cancel();
                if (_streamTask != null)
                {
                    try
                    {
                        await _streamTask;
                    }
                    catch (OperationCanceledException) { }
                }
            }
            _isRecording = false;
        }

        private async Task OnEegSampleReceivedAsync(MindwaveSample sample)
        {
            try
            {
                var eegData = new EEGVerisi
                {
                    OturumID = _sessionId,
                    KullaniciID = 0, // Session'dan alınacak
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

                await _eegDataService.CreateAsync(eegData);
            }
            catch { /* Sessiz devam */ }
        }

        private void UpdateSignalStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateSignalStatus), status);
                return;
            }

            var lblSignal = this.Controls.Find("lblSignalStatus", true).FirstOrDefault() as Label;
            if (lblSignal == null) return;

            if (status.Contains("aktif") || status.Contains("Akis"))
            {
                lblSignal.Text = "🟢 EEG Sinyal Aktif";
                lblSignal.BackColor = Color.LightGreen;
            }
            else if (status.Contains("zayif"))
            {
                lblSignal.Text = "🟡 EEG Sinyal Zayıf";
                lblSignal.BackColor = Color.Yellow;
            }
            else
            {
                lblSignal.Text = "🔴 EEG Sinyal Yok";
                lblSignal.BackColor = Color.LightCoral;
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isRecording)
            {
                var result = MessageBox.Show(
                    this,
                    "Sinav devam ediyor. Kapatmak istediginizden emin misiniz?",
                    "Uyari",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                await StopRecordingAsync();
            }

            base.OnFormClosing(e);
        }
    }
}
```

### Dosya: `eegProject/Forms/ExamForm.Designer.cs` (YENİ)

```csharp
namespace eegProject.Forms
{
    partial class ExamForm
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

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "ExamForm";
        }
    }
}
```

---

## 6. Ana Form'a Tab Ekleme

### Dosya: `eegProject/Form1.Designer.cs`

Designer'da `tabPageSinav` tab'ı eklenecek (veya kod ile):

```csharp
// tabPageSinav
this.tabPageSinav = new System.Windows.Forms.TabPage();
this.tabPageSinav.Location = new System.Drawing.Point(4, 24);
this.tabPageSinav.Name = "tabPageSinav";
this.tabPageSinav.Padding = new System.Windows.Forms.Padding(3);
this.tabPageSinav.Size = new System.Drawing.Size(1042, 622);
this.tabPageSinav.TabIndex = 6;
this.tabPageSinav.Text = "Sınav Modülü";
this.tabPageSinav.UseVisualStyleBackColor = true;

// tabMain.Controls.Add
this.tabMain.Controls.Add(this.tabPageSinav);
```

### Dosya: `eegProject/Form1.cs`

Sınav tab'ı için UI ve event handler'lar:

```csharp
// Form1.cs içine eklenecek

private TabPage tabPageSinav;
private Button btnLoadExam;
private Button btnStartExam;
private ComboBox cmbExamSession;
private Label lblExamInfo;
private TextBox txtExamPreview;
private ExamData _loadedExam;

private void InitializeSinavTab()
{
    // Oturum seçimi
    var lblSession = new Label
    {
        Text = "Oturum Seç:",
        Left = 20,
        Top = 20,
        Width = 100
    };

    cmbExamSession = new ComboBox
    {
        Left = 130,
        Top = 18,
        Width = 300,
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(SessionRow.DisplayName),
        ValueMember = nameof(SessionRow.OturumID)
    };

    // JSON yükleme
    btnLoadExam = new Button
    {
        Text = "Sınav Yükle (JSON)",
        Left = 20,
        Top = 60,
        Width = 150,
        Height = 30
    };
    btnLoadExam.Click += BtnLoadExam_Click;

    // Bilgi label
    lblExamInfo = new Label
    {
        Text = "Henüz sınav yüklenmedi",
        Left = 180,
        Top = 65,
        Width = 400,
        ForeColor = Color.DarkGray
    };

    // Önizleme
    var lblPreview = new Label
    {
        Text = "Sınav Önizleme:",
        Left = 20,
        Top = 110,
        Width = 150
    };

    txtExamPreview = new TextBox
    {
        Left = 20,
        Top = 135,
        Width = 600,
        Height = 300,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        ReadOnly = true,
        Font = new Font("Consolas", 9)
    };

    // Sınava başla butonu
    btnStartExam = new Button
    {
        Text = "Sınava Başla",
        Left = 20,
        Top = 450,
        Width = 150,
        Height = 40,
        Enabled = false,
        BackColor = Color.LightGreen,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };
    btnStartExam.Click += BtnStartExam_Click;

    // JSON format örneği butonu
    var btnShowSample = new Button
    {
        Text = "JSON Format Örneği",
        Left = 180,
        Top = 450,
        Width = 150,
        Height = 40
    };
    btnShowSample.Click += BtnShowSample_Click;

    tabPageSinav.Controls.AddRange(new Control[]
    {
        lblSession, cmbExamSession,
        btnLoadExam, lblExamInfo,
        lblPreview, txtExamPreview,
        btnStartExam, btnShowSample
    });
}

private void BtnLoadExam_Click(object sender, EventArgs e)
{
    using (var openDialog = new OpenFileDialog
    {
        Title = "Sınav JSON Dosyası Seç",
        Filter = "JSON Dosyaları (*.json)|*.json|Tüm Dosyalar (*.*)|*.*",
        Multiline = false
    })
    {
        if (openDialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var loader = new ExamLoaderService();
            _loadedExam = loader.LoadFromJson(openDialog.FileName);

            lblExamInfo.Text = $"✓ Yüklendi: {_loadedExam.SinavTuru} ({_loadedExam.Sorular.Count} soru)";
            lblExamInfo.ForeColor = Color.DarkGreen;

            // Önizleme
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Sınav Türü: {_loadedExam.SinavTuru}");
            sb.AppendLine($"Açıklama: {_loadedExam.Aciklama}");
            sb.AppendLine($"Toplam Soru: {_loadedExam.Sorular.Count}");
            sb.AppendLine();
            sb.AppendLine("Sorular:");
            foreach (var q in _loadedExam.Sorular.Take(3))
            {
                sb.AppendLine($"  {q.SoruNo}. {q.SoruMetni}");
                foreach (var option in q.Siklar.Select((s, i) => $"    {(char)('A' + i)}) {s}"))
                    sb.AppendLine(option);
                sb.AppendLine($"    Doğru: {q.DogruCevap}");
                sb.AppendLine();
            }
            if (_loadedExam.Sorular.Count > 3)
                sb.AppendLine($"  ... ve {_loadedExam.Sorular.Count - 3} soru daha");

            txtExamPreview.Text = sb.ToString();
            btnStartExam.Enabled = cmbExamSession.SelectedItem != null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Sınav yüklenemedi: {ex.Message}", "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblExamInfo.Text = "✗ Yükleme başarısız";
            lblExamInfo.ForeColor = Color.Red;
        }
    }
}

private void BtnStartExam_Click(object sender, EventArgs e)
{
    if (_loadedExam == null)
    {
        MessageBox.Show(this, "Önce bir sınav yükleyin", "Uyarı",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var selectedSession = cmbExamSession.SelectedItem as SessionRow;
    if (selectedSession == null)
    {
        MessageBox.Show(this, "Lütfen bir oturum seçin", "Uyarı",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    // Sınav formunu aç
    using (var examForm = new ExamForm(selectedSession.OturumID, _loadedExam))
    {
        if (examForm.ShowDialog(this) == DialogResult.OK)
        {
            MessageBox.Show(this, "Sınav tamamlandı ve kaydedildi!", "Bilgi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

private void BtnShowSample_Click(object sender, EventArgs e)
{
    var loader = new ExamLoaderService();
    var sampleJson = loader.GetSampleJsonFormat();

    var form = new Form
    {
        Text = "JSON Format Örneği",
        Size = new Size(600, 500),
        StartPosition = FormStartPosition.CenterParent
    };

    var txt = new TextBox
    {
        Text = sampleJson,
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9),
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
            Filter = "JSON Dosyası (*.json)|*.json",
            FileName = "ornek_sinav.json"
        })
        {
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(saveDialog.FileName, sampleJson);
                MessageBox.Show("Kaydedildi!");
            }
        }
    };

    form.Controls.Add(txt);
    form.Controls.Add(btnSave);
    form.ShowDialog(this);
}

// Form1_Load içinde çağrılacak
private async void Form1_Load(object sender, EventArgs e)
{
    // ... mevcut kodlar ...
    
    InitializeSinavTab();
    
    // Sınav oturum combobox'ını doldur
    cmbExamSession.SelectedIndexChanged += (s, args) =>
    {
        btnStartExam.Enabled = _loadedExam != null && cmbExamSession.SelectedItem != null;
    };
}

// Oturumlar yenilendiğinde sınav combobox'ını da güncelle
private async Task RefreshSessionsAsync()
{
    // ... mevcut kod ...
    
    // Sınav tab için de güncelle
    if (cmbExamSession != null)
    {
        cmbExamSession.DataSource = _sessions.ToList();
    }
}
```

---

## 7. Proje Dosyasına Ekleme

### Dosya: `eegProject/eegProject.csproj`

Yeni dosyaları projeye eklemek için:

```xml
<Compile Include="SinavSonucu.cs" />
<Compile Include="Models\ExamData.cs" />
<Compile Include="Services\ExamService.cs" />
<Compile Include="Services\ExamLoaderService.cs" />
<Compile Include="Forms\ExamForm.cs">
  <SubType>Form</SubType>
</Compile>
<Compile Include="Forms\ExamForm.Designer.cs">
  <DependentUpon>ExamForm.cs</DependentUpon>
</Compile>
```

---

## 8. Test Senaryosu

1. Veritabanında `SinavSonucu` tablosu oluşturulmalı
2. Örnek JSON dosyası oluşturulmalı (btnShowSample ile)
3. Oturum seçilmeli
4. JSON yüklenmeli
5. Sınava başlanmalı
6. EEG sinyali kontrol edilmeli
7. Sorular cevaplanmalı
8. Sınav bitirilmeli
9. Sonuçlar görüntülenmeli

---

## Dosya Yapısı Özeti

```
eegProject/
├── SinavSonucu.cs (YENİ - Entity)
├── Models/ (YENİ KLASÖR)
│   └── ExamData.cs (YENİ - Exam modelleri)
├── Services/
│   ├── ExamService.cs (YENİ)
│   └── ExamLoaderService.cs (YENİ)
├── Forms/
│   ├── ExamForm.cs (YENİ)
│   └── ExamForm.Designer.cs (YENİ)
└── Form1.cs (GÜNCELLENECEK - Sınav tab)
```

---

## Notlar

- EEG kaydı sınav sırasında arka planda çalışır
- Kullanıcı sorular arasında gidip gelebilir
- Sinyal durumu gerçek zamanlı gösterilir
- Detaylı sonuç özeti sağlanır
- JSON formatı esnek ve genişletilebilir

### To-dos

- [ ] SinavSonucu tablosunu veritabanında oluştur (SQL script çalıştır)
- [ ] SinavSonucu.cs entity dosyasını oluştur ve DbContext'e ekle
- [ ] Models/ExamData.cs dosyasını oluştur (ExamData, ExamQuestion, ExamAnswer)
- [ ] Services/ExamService.cs servisini oluştur (CRUD operations)
- [ ] Services/ExamLoaderService.cs servisini oluştur (JSON yükleme)
- [ ] Forms/ExamForm.cs ve Designer.cs dosyalarını oluştur (sınav UI)
- [ ] Form1.Designer.cs'e tabPageSinav tab'ını ekle
- [ ] Form1.cs'e sınav tab için UI ve event handler'ları ekle
- [ ] eegProject.csproj dosyasına yeni dosyaları ekle
- [ ] Örnek sınav JSON dosyası oluştur ve test et
- [ ] Sınav modülünü end-to-end test et (EEG + sınav + sonuçlar)
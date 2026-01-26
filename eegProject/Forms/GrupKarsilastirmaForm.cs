using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Services;
using Newtonsoft.Json;

namespace eegProject.Forms
{
    /// <summary>
    /// Grup Karşılaştırma Formu
    /// İki deney grubunun EEG metriklerini karşılaştırır
    /// </summary>
    internal sealed class GrupKarsilastirmaForm : Form
    {
        private readonly DeneyGrubuService _deneyGrubuService = new DeneyGrubuService();
        private readonly AnalysisComputationService _analysisService;
        private readonly EegDataService _eegDataService = new EegDataService();
        private readonly string _aiProvider;

        private ComboBox _cmbGrup1;
        private ComboBox _cmbGrup2;
        private RadioButton _rbRawComparison;
        private RadioButton _rbNormalizedComparison;
        private Button _btnCompare;
        private TextBox _txtResults;
        private Label _lblStatus;
        private ProgressBar _progressBar;

        private List<DeneyGrubu> _gruplar = new List<DeneyGrubu>();

        public GrupKarsilastirmaForm(AnalysisComputationService analysisService, string aiProvider = "openai")
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _aiProvider = aiProvider ?? "openai";
            InitializeUI();
            Load += async (s, e) => await LoadGruplarAsync();
        }

        private void InitializeUI()
        {
            string providerDisplay = _aiProvider == "gemini" ? "Google Gemini" : "OpenAI ChatGPT";
            Text = $"Grup Karşılaştırma (AI: {providerDisplay})";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 600);
            MinimumSize = new Size(600, 500);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Grup seçimi
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Karşılaştırma tipi
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Sonuçlar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Durum

            // --- Grup Seçimi Panel ---
            var grupPanel = new GroupBox
            {
                Text = "Deney Gruplarını Seçin",
                Dock = DockStyle.Fill
            };
            var grupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
            };
            grupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            grupLayout.Controls.Add(new Label { Text = "Grup 1:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            grupLayout.Controls.Add(new Label { Text = "Grup 2:", TextAlign = ContentAlignment.MiddleLeft }, 1, 0);

            _cmbGrup1 = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbGrup2 = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            grupLayout.Controls.Add(_cmbGrup1, 0, 1);
            grupLayout.Controls.Add(_cmbGrup2, 1, 1);
            grupPanel.Controls.Add(grupLayout);

            // --- Karşılaştırma Tipi Panel ---
            var tipPanel = new GroupBox
            {
                Text = "Karşılaştırma Yöntemi",
                Dock = DockStyle.Fill
            };
            var tipLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(8)
            };

            _rbRawComparison = new RadioButton
            {
                Text = "Ham Karşılaştırma (Tüm oturumların ortalaması)",
                Checked = true,
                AutoSize = true
            };
            _rbNormalizedComparison = new RadioButton
            {
                Text = "Bazal-Normalize Karşılaştırma (Görev oturumları bazale göre normalize)",
                AutoSize = true
            };

            _btnCompare = new Button
            {
                Text = "Karşılaştır",
                Size = new Size(120, 30),
                Margin = new Padding(0, 8, 0, 0)
            };
            _btnCompare.Click += async (s, e) => await RunComparisonAsync();

            tipLayout.Controls.Add(_rbRawComparison);
            tipLayout.Controls.Add(_rbNormalizedComparison);
            tipLayout.Controls.Add(_btnCompare);
            tipPanel.Controls.Add(tipLayout);

            // --- Sonuçlar Panel ---
            var sonucPanel = new GroupBox
            {
                Text = "Sonuçlar",
                Dock = DockStyle.Fill
            };
            _txtResults = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                BackColor = Color.White
            };
            sonucPanel.Controls.Add(_txtResults);

            // --- Durum Panel ---
            var durumPanel = new Panel { Dock = DockStyle.Fill };
            _lblStatus = new Label
            {
                Text = "Hazır",
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Right,
                Width = 200,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            durumPanel.Controls.Add(_lblStatus);
            durumPanel.Controls.Add(_progressBar);

            mainLayout.Controls.Add(grupPanel, 0, 0);
            mainLayout.Controls.Add(tipPanel, 0, 1);
            mainLayout.Controls.Add(sonucPanel, 0, 2);
            mainLayout.Controls.Add(durumPanel, 0, 3);

            Controls.Add(mainLayout);
        }

        private async Task LoadGruplarAsync()
        {
            try
            {
                _gruplar = await _deneyGrubuService.GetActiveAsync();

                _cmbGrup1.DataSource = null;
                _cmbGrup2.DataSource = null;

                var list1 = _gruplar.Select(g => new { Id = g.DeneyGrubuID, Name = g.GrupAdi }).ToList();
                var list2 = _gruplar.Select(g => new { Id = g.DeneyGrubuID, Name = g.GrupAdi }).ToList();

                _cmbGrup1.DataSource = list1;
                _cmbGrup1.DisplayMember = "Name";
                _cmbGrup1.ValueMember = "Id";

                _cmbGrup2.DataSource = list2;
                _cmbGrup2.DisplayMember = "Name";
                _cmbGrup2.ValueMember = "Id";

                if (list2.Count > 1) _cmbGrup2.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Gruplar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RunComparisonAsync()
        {
            if (_cmbGrup1.SelectedValue == null || _cmbGrup2.SelectedValue == null)
            {
                MessageBox.Show(this, "Lütfen her iki grubu da seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var grup1Id = (int)_cmbGrup1.SelectedValue;
            var grup2Id = (int)_cmbGrup2.SelectedValue;

            if (grup1Id == grup2Id)
            {
                MessageBox.Show(this, "Lütfen farklı gruplar seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true, "Oturumlar yükleniyor...");

            try
            {
                var grup1Name = _gruplar.FirstOrDefault(g => g.DeneyGrubuID == grup1Id)?.GrupAdi ?? "Grup 1";
                var grup2Name = _gruplar.FirstOrDefault(g => g.DeneyGrubuID == grup2Id)?.GrupAdi ?? "Grup 2";

                // Grup oturumlarını al
                var grup1Sessions = await _deneyGrubuService.GetSessionsByGroupAsync(grup1Id);
                var grup2Sessions = await _deneyGrubuService.GetSessionsByGroupAsync(grup2Id);

                if (grup1Sessions.Count == 0 || grup2Sessions.Count == 0)
                {
                    _txtResults.Text = "Bir veya her iki grupta oturum bulunamadı.";
                    return;
                }

                SetBusy(true, "Analiz yapılıyor...");

                if (_rbNormalizedComparison.Checked)
                {
                    await RunNormalizedComparisonAsync(grup1Sessions, grup2Sessions, grup1Name, grup2Name);
                }
                else
                {
                    await RunRawComparisonAsync(grup1Sessions, grup2Sessions, grup1Name, grup2Name);
                }
            }
            catch (Exception ex)
            {
                _txtResults.Text = $"Hata: {ex.Message}\n\n{ex.StackTrace}";
            }
            finally
            {
                SetBusy(false, "Tamamlandı");
            }
        }

        private async Task RunRawComparisonAsync(List<Oturum> grup1Sessions, List<Oturum> grup2Sessions, string grup1Name, string grup2Name)
        {
            var grup1Ids = grup1Sessions.Select(s => s.OturumID).ToList();
            var grup2Ids = grup2Sessions.Select(s => s.OturumID).ToList();

            var result = await _analysisService.ComputeNeuroISGroupComparisonAsync(grup1Ids, grup2Ids, grup1Name, grup2Name);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("                  HAM KARŞILAŞTIRMA SONUÇLARI                   ");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine(result.Summary);
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("DETAYLI METRİKLER (JSON):");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine(result.MetricsJSON);

            _txtResults.Text = sb.ToString();
        }

        private async Task RunNormalizedComparisonAsync(List<Oturum> grup1Sessions, List<Oturum> grup2Sessions, string grup1Name, string grup2Name)
        {
            // Sadece Görev oturumlarını filtrele
            var grup1GorevSessions = grup1Sessions.Where(s => s.OturumTipi == "Gorev").ToList();
            var grup2GorevSessions = grup2Sessions.Where(s => s.OturumTipi == "Gorev").ToList();

            // Bazal oturumları bul (kullanıcı bazında)
            var grup1BazalByUser = grup1Sessions
                .Where(s => s.OturumTipi == "Bazal")
                .GroupBy(s => s.KullaniciID)
                .ToDictionary(g => g.Key, g => g.First().OturumID);

            var grup2BazalByUser = grup2Sessions
                .Where(s => s.OturumTipi == "Bazal")
                .GroupBy(s => s.KullaniciID)
                .ToDictionary(g => g.Key, g => g.First().OturumID);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("              BAZAL-NORMALİZE KARŞILAŞTIRMA SONUÇLARI           ");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Her grup için normalize değerleri hesapla
            var grup1NormValues = new List<NormalizedMetrics>();
            var grup2NormValues = new List<NormalizedMetrics>();

            sb.AppendLine($"── {grup1Name} ──");
            foreach (var gorevSession in grup1GorevSessions)
            {
                if (grup1BazalByUser.TryGetValue(gorevSession.KullaniciID, out var bazalId))
                {
                    try
                    {
                        var normalized = await _analysisService.ComputeNormalizedRatiosAsync(gorevSession.OturumID, bazalId);
                        var metrics = JsonConvert.DeserializeObject<dynamic>(normalized.MetricsJSON);
                        grup1NormValues.Add(new NormalizedMetrics
                        {
                            ThetaAlpha = (double)metrics.NormalizeDegerler.ThetaAlpha,
                            ThetaBeta = (double)metrics.NormalizeDegerler.ThetaBeta,
                            BetaAlpha = (double)metrics.NormalizeDegerler.BetaAlpha
                        });
                        sb.AppendLine($"  Oturum {gorevSession.OturumID}: θ/α={metrics.NormalizeDegerler.ThetaAlphaYuzde}%, θ/β={metrics.NormalizeDegerler.ThetaBetaYuzde}%, β/α={metrics.NormalizeDegerler.BetaAlphaYuzde}%");
                    }
                    catch { /* Skip if not enough data */ }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"── {grup2Name} ──");
            foreach (var gorevSession in grup2GorevSessions)
            {
                if (grup2BazalByUser.TryGetValue(gorevSession.KullaniciID, out var bazalId))
                {
                    try
                    {
                        var normalized = await _analysisService.ComputeNormalizedRatiosAsync(gorevSession.OturumID, bazalId);
                        var metrics = JsonConvert.DeserializeObject<dynamic>(normalized.MetricsJSON);
                        grup2NormValues.Add(new NormalizedMetrics
                        {
                            ThetaAlpha = (double)metrics.NormalizeDegerler.ThetaAlpha,
                            ThetaBeta = (double)metrics.NormalizeDegerler.ThetaBeta,
                            BetaAlpha = (double)metrics.NormalizeDegerler.BetaAlpha
                        });
                        sb.AppendLine($"  Oturum {gorevSession.OturumID}: θ/α={metrics.NormalizeDegerler.ThetaAlphaYuzde}%, θ/β={metrics.NormalizeDegerler.ThetaBetaYuzde}%, β/α={metrics.NormalizeDegerler.BetaAlphaYuzde}%");
                    }
                    catch { /* Skip if not enough data */ }
                }
            }

            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("ÖZET KARŞILAŞTIRMA:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");

            if (grup1NormValues.Count > 0 && grup2NormValues.Count > 0)
            {
                var g1AvgTA = grup1NormValues.Average(v => v.ThetaAlpha) * 100;
                var g1AvgTB = grup1NormValues.Average(v => v.ThetaBeta) * 100;
                var g1AvgBA = grup1NormValues.Average(v => v.BetaAlpha) * 100;

                var g2AvgTA = grup2NormValues.Average(v => v.ThetaAlpha) * 100;
                var g2AvgTB = grup2NormValues.Average(v => v.ThetaBeta) * 100;
                var g2AvgBA = grup2NormValues.Average(v => v.BetaAlpha) * 100;

                sb.AppendLine();
                sb.AppendLine($"                        {grup1Name,-20} {grup2Name,-20} Fark");
                sb.AppendLine($"  θ/α (Bilişsel Yük)    {g1AvgTA:+0.0;-0.0}%{"",-15} {g2AvgTA:+0.0;-0.0}%{"",-15} {g1AvgTA - g2AvgTA:+0.0;-0.0}%");
                sb.AppendLine($"  θ/β (Konsantrasyon)   {g1AvgTB:+0.0;-0.0}%{"",-15} {g2AvgTB:+0.0;-0.0}%{"",-15} {g1AvgTB - g2AvgTB:+0.0;-0.0}%");
                sb.AppendLine($"  β/α (Uyanıklık)       {g1AvgBA:+0.0;-0.0}%{"",-15} {g2AvgBA:+0.0;-0.0}%{"",-15} {g1AvgBA - g2AvgBA:+0.0;-0.0}%");
                sb.AppendLine();
                sb.AppendLine($"  Oturum Sayısı         {grup1NormValues.Count,-20} {grup2NormValues.Count,-20}");

                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("YORUM:");
                sb.AppendLine("───────────────────────────────────────────────────────────────");

                if (Math.Abs(g1AvgTA - g2AvgTA) > 10)
                {
                    var higherGroup = g1AvgTA > g2AvgTA ? grup1Name : grup2Name;
                    sb.AppendLine($"  • {higherGroup} grubunda bazale göre daha yüksek bilişsel yük gözlemlendi.");
                }
                if (Math.Abs(g1AvgTB - g2AvgTB) > 10)
                {
                    var lowerGroup = g1AvgTB < g2AvgTB ? grup1Name : grup2Name;
                    sb.AppendLine($"  • {lowerGroup} grubunda bazale göre daha iyi konsantrasyon gözlemlendi.");
                }
                if (Math.Abs(g1AvgBA - g2AvgBA) > 10)
                {
                    var higherGroup = g1AvgBA > g2AvgBA ? grup1Name : grup2Name;
                    sb.AppendLine($"  • {higherGroup} grubunda bazale göre daha yüksek uyanıklık gözlemlendi.");
                }
            }
            else
            {
                sb.AppendLine("  Yetersiz veri: Bazal-normalize karşılaştırma için her grupta");
                sb.AppendLine("  en az bir kullanıcının hem 'Bazal' hem de 'Gorev' oturumu olmalı.");
                sb.AppendLine();
                sb.AppendLine("  İpucu: Oturum düzenlerken 'Oturum Tipi' alanını doldurun.");
            }

            _txtResults.Text = sb.ToString();
        }

        private void SetBusy(bool busy, string status)
        {
            _btnCompare.Enabled = !busy;
            _cmbGrup1.Enabled = !busy;
            _cmbGrup2.Enabled = !busy;
            _progressBar.Visible = busy;
            _lblStatus.Text = status;
        }

        private class NormalizedMetrics
        {
            public double ThetaAlpha { get; set; }
            public double ThetaBeta { get; set; }
            public double BetaAlpha { get; set; }
        }
    }
}

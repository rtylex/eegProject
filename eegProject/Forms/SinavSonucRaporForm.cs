using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    /// <summary>
    /// Yönetici için sınav sonuçlarını görüntüleme formu (Kullanıcı → Oturum → Sınav hiyerarşisi)
    /// </summary>
    public partial class SinavSonucRaporForm : Form
    {
        private readonly UserService _userService = new UserService();
        private readonly ExamService _examService = new ExamService();
        private readonly SinavCevapService _sinavCevapService = new SinavCevapService();
        private readonly int _currentUserId;

        private ListBox lstUsers;
        private TreeView treeOturumlar;
        private Panel pnlOzet;
        private Label lblSinavAdi;
        private Label lblTarih;
        private Label lblBasari;
        private Label lblSure;
        private Label lblToplamSoru;
        private Label lblDogru;
        private Label lblYanlis;
        private Label lblBos;
        private Button btnDetayliRapor;
        private Button btnSoruAnalizi;

        private SinavSonucu _selectedExam;

        public SinavSonucRaporForm(int currentUserId)
        {
            _currentUserId = currentUserId;
            InitializeComponent();
            InitializeCustomComponents();
            _ = LoadUsersAsync();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SinavSonucRaporForm
            // 
            this.ClientSize = new System.Drawing.Size(1182, 653);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "SinavSonucRaporForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sınav Sonuçları Yönetimi";
            this.Load += new System.EventHandler(this.SinavSonucRaporForm_Load);
            this.ResumeLayout(false);

        }

        private void InitializeCustomComponents()
        {
            // Ana layout
            var splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 250,
                FixedPanel = FixedPanel.Panel1
            };

            // Sol panel - Kullanıcılar
            var pnlUsers = new Panel { Dock = DockStyle.Fill };
            var lblUsers = new Label
            {
                Text = "👥 KULLANICILAR",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            lstUsers = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                DisplayMember = "AdSoyad",
                IntegralHeight = false
            };
            lstUsers.SelectedIndexChanged += LstUsers_SelectedIndexChanged;
            pnlUsers.Controls.Add(lstUsers);
            pnlUsers.Controls.Add(lblUsers);

            splitMain.Panel1.Controls.Add(pnlUsers);

            // Sağ taraf - İkinci split (Oturumlar & Özet)
            var splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                Orientation = Orientation.Horizontal
            };

            // Üst - Oturumlar & Sınavlar (TreeView)
            var pnlOturumlar = new Panel { Dock = DockStyle.Fill };
            var lblOturumlar = new Label
            {
                Text = "📁 OTURUMLAR & SINAVLAR",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            treeOturumlar = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                FullRowSelect = true
            };
            treeOturumlar.AfterSelect += TreeOturumlar_AfterSelect;
            pnlOturumlar.Controls.Add(treeOturumlar);
            pnlOturumlar.Controls.Add(lblOturumlar);

            splitRight.Panel1.Controls.Add(pnlOturumlar);

            // Alt - Özet Bilgiler
            pnlOzet = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };
            var lblOzetBaslik = new Label
            {
                Text = "📊 SINAV ÖZET",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 0, 0, 0)
            };

            var pnlOzetIcerik = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            lblSinavAdi = CreateLabel("Sınav: -", 20);
            lblTarih = CreateLabel("Tarih: -", 50);
            lblBasari = CreateLabel("Başarı: -", 80);
            lblSure = CreateLabel("Süre: -", 110);
            lblToplamSoru = CreateLabel("Toplam Soru: -", 140);
            lblDogru = CreateLabel("✔️ Doğru: -", 170);
            lblYanlis = CreateLabel("✖️ Yanlış: -", 200);
            lblBos = CreateLabel("⭕ Boş: -", 230);

            btnDetayliRapor = new Button
            {
                Text = "📄 Detaylı Rapor Göster",
                Left = 20,
                Top = 280,
                Width = 200,
                Height = 40,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnDetayliRapor.Click += BtnDetayliRapor_Click;

            btnSoruAnalizi = new Button
            {
                Text = "🔍 Soru Bazlı Analiz",
                Left = 240,
                Top = 280,
                Width = 200,
                Height = 40,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnSoruAnalizi.Click += BtnSoruAnalizi_Click;

            pnlOzetIcerik.Controls.AddRange(new Control[]
            {
                lblSinavAdi, lblTarih, lblBasari, lblSure,
                lblToplamSoru, lblDogru, lblYanlis, lblBos,
                btnDetayliRapor, btnSoruAnalizi
            });

            pnlOzet.Controls.Add(pnlOzetIcerik);
            pnlOzet.Controls.Add(lblOzetBaslik);

            splitRight.Panel2.Controls.Add(pnlOzet);
            splitMain.Panel2.Controls.Add(splitRight);

            this.Controls.Add(splitMain);
        }

        private Label CreateLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Left = 20,
                Top = top,
                Width = 600,
                Height = 25,
                Font = new Font("Segoe UI", 10)
            };
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                lstUsers.Items.Clear();

                foreach (var user in users.OrderBy(u => u.AdSoyad))
                {
                    lstUsers.Items.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kullanıcılar yüklenirken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedUser = lstUsers.SelectedItem as Kullanici;
            if (selectedUser == null)
            {
                treeOturumlar.Nodes.Clear();
                ClearOzetPanel();
                return;
            }

            await LoadUserSessionsWithExamsAsync(selectedUser.KullaniciID);
        }

        private async Task LoadUserSessionsWithExamsAsync(int userId)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                treeOturumlar.Nodes.Clear();
                ClearOzetPanel();

                // Kullanıcının tüm sınav sonuçlarını oturum bazlı çek
                var examResults = await _examService.GetByUserWithSessionsAsync(userId);

                if (examResults.Count == 0)
                {
                    var emptyNode = new TreeNode("Bu kullanıcının sınavı bulunmuyor");
                    emptyNode.ForeColor = Color.Gray;
                    treeOturumlar.Nodes.Add(emptyNode);
                    return;
                }

                // Oturum bazlı grupla
                var groupedBySession = examResults.GroupBy(e => e.OturumID);

                foreach (var sessionGroup in groupedBySession.OrderByDescending(g => g.First().BaslamaTarihi))
                {
                    var firstExam = sessionGroup.First();
                    var oturum = firstExam.Oturum;

                    var sessionNodeText = $"📁 Oturum #{oturum.OturumID} - " +
                                         $"{oturum.DeneyTuru ?? "Genel"}" +
                                         $"{(string.IsNullOrEmpty(oturum.ZamanEtiketi) ? "" : " - " + oturum.ZamanEtiketi)}" +
                                         $" ({oturum.KayitBaslangic?.ToString("dd.MM.yyyy HH:mm") ?? "Tarih yok"})";

                    var sessionNode = new TreeNode(sessionNodeText);
                    sessionNode.Tag = oturum;
                    sessionNode.NodeFont = new Font("Segoe UI", 10, FontStyle.Bold);

                    // Bu oturumdaki tüm sınavlar
                    foreach (var exam in sessionGroup.OrderBy(e => e.BaslamaTarihi))
                    {
                        var basariYuzdesi = exam.BasariYuzdesi ?? 0;
                        var icon = basariYuzdesi >= 70 ? "✅" : basariYuzdesi >= 50 ? "⚠️" : "❌";

                        var examNodeText = $"📊 {exam.SinavTuru} {icon} - " +
                                          $"%{basariYuzdesi:F0} " +
                                          $"({exam.DogruSayisi}/{exam.ToplamSoru}) - " +
                                          $"{exam.BaslamaTarihi:HH:mm}";

                        var examNode = new TreeNode(examNodeText);
                        examNode.Tag = exam;
                        examNode.ForeColor = basariYuzdesi >= 70 ? Color.Green :
                                            basariYuzdesi >= 50 ? Color.Orange : Color.Red;

                        sessionNode.Nodes.Add(examNode);
                    }

                    treeOturumlar.Nodes.Add(sessionNode);
                }

                // İlk oturumu genişlet
                if (treeOturumlar.Nodes.Count > 0)
                {
                    treeOturumlar.Nodes[0].Expand();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Oturumlar yüklenirken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void TreeOturumlar_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var selectedExam = e.Node.Tag as SinavSonucu;
            if (selectedExam == null)
            {
                ClearOzetPanel();
                return;
            }

            _selectedExam = selectedExam;
            ShowExamSummary(selectedExam);
        }

        private void ShowExamSummary(SinavSonucu exam)
        {
            lblSinavAdi.Text = $"Sınav: {exam.SinavTuru}";
            lblTarih.Text = $"Tarih: {exam.BaslamaTarihi:dd.MM.yyyy HH:mm}";

            var basariYuzdesi = exam.BasariYuzdesi ?? 0;
            var basariIcon = basariYuzdesi >= 70 ? "✅" : basariYuzdesi >= 50 ? "⚠️" : "❌";
            lblBasari.Text = $"Başarı: {basariIcon} %{basariYuzdesi:F1}";
            lblBasari.ForeColor = basariYuzdesi >= 70 ? Color.Green :
                                 basariYuzdesi >= 50 ? Color.Orange : Color.Red;

            var sure = exam.Sure ?? "-";
            var bitisTarihi = exam.BitisTarihi;
            if (bitisTarihi.HasValue)
            {
                var duration = (bitisTarihi.Value - exam.BaslamaTarihi).TotalMinutes;
                sure = $"{duration:F1} dakika";
            }
            lblSure.Text = $"Süre: {sure}";

            lblToplamSoru.Text = $"Toplam Soru: {exam.ToplamSoru}";
            lblDogru.Text = $"✔️ Doğru: {exam.DogruSayisi}";
            lblYanlis.Text = $"✖️ Yanlış: {exam.YanlisSayisi}";
            var bos = exam.ToplamSoru - exam.DogruSayisi - exam.YanlisSayisi;
            lblBos.Text = $"⭕ Boş: {bos}";

            btnDetayliRapor.Enabled = true;
            btnSoruAnalizi.Enabled = true;
        }

        private void ClearOzetPanel()
        {
            lblSinavAdi.Text = "Sınav: -";
            lblTarih.Text = "Tarih: -";
            lblBasari.Text = "Başarı: -";
            lblBasari.ForeColor = Color.Black;
            lblSure.Text = "Süre: -";
            lblToplamSoru.Text = "Toplam Soru: -";
            lblDogru.Text = "✔️ Doğru: -";
            lblYanlis.Text = "✖️ Yanlış: -";
            lblBos.Text = "⭕ Boş: -";

            btnDetayliRapor.Enabled = false;
            btnSoruAnalizi.Enabled = false;
            _selectedExam = null;
        }

        private async void BtnDetayliRapor_Click(object sender, EventArgs e)
        {
            if (_selectedExam == null) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Tüm soru cevaplarını çek
                var cevaplar = await _sinavCevapService.GetByExamResultAsync(_selectedExam.SinavSonucuID);

                // Detaylı rapor göster
                ShowDetailedReport(_selectedExam, cevaplar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Rapor oluşturulurken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ShowDetailedReport(SinavSonucu exam, List<SinavCevap> cevaplar)
        {
            var form = new Form
            {
                Text = $"Detaylı Sınav Raporu - {exam.SinavTuru}",
                Size = new Size(800, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            var txt = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.White
            };

            var sb = new StringBuilder();
            sb.AppendLine("╔" + new string('═', 70) + "╗");
            sb.AppendLine("║" + "           DETAYLI SINAV RAPORU".PadLeft(45).PadRight(70) + "║");
            sb.AppendLine("╚" + new string('═', 70) + "╝");
            sb.AppendLine();
            sb.AppendLine($"Sınav: {exam.SinavTuru}");
            sb.AppendLine($"Tarih: {exam.BaslamaTarihi:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Oturum: #{exam.OturumID} - {exam.Oturum?.DeneyTuru ?? "Genel"}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────────────────────────");
            sb.AppendLine($"Toplam Soru   : {exam.ToplamSoru}");
            sb.AppendLine($"Doğru         : {exam.DogruSayisi}");
            sb.AppendLine($"Yanlış        : {exam.YanlisSayisi}");
            sb.AppendLine($"Boş           : {exam.ToplamSoru - exam.DogruSayisi - exam.YanlisSayisi}");
            sb.AppendLine();
            sb.AppendLine($"Başarı Oranı  : %{exam.BasariYuzdesi ?? 0:F1}");

            if (exam.ToplamPuan.HasValue)
            {
                sb.AppendLine($"Alınan Puan   : {exam.AlinanPuan ?? 0:F1} / {exam.ToplamPuan:F1}");
            }

            if (exam.OrtalamaCevapSuresi.HasValue)
            {
                sb.AppendLine($"Ort. Süre     : {exam.OrtalamaCevapSuresi:F0} saniye/soru");
            }

            sb.AppendLine("─────────────────────────────────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine("SORU BAZLI DETAYLAR:");
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
                    sb.AppendLine($"    Süre: {cevap.CevaplamaSuresi.Value}sn");
                }

                sb.AppendLine();
            }

            txt.Text = sb.ToString();

            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(txt);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }

        private async void BtnSoruAnalizi_Click(object sender, EventArgs e)
        {
            if (_selectedExam == null) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                var cevaplar = await _sinavCevapService.GetByExamResultAsync(_selectedExam.SinavSonucuID);

                ShowQuestionAnalysis(_selectedExam, cevaplar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Analiz oluşturulurken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ShowQuestionAnalysis(SinavSonucu exam, List<SinavCevap> cevaplar)
        {
            var form = new Form
            {
                Text = $"Soru Bazlı Analiz - {exam.SinavTuru}",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            var txt = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White
            };

            var sb = new StringBuilder();
            sb.AppendLine("╔" + new string('═', 80) + "╗");
            sb.AppendLine("║" + "           SORU BAZLI PERFORMANS ANALİZİ".PadLeft(50).PadRight(80) + "║");
            sb.AppendLine("╚" + new string('═', 80) + "╝");
            sb.AppendLine();

            // Soru tipi bazlı analiz
            var groupedByType = cevaplar.GroupBy(c => c.SoruTipi);

            foreach (var group in groupedByType)
            {
                var total = group.Count();
                var correct = group.Count(c => c.DogruMu);
                var wrong = group.Count(c => !c.DogruMu && !string.IsNullOrWhiteSpace(c.VerilenCevap));
                var empty = group.Count(c => string.IsNullOrWhiteSpace(c.VerilenCevap));
                var successRate = total > 0 ? (correct * 100.0 / total) : 0;

                sb.AppendLine($"📊 {group.Key}:");
                sb.AppendLine($"   Toplam: {total} | ✔️ Doğru: {correct} | ✗ Yanlış: {wrong} | ⭕ Boş: {empty}");
                sb.AppendLine($"   Başarı: %{successRate:F1}");

                if (group.Any(c => c.CevaplamaSuresi.HasValue))
                {
                    var avgTime = group.Where(c => c.CevaplamaSuresi.HasValue)
                                      .Average(c => c.CevaplamaSuresi.Value);
                    sb.AppendLine($"   Ortalama Süre: {avgTime:F0}sn");
                }

                sb.AppendLine();
            }

            sb.AppendLine("─────────────────────────────────────────────────────────────────────────────");
            sb.AppendLine("SORU DETAYLARI:");
            sb.AppendLine();

            foreach (var cevap in cevaplar.OrderBy(c => c.SoruNo))
            {
                var icon = cevap.DogruMu ? "✓" : string.IsNullOrWhiteSpace(cevap.VerilenCevap) ? " " : "✗";
                sb.AppendLine($"[{icon}] Soru {cevap.SoruNo} - {cevap.SoruTipi}");

                if (!string.IsNullOrWhiteSpace(cevap.SoruMetni))
                {
                    var maxLen = Math.Min(70, cevap.SoruMetni.Length);
                    sb.AppendLine($"    {cevap.SoruMetni.Substring(0, maxLen)}...");
                }

                if (!string.IsNullOrWhiteSpace(cevap.VerilenCevap))
                {
                    sb.AppendLine($"    Verilen: {cevap.VerilenCevap} | Doğru: {cevap.DogruCevap}");
                }

                if (cevap.CevaplamaSuresi.HasValue)
                {
                    sb.AppendLine($"    ⏱️ Süre: {cevap.CevaplamaSuresi}sn");
                }

                if (cevap.SoruTipi == "Klasik" && cevap.EslesmeYuzdesi.HasValue)
                {
                    sb.AppendLine($"    📊 Eşleşme: %{cevap.EslesmeYuzdesi:F0} - Puan: {cevap.AlinanPuan:F1}/{cevap.ToplamPuan}");
                }

                sb.AppendLine();
            }

            txt.Text = sb.ToString();

            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(txt);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }

        private void SinavSonucRaporForm_Load(object sender, EventArgs e)
        {

        }
    }
}


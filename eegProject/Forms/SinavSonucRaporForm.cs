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
        private SplitContainer splitRight;
        private const int SummaryPanelWidth = 320;
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
        private Button btnSinavSil;
        
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

            // Sağ taraf - Oturumlar ve özet
            splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2
            };
            splitRight.SizeChanged += (s, e) => EnsureSplitterLayout();

            // Oturumlar & Sınavlar
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
            // Sağ tık menüsü
            var contextMenu = new ContextMenuStrip();
            var itemSil = new ToolStripMenuItem("🗑️ Sınavı Sil");
            itemSil.Click += BtnSinavSil_Click;
            contextMenu.Items.Add(itemSil);

            treeOturumlar = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                FullRowSelect = true,
                ContextMenuStrip = contextMenu
            };
            treeOturumlar.AfterSelect += TreeOturumlar_AfterSelect;
            treeOturumlar.NodeMouseClick += (s, e) => 
            {
                if (e.Button == MouseButtons.Right) 
                    treeOturumlar.SelectedNode = e.Node;
            };

            pnlOturumlar.Controls.Add(treeOturumlar);
            pnlOturumlar.Controls.Add(lblOturumlar);

            splitRight.Panel1.Controls.Add(pnlOturumlar);

            // Sağ panel - Özet
            pnlOzet = new Panel
            {
                Dock = DockStyle.Fill,
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
            var summaryLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            lblSinavAdi = CreateLabel("Sınav: -", 20);
            lblTarih = CreateLabel("Tarih: -", 50);
            lblBasari = CreateLabel("Başarı: -", 80);
            lblSure = CreateLabel("Süre: -", 110);
            lblToplamSoru = CreateLabel("Toplam Soru: -", 140);
            lblDogru = CreateLabel("✔️ Doğru: -", 170);
            lblYanlis = CreateLabel("✖️ Yanlış: -", 200);
            lblBos = CreateLabel("⭕ Boş: -", 230);

            summaryLayout.Controls.AddRange(new Control[]
            {
                lblSinavAdi, lblTarih, lblBasari, lblSure,
                lblToplamSoru, lblDogru, lblYanlis, lblBos
            });

            btnDetayliRapor = new Button
            {
                Text = "📝 Rapor",
                Width = 120,
                Height = 40,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(0, 0, 5, 0)
            };
            btnDetayliRapor.Click += BtnDetayliRapor_Click;

            btnSoruAnalizi = new Button
            {
                Text = "🔍 Analiz",
                Width = 120,
                Height = 40,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(0, 0, 5, 0)
            };
            btnSoruAnalizi.Click += BtnSoruAnalizi_Click;

            btnSinavSil = new Button
            {
                Text = "🗑️ Sil",
                Width = 100,
                Height = 40,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnSinavSil.Click += BtnSinavSil_Click;

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 10, 0, 0)
            };
            actionPanel.Controls.AddRange(new Control[]
            {
                btnDetayliRapor,
                btnSoruAnalizi,
                btnSinavSil
            });

            pnlOzetIcerik.Controls.Add(summaryLayout);
            pnlOzetIcerik.Controls.Add(actionPanel);

            pnlOzet.Controls.Add(pnlOzetIcerik);
            pnlOzet.Controls.Add(lblOzetBaslik);

            splitRight.Panel2.Controls.Add(pnlOzet);
            splitMain.Panel2.Controls.Add(splitRight);

            this.Controls.Add(splitMain);
        }

        private Label CreateLabel(string text, int top)
        {
            int marginTop = top <= 20 ? 0 : 8;
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(400, 0),
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, marginTop, 0, 0)
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
                MessageBox.Show(this, $"KullanÄ±cÄ±lar yÃ¼klenirken hata:\n{ex.Message}",
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

                // KullanÄ±cÄ±nÄ±n tÃ¼m sÄ±nav sonuÃ§larÄ±nÄ± oturum bazlÄ± Ã§ek
                var examResults = await _examService.GetByUserWithSessionsAsync(userId);

                if (examResults.Count == 0)
                {
                    var emptyNode = new TreeNode("Bu kullanıcının sınavı bulunmuyor");
                    emptyNode.ForeColor = Color.Gray;
                    treeOturumlar.Nodes.Add(emptyNode);
                    return;
                }

                // Oturum bazlÄ± grupla
                var groupedBySession = examResults.GroupBy(e => e.OturumID);

                foreach (var sessionGroup in groupedBySession.OrderByDescending(g => g.First().BaslamaTarihi))
                {
                    var firstExam = sessionGroup.First();
                    var oturum = firstExam.Oturum;
                    
                    var sessionNodeText = $"📂 Oturum #{oturum.OturumID} - " +
                                         $"{oturum.DeneyTuru ?? "Genel"}" +
                                         $"{(string.IsNullOrEmpty(oturum.ZamanEtiketi) ? "" : " - " + oturum.ZamanEtiketi)}" +
                                         $" ({oturum.KayitBaslangic?.ToString("dd.MM.yyyy HH:mm") ?? "Tarih yok"})";
                    
                    var sessionNode = new TreeNode(sessionNodeText);
                    sessionNode.Tag = oturum;
                    sessionNode.NodeFont = new Font("Segoe UI", 10, FontStyle.Bold);

                    // Bu oturumdaki tüm sınavlar
                    foreach (var exam in sessionGroup.OrderBy(e => e.BaslamaTarihi))
                    {
                        // İstatistikleri veritabanından (cevaplardan) taze çek
                        var cevaplar = await _sinavCevapService.GetByExamResultAsync(exam.SinavSonucuID);
                        if (cevaplar != null && cevaplar.Count > 0)
                        {
                            var dogru = cevaplar.Count(c => c.DogruMu);
                            var bos = cevaplar.Count(c => string.IsNullOrWhiteSpace(c.VerilenCevap));
                            var yanlis = cevaplar.Count - dogru - bos;
                            var toplamPuan = cevaplar.Sum(c => c.ToplamPuan ?? 0);
                            var alinanPuan = cevaplar.Sum(c => c.AlinanPuan ?? 0);

                            exam.DogruSayisi = dogru;
                            exam.YanlisSayisi = yanlis;
                            exam.ToplamSoru = cevaplar.Count;
                            exam.ToplamPuan = toplamPuan;
                            exam.AlinanPuan = alinanPuan;
                            exam.BasariYuzdesi = (dogru * 100.0) / cevaplar.Count;
                        }

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

        private async void TreeOturumlar_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var selectedExam = e.Node.Tag as SinavSonucu;
            if (selectedExam == null)
            {
                ClearOzetPanel();
                return;
            }

            _selectedExam = selectedExam;

            // İstatistikleri cevaplardan yeniden hesapla (DB'de 0 kalmış olabilir)
            try
            {
                this.Cursor = Cursors.WaitCursor;
                var cevaplar = await _sinavCevapService.GetByExamResultAsync(selectedExam.SinavSonucuID);
                
                if (cevaplar != null && cevaplar.Count > 0)
                {
                    var dogru = cevaplar.Count(c => c.DogruMu);
                    var bos = cevaplar.Count(c => string.IsNullOrWhiteSpace(c.VerilenCevap));
                    var yanlis = cevaplar.Count - dogru - bos;
                    var toplamPuan = cevaplar.Sum(c => c.ToplamPuan ?? 0);
                    var alinanPuan = cevaplar.Sum(c => c.AlinanPuan ?? 0);

                    // Nesneyi güncelle
                    _selectedExam.DogruSayisi = dogru;
                    _selectedExam.YanlisSayisi = yanlis;
                    _selectedExam.ToplamSoru = cevaplar.Count;
                    _selectedExam.ToplamPuan = toplamPuan;
                    _selectedExam.AlinanPuan = alinanPuan;
                    
                    if (cevaplar.Count > 0)
                    {
                        _selectedExam.BasariYuzdesi = (dogru * 100.0) / cevaplar.Count;
                    }

                    // TreeView node metnini de güncelle
                    var basariYuzdesi = _selectedExam.BasariYuzdesi ?? 0;
                    var icon = basariYuzdesi >= 70 ? "✅" : basariYuzdesi >= 50 ? "⚠️" : "❌";
                    
                    e.Node.Text = $"📊 {_selectedExam.SinavTuru} {icon} - " +
                                  $"%{basariYuzdesi:F0} " +
                                  $"({_selectedExam.DogruSayisi}/{_selectedExam.ToplamSoru}) - " +
                                  $"{_selectedExam.BaslamaTarihi:HH:mm}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("İstatistik hesaplama hatası: " + ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            ShowExamSummary(_selectedExam);
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
            btnSinavSil.Enabled = true;
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
            btnSinavSil.Enabled = false;
            _selectedExam = null;
        }

        private async void BtnDetayliRapor_Click(object sender, EventArgs e)
        {
            if (_selectedExam == null) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                // TÃ¼m soru cevaplarÄ±nÄ± Ã§ek
                var cevaplar = await _sinavCevapService.GetByExamResultAsync(_selectedExam.SinavSonucuID);
                
                // DetaylÄ± rapor gÃ¶ster
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

        private async void BtnSinavSil_Click(object sender, EventArgs e)
        {
            if (_selectedExam == null) return;

            var confirm = MessageBox.Show(this,
                $"{_selectedExam.SinavTuru} sınavını silmek istediğinize emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                await _examService.DeleteAsync(_selectedExam.SinavSonucuID);

                MessageBox.Show(this,
                    "Sınav sonucu silindi.",
                    "Bilgi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                var selectedUser = lstUsers.SelectedItem as Kullanici;
                if (selectedUser != null)
                {
                    await LoadUserSessionsWithExamsAsync(selectedUser.KullaniciID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Sınav silinirken hata oluştu:\\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void EnsureSplitterLayout()
        {
            if (splitRight == null || splitRight.Width <= 0)
                return;

            // Minimum boyut kontrolü
            if (splitRight.Width < splitRight.Panel1MinSize + splitRight.Panel2MinSize)
                return;

            var target = splitRight.Width - SummaryPanelWidth;
            
            // Alt sınır kontrolü (Panel1MinSize)
            if (target < splitRight.Panel1MinSize)
                target = splitRight.Panel1MinSize;

            // Üst sınır kontrolü (Width - Panel2MinSize)
            var maxDist = splitRight.Width - splitRight.Panel2MinSize;
            if (target > maxDist)
                target = maxDist;

            try
            {
                splitRight.SplitterDistance = target;
            }
            catch
            {
                // Olası diğer hataları yut, kritik değil
            }
        }

        private void ShowDetailedReport(SinavSonucu exam, List<SinavCevap> cevaplar)
        {
            var form = new Form
            {
                Text = $"Detaylı Sınav Raporu - {exam.SinavTuru}",
                Size = new Size(1100, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            // Üst Bilgi Paneli
            var pnlInfo = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 80, 
                BackColor = Color.WhiteSmoke, 
                Padding = new Padding(15) 
            };
            
            var lblInfo = new Label 
            { 
                Dock = DockStyle.Fill, 
                Text = $"Sınav: {exam.SinavTuru} | Tarih: {exam.BaslamaTarihi:dd.MM.yyyy HH:mm}\n" +
                       $"Oturum: #{exam.OturumID} | Başarı: %{exam.BasariYuzdesi:F1} | Puan: {exam.AlinanPuan:F1}/{exam.ToplamPuan:F1}",
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlInfo.Controls.Add(lblInfo);

            // Grid
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                RowTemplate = { Height = 35 }
            };

            grid.Columns.Add("No", "No");
            grid.Columns[0].Width = 50;
            grid.Columns.Add("Tip", "Soru Tipi");
            grid.Columns.Add("Soru", "Soru Özeti");
            grid.Columns.Add("Durum", "Durum");
            grid.Columns.Add("Verilen", "Verilen Cevap");
            grid.Columns.Add("Dogru", "Doğru Cevap");
            grid.Columns.Add("Sure", "Süre (sn)");
            grid.Columns.Add("Puan", "Puan");

            foreach (var cevap in cevaplar.OrderBy(c => c.SoruNo))
            {
                var status = cevap.DogruMu ? "DOĞRU" : string.IsNullOrWhiteSpace(cevap.VerilenCevap) ? "BOŞ" : "YANLIŞ";
                var soruOzet = string.IsNullOrWhiteSpace(cevap.SoruMetni) ? "" : 
                              cevap.SoruMetni.Length > 50 ? cevap.SoruMetni.Substring(0, 47) + "..." : cevap.SoruMetni;

                var rowIndex = grid.Rows.Add(
                    cevap.SoruNo,
                    cevap.SoruTipi,
                    soruOzet,
                    status,
                    cevap.VerilenCevap,
                    cevap.DogruCevap,
                    cevap.CevaplamaSuresi,
                    cevap.AlinanPuan
                );
                
                var row = grid.Rows[rowIndex];
                if (cevap.DogruMu) 
                    row.DefaultCellStyle.BackColor = Color.FromArgb(220, 255, 220); // Açık yeşil
                else if (string.IsNullOrWhiteSpace(cevap.VerilenCevap)) 
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 240); // Açık sarı
                else 
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220); // Açık kırmızı
            }

            // Kapat Butonu
            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(grid);
            form.Controls.Add(pnlInfo);
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
                Size = new Size(1000, 750),
                StartPosition = FormStartPosition.CenterParent
            };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
                FixedPanel = FixedPanel.Panel1
            };

            // 1. Üst Panel: Tip Bazlı Özet (Grid)
            var gridSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10)
            };
            
            gridSummary.Columns.Add("Tip", "Soru Tipi");
            gridSummary.Columns.Add("Toplam", "Toplam");
            gridSummary.Columns.Add("Dogru", "Doğru");
            gridSummary.Columns.Add("Yanlis", "Yanlış");
            gridSummary.Columns.Add("Bos", "Boş");
            gridSummary.Columns.Add("Basari", "Başarı (%)");
            gridSummary.Columns.Add("Sure", "Ort. Süre (sn)");

            var groupedByType = cevaplar.GroupBy(c => c.SoruTipi);
            foreach (var group in groupedByType)
            {
                var total = group.Count();
                var correct = group.Count(c => c.DogruMu);
                var wrong = group.Count(c => !c.DogruMu && !string.IsNullOrWhiteSpace(c.VerilenCevap));
                var empty = group.Count(c => string.IsNullOrWhiteSpace(c.VerilenCevap));
                var successRate = total > 0 ? (correct * 100.0 / total) : 0;
                var avgTime = group.Where(c => c.CevaplamaSuresi.HasValue).Average(c => c.CevaplamaSuresi.Value);

                gridSummary.Rows.Add(
                    group.Key,
                    total,
                    correct,
                    wrong,
                    empty,
                    $"%{successRate:F1}",
                    $"{avgTime:F1}"
                );
            }

            var lblSummaryTitle = new Label 
            { 
                Text = "TİP BAZLI PERFORMANS ÖZETİ", 
                Dock = DockStyle.Top, 
                Height = 30, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10,0,0,0)
            };

            split.Panel1.Controls.Add(gridSummary);
            split.Panel1.Controls.Add(lblSummaryTitle);

            // 2. Alt Panel: Detaylı Analiz (Grid)
            var gridDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9)
            };

            gridDetails.Columns.Add("No", "No");
            gridDetails.Columns[0].Width = 40;
            gridDetails.Columns.Add("Tip", "Tip");
            gridDetails.Columns.Add("Soru", "Soru");
            gridDetails.Columns.Add("Durum", "Durum");
            gridDetails.Columns.Add("Sure", "Süre");
            gridDetails.Columns.Add("Analiz", "Analiz Notu");

            foreach (var cevap in cevaplar.OrderBy(c => c.SoruNo))
            {
                var status = cevap.DogruMu ? "DOĞRU" : string.IsNullOrWhiteSpace(cevap.VerilenCevap) ? "BOŞ" : "YANLIŞ";
                var analysisNote = "";
                if (cevap.SoruTipi == "Klasik" && cevap.EslesmeYuzdesi.HasValue)
                    analysisNote = $"Eşleşme: %{cevap.EslesmeYuzdesi:F0}";
                
                var rowIndex = gridDetails.Rows.Add(
                    cevap.SoruNo,
                    cevap.SoruTipi,
                    cevap.SoruMetni,
                    status,
                    cevap.CevaplamaSuresi + "sn",
                    analysisNote
                );

                var row = gridDetails.Rows[rowIndex];
                if (cevap.DogruMu) row.DefaultCellStyle.ForeColor = Color.Green;
                else if (status == "YANLIŞ") row.DefaultCellStyle.ForeColor = Color.Red;
            }

            var lblDetailsTitle = new Label 
            { 
                Text = "SORU DETAYLARI", 
                Dock = DockStyle.Top, 
                Height = 30, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10,0,0,0)
            };

            split.Panel2.Controls.Add(gridDetails);
            split.Panel2.Controls.Add(lblDetailsTitle);

            // Kapat Butonu
            var btnClose = new Button
            {
                Text = "Kapat",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(split);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }
        private void SinavSonucRaporForm_Load(object sender, EventArgs e)
        {
            EnsureSplitterLayout();
        }
    }
}



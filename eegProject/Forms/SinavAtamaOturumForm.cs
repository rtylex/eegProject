using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    /// <summary>
    /// Oturuma sınav atama formu
    /// </summary>
    public partial class SinavAtamaOturumForm : Form
    {
        private readonly SinavAtamaService _atamaService;
        private readonly SessionService _sessionService;
        private readonly UserService _userService;
        private readonly ExamLoaderService _examLoaderService;
        private readonly int _yoneticiId;

        private List<Oturum> _oturumlar;
        private string _selectedJsonPath;
        private string _selectedJsonContent;

        public SinavAtamaOturumForm(int yoneticiId)
        {
            _atamaService = new SinavAtamaService();
            _sessionService = new SessionService();
            _userService = new UserService();
            _examLoaderService = new ExamLoaderService();
            _yoneticiId = yoneticiId;

            InitializeComponent();
            InitializeFormAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Oturuma Sınav Atama";
            this.Size = new System.Drawing.Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Kullanıcı seçimi
            var lblKullanici = new Label
            {
                Text = "Kullanıcı:",
                Left = 20,
                Top = 20,
                Width = 100
            };

            var cmbKullanici = new ComboBox
            {
                Name = "cmbKullanici",
                Left = 130,
                Top = 17,
                Width = 520,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Event handler InitializeFormAsync'de eklenecek

            // Oturum seçimi
            var lblOturum = new Label
            {
                Text = "Oturum:",
                Left = 20,
                Top = 60,
                Width = 100,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };

            var listOturum = new ListBox
            {
                Name = "listOturum",
                Left = 130,
                Top = 60,
                Width = 520,
                Height = 200,
                Font = new System.Drawing.Font("Consolas", 9)
            };
            listOturum.SelectedIndexChanged += ListOturum_SelectedIndexChanged;

            // Sınav adı
            var lblSinavAdi = new Label
            {
                Text = "Sınav Adı:",
                Left = 20,
                Top = 280,
                Width = 100
            };

            var txtSinavAdi = new TextBox
            {
                Name = "txtSinavAdi",
                Left = 130,
                Top = 277,
                Width = 520
            };

            // Açıklama
            var lblAciklama = new Label
            {
                Text = "Açıklama:",
                Left = 20,
                Top = 320,
                Width = 100
            };

            var txtAciklama = new TextBox
            {
                Name = "txtAciklama",
                Left = 130,
                Top = 317,
                Width = 520,
                Height = 60,
                Multiline = true
            };

            // JSON Seçimi
            var lblJson = new Label
            {
                Text = "Sınav JSON:",
                Left = 20,
                Top = 395,
                Width = 100
            };

            var btnJsonSec = new Button
            {
                Name = "btnJsonSec",
                Text = "JSON Seç...",
                Left = 130,
                Top = 390,
                Width = 120
            };
            btnJsonSec.Click += BtnJsonSec_Click;

            var lblJsonPath = new Label
            {
                Name = "lblJsonPath",
                Text = "Henüz seçilmedi",
                Left = 260,
                Top = 395,
                Width = 390,
                ForeColor = System.Drawing.Color.Gray
            };

            var btnJsonOrnek = new Button
            {
                Text = "Örnek JSON",
                Left = 130,
                Top = 425,
                Width = 120
            };
            btnJsonOrnek.Click += BtnJsonOrnek_Click;

            // Notlar
            var lblNotlar = new Label
            {
                Text = "Notlar:",
                Left = 20,
                Top = 470,
                Width = 100
            };

            var txtNotlar = new TextBox
            {
                Name = "txtNotlar",
                Left = 130,
                Top = 467,
                Width = 520,
                Height = 50,
                Multiline = true
            };

            // Butonlar
            var btnKaydet = new Button
            {
                Text = "Ata",
                Left = 480,
                Top = 530,
                Width = 80,
                Height = 35,
                BackColor = System.Drawing.Color.LightGreen
            };
            btnKaydet.Click += BtnKaydet_Click;

            var btnIptal = new Button
            {
                Text = "İptal",
                Left = 570,
                Top = 530,
                Width = 80,
                Height = 35,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblKullanici, cmbKullanici,
                lblOturum, listOturum,
                lblSinavAdi, txtSinavAdi,
                lblAciklama, txtAciklama,
                lblJson, btnJsonSec, lblJsonPath, btnJsonOrnek,
                lblNotlar, txtNotlar,
                btnKaydet, btnIptal
            });

            this.AcceptButton = btnKaydet;
            this.CancelButton = btnIptal;
        }

        private async void InitializeFormAsync()
        {
            try
            {
                var kullanicilar = await _userService.GetAllAsync();
                
                var cmbKullanici = Controls.Find("cmbKullanici", true).FirstOrDefault() as ComboBox;
                if (cmbKullanici != null && kullanicilar != null && kullanicilar.Count > 0)
                {
                    // Event handler'ı geçici olarak kaldır (çift tetikleme olmasın)
                    cmbKullanici.SelectedIndexChanged -= CmbKullanici_SelectedIndexChanged;
                    
                    cmbKullanici.DataSource = kullanicilar;
                    cmbKullanici.DisplayMember = "AdSoyad";
                    cmbKullanici.ValueMember = "KullaniciID";
                    
                    // Event handler'ı geri ekle
                    cmbKullanici.SelectedIndexChanged += CmbKullanici_SelectedIndexChanged;
                    
                    // İlk kullanıcıyı seç
                    if (cmbKullanici.Items.Count > 0)
                    {
                        cmbKullanici.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kullanıcılar yüklenirken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CmbKullanici_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cmbKullanici = sender as ComboBox;
            if (cmbKullanici?.SelectedValue == null) return;

            // Güvenli cast
            if (cmbKullanici.SelectedValue is int kullaniciId)
            {
                await LoadOturumlarAsync(kullaniciId);
            }
            else
            {
                // İlk yüklemede SelectedValue henüz int değil, bekle
                return;
            }
        }

        private async System.Threading.Tasks.Task LoadOturumlarAsync(int kullaniciId)
        {
            try
            {
                _oturumlar = await _sessionService.GetByUserAsync(kullaniciId);

                var listOturum = Controls.Find("listOturum", true).FirstOrDefault() as ListBox;
                if (listOturum != null)
                {
                    listOturum.Items.Clear();

                    if (_oturumlar != null && _oturumlar.Count > 0)
                    {
                        foreach (var oturum in _oturumlar)
                        {
                            // Oturuma sınav atanmış mı kontrol et
                            var atama = await _atamaService.GetBySessionAsync(oturum.OturumID);
                            string sinavDurumu = atama != null ? $"[{atama.SinavAdi}]" : "[Sınav YOK]";

                            var tarih = oturum.KayitBaslangic?.ToString("dd.MM.yyyy HH:mm") ?? "Tarih yok";
                            var deneyTuru = oturum.DeneyTuru ?? "Genel";
                            var etiket = oturum.ZamanEtiketi ?? "Etiketsiz";

                            var displayText = $"#{oturum.OturumID,-5} | {deneyTuru,-20} | {etiket,-15} | {tarih,-16} | {sinavDurumu}";
                            
                            listOturum.Items.Add(new
                            {
                                Text = displayText,
                                Value = oturum
                            });
                        }

                        listOturum.DisplayMember = "Text";
                    }
                    else
                    {
                        listOturum.Items.Add(new
                        {
                            Text = "Bu kullanıcıya ait oturum bulunamadı",
                            Value = (Oturum)null
                        });
                        listOturum.DisplayMember = "Text";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Oturumlar yüklenirken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListOturum_SelectedIndexChanged(object sender, EventArgs e)
        {
            var list = sender as ListBox;
            if (list?.SelectedItem == null) return;

            dynamic item = list.SelectedItem;
            var oturum = item.Value as Oturum;

            if (oturum != null)
            {
                var txtSinavAdi = Controls.Find("txtSinavAdi", true).FirstOrDefault() as TextBox;
                if (txtSinavAdi != null && string.IsNullOrWhiteSpace(txtSinavAdi.Text))
                {
                    txtSinavAdi.Text = $"{oturum.DeneyTuru ?? "Genel"} Sınavı";
                }
            }
        }

        private void BtnJsonSec_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog
            {
                Title = "Sınav JSON Dosyası Seç",
                Filter = "JSON Dosyaları (*.json)|*.json|Tüm Dosyalar (*.*)|*.*",
                FilterIndex = 1
            })
            {
                if (openDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var examData = _examLoaderService.LoadFromJson(openDialog.FileName);
                        
                        _selectedJsonPath = openDialog.FileName;
                        _selectedJsonContent = System.IO.File.ReadAllText(openDialog.FileName);
                        
                        var lblJsonPath = Controls.Find("lblJsonPath", true).FirstOrDefault() as Label;
                        if (lblJsonPath != null)
                        {
                            lblJsonPath.Text = $"✓ {System.IO.Path.GetFileName(openDialog.FileName)} ({examData.Sorular.Count} soru)";
                            lblJsonPath.ForeColor = System.Drawing.Color.Green;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"JSON dosyası geçersiz:\n{ex.Message}",
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnJsonOrnek_Click(object sender, EventArgs e)
        {
            var sample = _examLoaderService.GetSampleJsonFormat();
            
            var form = new Form
            {
                Text = "Örnek Sınav JSON Formatı",
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

        private async void BtnKaydet_Click(object sender, EventArgs e)
        {
            try
            {
                var listOturum = Controls.Find("listOturum", true).FirstOrDefault() as ListBox;
                var txtSinavAdi = Controls.Find("txtSinavAdi", true).FirstOrDefault() as TextBox;
                var txtAciklama = Controls.Find("txtAciklama", true).FirstOrDefault() as TextBox;
                var txtNotlar = Controls.Find("txtNotlar", true).FirstOrDefault() as TextBox;

                // Validasyon
                if (listOturum?.SelectedItem == null)
                {
                    MessageBox.Show(this, "Lütfen bir oturum seçin.", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                dynamic item = listOturum.SelectedItem;
                var oturum = item.Value as Oturum;

                if (string.IsNullOrWhiteSpace(txtSinavAdi?.Text))
                {
                    MessageBox.Show(this, "Lütfen sınav adı girin.", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtSinavAdi?.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_selectedJsonPath) && string.IsNullOrWhiteSpace(_selectedJsonContent))
                {
                    MessageBox.Show(this, "Lütfen bir sınav JSON dosyası seçin.", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                this.Cursor = Cursors.WaitCursor;

                // Oturuma atama yap
                await _atamaService.CreateForSessionAsync(
                    oturum.OturumID,
                    txtSinavAdi.Text.Trim(),
                    txtAciklama?.Text?.Trim(),
                    _selectedJsonPath,
                    _selectedJsonContent,
                    _yoneticiId,
                    txtNotlar?.Text?.Trim()
                );

                this.Cursor = Cursors.Default;

                MessageBox.Show(this, 
                    $"Sınav başarıyla atandı!\n\nOturum: #{oturum.OturumID} ({oturum.DeneyTuru})\nSınav: {txtSinavAdi.Text}",
                    "Başarılı",
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, $"Sınav atanırken hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


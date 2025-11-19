using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    public partial class SinavAtamaForm : Form
    {
        private readonly SinavAtamaService _atamaService;
        private readonly UserService _userService;
        private readonly ExamLoaderService _examLoaderService;
        private readonly int _yoneticiId;

        private List<Kullanici> _kullanicilar;
        private string _selectedJsonPath;
        private string _selectedJsonContent;

        public SinavAtamaForm(int yoneticiId)
        {
            _atamaService = new SinavAtamaService();
            _userService = new UserService();
            _examLoaderService = new ExamLoaderService();
            _yoneticiId = yoneticiId;

            InitializeComponent();
            InitializeFormAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Sınav Atama";
            this.Size = new System.Drawing.Size(600, 500);
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
                Width = 420,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Sınav adı
            var lblSinavAdi = new Label
            {
                Text = "Sınav Adı:",
                Left = 20,
                Top = 60,
                Width = 100
            };

            var txtSinavAdi = new TextBox
            {
                Name = "txtSinavAdi",
                Left = 130,
                Top = 57,
                Width = 420
            };

            // Açıklama
            var lblAciklama = new Label
            {
                Text = "Açıklama:",
                Left = 20,
                Top = 100,
                Width = 100
            };

            var txtAciklama = new TextBox
            {
                Name = "txtAciklama",
                Left = 130,
                Top = 97,
                Width = 420,
                Height = 60,
                Multiline = true
            };

            // JSON Seçimi
            var lblJson = new Label
            {
                Text = "Sınav JSON:",
                Left = 20,
                Top = 175,
                Width = 100
            };

            var btnJsonSec = new Button
            {
                Name = "btnJsonSec",
                Text = "JSON Seç...",
                Left = 130,
                Top = 170,
                Width = 120
            };
            btnJsonSec.Click += BtnJsonSec_Click;

            var lblJsonPath = new Label
            {
                Name = "lblJsonPath",
                Text = "Henüz seçilmedi",
                Left = 260,
                Top = 175,
                Width = 290,
                ForeColor = System.Drawing.Color.Gray
            };

            var btnJsonOrnek = new Button
            {
                Text = "Örnek JSON",
                Left = 130,
                Top = 205,
                Width = 120
            };
            btnJsonOrnek.Click += BtnJsonOrnek_Click;

            // Son geçerlilik tarihi
            var chkGecerlilik = new CheckBox
            {
                Name = "chkGecerlilik",
                Text = "Son Geçerlilik Tarihi Belirle",
                Left = 20,
                Top = 250,
                Width = 250
            };
            chkGecerlilik.CheckedChanged += (s, e) =>
            {
                var dtp = Controls.Find("dtpGecerlilik", true).FirstOrDefault() as DateTimePicker;
                if (dtp != null) dtp.Enabled = chkGecerlilik.Checked;
            };

            var dtpGecerlilik = new DateTimePicker
            {
                Name = "dtpGecerlilik",
                Left = 130,
                Top = 280,
                Width = 200,
                Enabled = false,
                Value = DateTime.Now.AddDays(7)
            };

            // Notlar
            var lblNotlar = new Label
            {
                Text = "Notlar:",
                Left = 20,
                Top = 320,
                Width = 100
            };

            var txtNotlar = new TextBox
            {
                Name = "txtNotlar",
                Left = 130,
                Top = 317,
                Width = 420,
                Height = 60,
                Multiline = true
            };

            // Butonlar
            var btnKaydet = new Button
            {
                Text = "Ata",
                Left = 350,
                Top = 400,
                Width = 90,
                Height = 35,
                BackColor = System.Drawing.Color.LightGreen
            };
            btnKaydet.Click += BtnKaydet_Click;

            var btnIptal = new Button
            {
                Text = "İptal",
                Left = 450,
                Top = 400,
                Width = 90,
                Height = 35,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblKullanici, cmbKullanici,
                lblSinavAdi, txtSinavAdi,
                lblAciklama, txtAciklama,
                lblJson, btnJsonSec, lblJsonPath, btnJsonOrnek,
                chkGecerlilik, dtpGecerlilik,
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
                _kullanicilar = await _userService.GetAllAsync();
                
                var cmbKullanici = Controls.Find("cmbKullanici", true).FirstOrDefault() as ComboBox;
                if (cmbKullanici != null)
                {
                    cmbKullanici.DataSource = _kullanicilar;
                    cmbKullanici.DisplayMember = "AdSoyad";
                    cmbKullanici.ValueMember = "KullaniciID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kullanıcılar yüklenirken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        // JSON dosyasını test amaçlı yükle
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
                var cmbKullanici = Controls.Find("cmbKullanici", true).FirstOrDefault() as ComboBox;
                var txtSinavAdi = Controls.Find("txtSinavAdi", true).FirstOrDefault() as TextBox;
                var txtAciklama = Controls.Find("txtAciklama", true).FirstOrDefault() as TextBox;
                var chkGecerlilik = Controls.Find("chkGecerlilik", true).FirstOrDefault() as CheckBox;
                var dtpGecerlilik = Controls.Find("dtpGecerlilik", true).FirstOrDefault() as DateTimePicker;
                var txtNotlar = Controls.Find("txtNotlar", true).FirstOrDefault() as TextBox;

                // Validasyon
                if (cmbKullanici?.SelectedValue == null)
                {
                    MessageBox.Show(this, "Lütfen bir kullanıcı seçin.", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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

                // Atama oluştur
                var kullaniciId = (int)cmbKullanici.SelectedValue;
                DateTime? sonGecerlilik = chkGecerlilik.Checked ? dtpGecerlilik.Value : (DateTime?)null;

                await _atamaService.CreateAsync(
                    kullaniciId,
                    txtSinavAdi.Text.Trim(),
                    txtAciklama?.Text?.Trim(),
                    _selectedJsonPath,
                    _selectedJsonContent,
                    _yoneticiId,
                    sonGecerlilik,
                    txtNotlar?.Text?.Trim()
                );

                this.Cursor = Cursors.Default;

                MessageBox.Show(this, "Sınav başarıyla atandı!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

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


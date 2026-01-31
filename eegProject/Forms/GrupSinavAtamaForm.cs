using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    /// <summary>
    /// Deney grubuna toplu sınav atama formu
    /// </summary>
    internal sealed class GrupSinavAtamaForm : Form
    {
        private readonly SinavAtamaService _atamaService = new SinavAtamaService();
        private readonly DeneyGrubuService _deneyGrubuService = new DeneyGrubuService();
        private readonly int _yoneticiId;

        private ComboBox _cmbGrup;
        private TextBox _txtSinavAdi;
        private TextBox _txtAciklama;
        private TextBox _txtJsonPath;
        private TextBox _txtNotlar;
        private Label _lblGrupInfo;
        private Button _btnJsonSec;
        private Button _btnKaydet;
        private Button _btnIptal;

        private string _jsonContent;
        private List<DeneyGrubu> _gruplar = new List<DeneyGrubu>();

        public GrupSinavAtamaForm(int yoneticiId)
        {
            _yoneticiId = yoneticiId;
            InitializeComponent();
            Load += async (s, e) => await LoadGruplarAsync();
        }

        private void InitializeComponent()
        {
            Text = "📋 Gruba Toplu Sınav Ata";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(500, 450);
            MinimumSize = new Size(450, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(15)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // Grup seçimi
            mainPanel.Controls.Add(new Label { Text = "Deney Grubu:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            _cmbGrup = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbGrup.SelectedIndexChanged += CmbGrup_SelectedIndexChanged;
            mainPanel.Controls.Add(_cmbGrup, 1, row++);

            // Grup bilgisi
            _lblGrupInfo = new Label
            {
                Text = "Grup seçiniz...",
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(new Label(), 0, row);
            mainPanel.Controls.Add(_lblGrupInfo, 1, row++);

            // Sınav adı
            mainPanel.Controls.Add(new Label { Text = "Sınav Adı:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            _txtSinavAdi = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_txtSinavAdi, 1, row++);

            // Açıklama
            mainPanel.Controls.Add(new Label { Text = "Açıklama:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            _txtAciklama = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_txtAciklama, 1, row++);

            // JSON dosyası
            mainPanel.Controls.Add(new Label { Text = "JSON Dosyası:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            var jsonPanel = new Panel { Dock = DockStyle.Fill };
            _txtJsonPath = new TextBox { Width = 240, ReadOnly = true };
            _btnJsonSec = new Button { Text = "Seç...", Left = 250, Width = 70 };
            _btnJsonSec.Click += BtnJsonSec_Click;
            jsonPanel.Controls.Add(_txtJsonPath);
            jsonPanel.Controls.Add(_btnJsonSec);
            mainPanel.Controls.Add(jsonPanel, 1, row++);

            // Notlar
            mainPanel.Controls.Add(new Label { Text = "Notlar:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            _txtNotlar = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            mainPanel.Controls.Add(_txtNotlar, 1, row++);

            // Butonlar
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            _btnIptal = new Button { Text = "İptal", Width = 80, DialogResult = DialogResult.Cancel };
            _btnKaydet = new Button { Text = "Toplu Ata", Width = 100, BackColor = Color.LightGreen };
            _btnKaydet.Click += BtnKaydet_Click;
            buttonPanel.Controls.Add(_btnIptal);
            buttonPanel.Controls.Add(_btnKaydet);
            mainPanel.Controls.Add(new Label(), 0, row);
            mainPanel.Controls.Add(buttonPanel, 1, row);

            Controls.Add(mainPanel);
            AcceptButton = _btnKaydet;
            CancelButton = _btnIptal;
        }

        private async System.Threading.Tasks.Task LoadGruplarAsync()
        {
            try
            {
                _gruplar = await _deneyGrubuService.GetActiveAsync();
                
                _cmbGrup.DataSource = null;
                var list = _gruplar.Select(g => new { Id = g.DeneyGrubuID, Name = g.GrupAdi }).ToList();
                _cmbGrup.DataSource = list;
                _cmbGrup.DisplayMember = "Name";
                _cmbGrup.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Gruplar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CmbGrup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbGrup.SelectedIndex < 0 || _cmbGrup.SelectedIndex >= _gruplar.Count) return;
            
            var grup = _gruplar[_cmbGrup.SelectedIndex];
            var grupId = grup.DeneyGrubuID;
            try
            {
                var count = await _deneyGrubuService.GetUsageCountAsync(grupId);
                if (_lblGrupInfo != null)
                {
                    _lblGrupInfo.Text = $"👥 {count} kullanıcıya atanacak";
                    _lblGrupInfo.ForeColor = count > 0 ? Color.DarkGreen : Color.DarkRed;
                }
            }
            catch { }
        }

        private Label FindLabelByText()
        {
            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel tlp)
                {
                    foreach (Control inner in tlp.Controls)
                    {
                        if (inner is Label lbl && (lbl.Text.Contains("kullanıcı") || lbl.Text.Contains("Grup seç")))
                            return lbl;
                    }
                }
            }
            return _lblGrupInfo;
        }

        private void BtnJsonSec_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Sınav JSON Dosyası Seç",
                Filter = "JSON Dosyaları (*.json)|*.json|Tüm Dosyalar (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _jsonContent = File.ReadAllText(dialog.FileName);
                        _txtJsonPath.Text = dialog.FileName;
                        
                        // Dosya adından sınav adı çıkar
                        if (string.IsNullOrWhiteSpace(_txtSinavAdi.Text))
                        {
                            _txtSinavAdi.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Dosya okunamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BtnKaydet_Click(object sender, EventArgs e)
        {
            // Validasyon
            if (_cmbGrup.SelectedIndex < 0 || _cmbGrup.SelectedIndex >= _gruplar.Count)
            {
                MessageBox.Show(this, "Lütfen bir grup seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtSinavAdi.Text))
            {
                MessageBox.Show(this, "Lütfen sınav adı giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_jsonContent))
            {
                MessageBox.Show(this, "Lütfen sınav JSON dosyası seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var grup = _gruplar[_cmbGrup.SelectedIndex];
            var grupId = grup.DeneyGrubuID;
            var grupAdi = grup.GrupAdi;

            var confirm = MessageBox.Show(this,
                $"'{_txtSinavAdi.Text}' sınavı '{grupAdi}' grubundaki tüm kullanıcılara atanacak.\n\nDevam edilsin mi?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnKaydet.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var count = await _atamaService.CreateForGroupAsync(
                    grupId,
                    _txtSinavAdi.Text.Trim(),
                    _txtAciklama.Text.Trim(),
                    _txtJsonPath.Text,
                    _jsonContent,
                    _yoneticiId,
                    _txtNotlar.Text.Trim()
                );

                MessageBox.Show(this,
                    $"✅ Sınav başarıyla atandı!\n\n{count} kullanıcıya atama yapıldı.",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Atama sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnKaydet.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }
}

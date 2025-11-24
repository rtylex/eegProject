using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    public partial class SinavAtamaListForm : Form
    {
        private readonly SinavAtamaService _atamaService;
        private readonly int _yoneticiId;
        private DataGridView _grid;

        public SinavAtamaListForm(int yoneticiId)
        {
            _atamaService = new SinavAtamaService();
            _yoneticiId = yoneticiId;

            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Sınav Atamaları";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // Toolbar
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.WhiteSmoke
            };

            var btnYeniAtama = new Button
            {
                Text = "+ Yeni Atama",
                Left = 10,
                Top = 10,
                Width = 120,
                Height = 30,
                BackColor = Color.LightGreen
            };
            btnYeniAtama.Click += BtnYeniAtama_Click;

            var btnYenile = new Button
            {
                Text = "🔄 Yenile",
                Left = 140,
                Top = 10,
                Width = 100,
                Height = 30
            };
            btnYenile.Click += (s, e) => LoadDataAsync();

            var btnSil = new Button
            {
                Name = "btnSil",
                Text = "🗑️ Sil",
                Left = 250,
                Top = 10,
                Width = 100,
                Height = 30,
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnSil.Click += BtnSil_Click;

            toolbar.Controls.AddRange(new Control[] { btnYeniAtama, btnYenile, btnSil });

            // Grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false
            };

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "AtamaID",
                    HeaderText = "ID",
                    DataPropertyName = "AtamaID",
                    Width = 50,
                    Visible = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "KullaniciAdi",
                    HeaderText = "Kullanıcı",
                    DataPropertyName = "KullaniciAdi",
                    Width = 150
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "OturumBilgi",
                    HeaderText = "Oturum",
                    DataPropertyName = "OturumBilgi",
                    Width = 200
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "SinavAdi",
                    HeaderText = "Sınav Adı",
                    DataPropertyName = "SinavAdi",
                    Width = 180
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "SinavAciklama",
                    HeaderText = "Açıklama",
                    DataPropertyName = "SinavAciklama",
                    Width = 200
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "AtamaTarihi",
                    HeaderText = "Atama Tarihi",
                    DataPropertyName = "AtamaTarihiStr",
                    Width = 120
                },
                new DataGridViewCheckBoxColumn
                {
                    Name = "TamamlandiMi",
                    HeaderText = "Tamamlandı?",
                    DataPropertyName = "TamamlandiMi",
                    Width = 100
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "TamamlanmaTarihi",
                    HeaderText = "Tamamlanma",
                    DataPropertyName = "TamamlanmaTarihiStr",
                    Width = 120
                }
            });

            _grid.SelectionChanged += Grid_SelectionChanged;

            this.Controls.Add(_grid);
            this.Controls.Add(toolbar);
        }

        private async void LoadDataAsync()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                var atamalar = await _atamaService.GetByManagerAsync(_yoneticiId);

                // Sadece oturum bazlı atamaları filtrele
                var oturumAtamalari = atamalar.Where(a => a.OturumID.HasValue).ToList();

                var dataSource = oturumAtamalari.Select(a => new
                {
                    a.AtamaID,
                    KullaniciAdi = a.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    OturumBilgi = a.Oturum != null 
                        ? $"#{a.Oturum.OturumID} - {a.Oturum.DeneyTuru ?? "Genel"} {(string.IsNullOrEmpty(a.Oturum.ZamanEtiketi) ? "" : " - " + a.Oturum.ZamanEtiketi)}"
                        : "Oturum yok",
                    a.SinavAdi,
                    a.SinavAciklama,
                    AtamaTarihiStr = a.AtamaTarihi.ToString("dd.MM.yyyy HH:mm"),
                    a.TamamlandiMi,
                    TamamlanmaTarihiStr = a.TamamlanmaTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "-"
                }).ToList();

                _grid.DataSource = dataSource;

                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, $"Veriler yüklenirken hata:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            var btnSil = Controls.Find("btnSil", true).FirstOrDefault() as Button;
            if (btnSil != null)
            {
                btnSil.Enabled = _grid.SelectedRows.Count > 0;
            }
        }

        private void BtnYeniAtama_Click(object sender, EventArgs e)
        {
            using (var form = new SinavAtamaOturumForm(_yoneticiId))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    LoadDataAsync();
                }
            }
        }

        private async void BtnSil_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
                return;

            var selectedRow = _grid.SelectedRows[0];
            var atamaId = Convert.ToInt32(selectedRow.Cells["AtamaID"].Value);
            var sinavAdi = selectedRow.Cells["SinavAdi"].Value?.ToString();

            var result = MessageBox.Show(this,
                $"'{sinavAdi}' atamasını silmek istediğinize emin misiniz?\n\nBu işlem geri alınamaz!",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    await _atamaService.DeleteAsync(atamaId);
                    this.Cursor = Cursors.Default;

                    MessageBox.Show(this, "Atama silindi.", "Başarılı",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadDataAsync();
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    MessageBox.Show(this, $"Silme işleminde hata:\n{ex.Message}",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}


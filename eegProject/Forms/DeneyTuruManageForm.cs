using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    internal sealed class DeneyTuruManageForm : Form
    {
        private readonly DeneyTuruService _service = new DeneyTuruService();
        private readonly BindingList<DeneyTuru> _items = new BindingList<DeneyTuru>();
        private readonly ListBox _listBox;

        public DeneyTuruManageForm()
        {
            Text = "Deney Turleri Yonetimi";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(500, 450);

            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DataSource = _items,
                DisplayMember = "TurAdi",
                IntegralHeight = false
            };

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.TopDown,
                Width = 120,
                Padding = new Padding(6)
            };

            var btnAdd = new Button { Text = "Ekle", Width = 100, Height = 36 };
            btnAdd.Click += async (sender, _) => await AddItemAsync();

            var btnEdit = new Button { Text = "Duzenle", Width = 100, Height = 36 };
            btnEdit.Click += async (sender, _) => await EditItemAsync();

            var btnDelete = new Button { Text = "Sil", Width = 100, Height = 36 };
            btnDelete.Click += async (sender, _) => await DeleteItemAsync();

            var btnRefresh = new Button { Text = "Yenile", Width = 100, Height = 36 };
            btnRefresh.Click += async (sender, _) => await LoadDataAsync();

            var btnClose = new Button { Text = "Kapat", Width = 100, Height = 36, DialogResult = DialogResult.OK };

            panelButtons.Controls.Add(btnAdd);
            panelButtons.Controls.Add(btnEdit);
            panelButtons.Controls.Add(btnDelete);
            panelButtons.Controls.Add(btnRefresh);
            panelButtons.Controls.Add(btnClose);

            Controls.Add(_listBox);
            Controls.Add(panelButtons);

            Load += async (sender, _) => await LoadDataAsync();
            AcceptButton = btnClose;
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                var data = await _service.GetAllAsync();
                _items.Clear();
                foreach (var item in data.OrderBy(d => d.TurAdi))
                {
                    _items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Veri yuklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task AddItemAsync()
        {
            var dialog = new TextInputDialog("Yeni Deney Turu", "Tur Adi:", "");
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                await _service.CreateAsync(dialog.InputValue);
                await LoadDataAsync();
                MessageBox.Show(this, "Deney turu eklendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task EditItemAsync()
        {
            var selected = _listBox.SelectedItem as DeneyTuru;
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir deger seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = new TextInputDialog("Deney Turu Duzenle", "Tur Adi:", selected.TurAdi);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                await _service.UpdateAsync(selected.DeneyTuruID, dialog.InputValue);
                await LoadDataAsync();
                MessageBox.Show(this, "Deney turu guncellendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task DeleteItemAsync()
        {
            var selected = _listBox.SelectedItem as DeneyTuru;
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir deger seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usageCount = await _service.GetUsageCountAsync(selected.DeneyTuruID);
            var message = usageCount > 0
                ? $"'{selected.TurAdi}' {usageCount} oturumda kullanilmaktadir. Pasif yapmak istiyor musunuz?"
                : $"'{selected.TurAdi}' silmek istiyor musunuz?";

            var result = MessageBox.Show(this, message, "Silme Onayi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            try
            {
                await _service.DeleteAsync(selected.DeneyTuruID);
                await LoadDataAsync();
                var msg = usageCount > 0 ? "Deney turu pasif yapildi." : "Deney turu silindi.";
                MessageBox.Show(this, msg, "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DeneyTuruManageForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "DeneyTuruManageForm";
            this.Load += new System.EventHandler(this.DeneyTuruManageForm_Load);
            this.ResumeLayout(false);

        }

        private void DeneyTuruManageForm_Load(object sender, EventArgs e)
        {

        }
    }

    internal class TextInputDialog : Form
    {
        public string InputValue => _txtInput.Text.Trim();
        private readonly TextBox _txtInput;

        public TextInputDialog(string title, string label, string initialValue)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(400, 120);

            var lbl = new Label { Text = label, Left = 20, Top = 20, Width = 100 };
            _txtInput = new TextBox { Left = 130, Top = 18, Width = 250, Text = initialValue ?? string.Empty };

            var btnOk = new Button { Text = "Tamam", Left = 180, Top = 60, Width = 90, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Vazgec", Left = 280, Top = 60, Width = 90, DialogResult = DialogResult.Cancel };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtInput.Text))
                {
                    MessageBox.Show(this, "Deger bos olamaz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            Controls.AddRange(new Control[] { lbl, _txtInput, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}



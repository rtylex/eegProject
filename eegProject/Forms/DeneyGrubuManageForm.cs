using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using eegProject.Services;

namespace eegProject.Forms
{
    /// <summary>
    /// Deney gruplarını yönetmek için form
    /// NeuroIS araştırma gruplarını oluşturma, düzenleme, silme
    /// </summary>
    internal sealed class DeneyGrubuManageForm : Form
    {
        private readonly DeneyGrubuService _service = new DeneyGrubuService();
        private readonly BindingList<DeneyGrubu> _items = new BindingList<DeneyGrubu>();
        private readonly ListBox _listBox;

        public DeneyGrubuManageForm()
        {
            Text = "Deney Gruplari Yonetimi";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(500, 450);

            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DataSource = _items,
                DisplayMember = "GrupAdi",
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
                foreach (var item in data.OrderBy(g => g.GrupAdi))
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
            var dialog = new TextInputDialog("Yeni Deney Grubu", "Grup Adi:", "");
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                await _service.CreateAsync(dialog.InputValue);
                await LoadDataAsync();
                MessageBox.Show(this, "Deney grubu eklendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task EditItemAsync()
        {
            var selected = _listBox.SelectedItem as DeneyGrubu;
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir grup seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = new TextInputDialog("Deney Grubu Duzenle", "Grup Adi:", selected.GrupAdi);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                await _service.UpdateAsync(selected.DeneyGrubuID, dialog.InputValue);
                await LoadDataAsync();
                MessageBox.Show(this, "Deney grubu guncellendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task DeleteItemAsync()
        {
            var selected = _listBox.SelectedItem as DeneyGrubu;
            if (selected == null)
            {
                MessageBox.Show(this, "Lutfen bir grup seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usageCount = await _service.GetUsageCountAsync(selected.DeneyGrubuID);
            var message = usageCount > 0
                ? $"'{selected.GrupAdi}' {usageCount} kullanicida atanmistir. Pasif yapmak istiyor musunuz?"
                : $"'{selected.GrupAdi}' silmek istiyor musunuz?";

            var result = MessageBox.Show(this, message, "Silme Onayi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            try
            {
                await _service.DeleteAsync(selected.DeneyGrubuID);
                await LoadDataAsync();
                var msg = usageCount > 0 ? "Deney grubu pasif yapildi." : "Deney grubu silindi.";
                MessageBox.Show(this, msg, "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

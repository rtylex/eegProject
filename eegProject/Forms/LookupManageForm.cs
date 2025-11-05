using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace eegProject.Forms
{
    internal sealed class LookupManageForm : Form
    {
        private readonly string _itemName;
        private readonly BindingList<string> _items;
        private readonly ListBox _listBox;

        public IList<string> Values => _items.ToList();

        public LookupManageForm(string title, IEnumerable<string> initialValues, string itemName)
        {
            _itemName = itemName;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(360, 420);

            _items = new BindingList<string>(initialValues?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>());

            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DataSource = _items,
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
            btnAdd.Click += (sender, _) => AddItem();

            var btnEdit = new Button { Text = "Duzenle", Width = 100, Height = 36 };
            btnEdit.Click += (sender, _) => EditItem();

            var btnDelete = new Button { Text = "Sil", Width = 100, Height = 36 };
            btnDelete.Click += (sender, _) => DeleteItem();

            var btnClose = new Button { Text = "Kaydet", Width = 100, Height = 36, DialogResult = DialogResult.OK };

            panelButtons.Controls.Add(btnAdd);
            panelButtons.Controls.Add(btnEdit);
            panelButtons.Controls.Add(btnDelete);
            panelButtons.Controls.Add(btnClose);

            Controls.Add(_listBox);
            Controls.Add(panelButtons);

            AcceptButton = btnClose;
        }

        private void AddItem()
        {
            if (PromptValue("Yeni " + _itemName, string.Empty, out var value))
            {
                if (_items.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, _itemName + " mevcut", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _items.Add(value);
                SortItems();
            }
        }

        private void EditItem()
        {
            if (!TryGetSelected(out var currentValue))
            {
                return;
            }

            if (PromptValue(_itemName + " Duzenle", currentValue, out var value))
            {
                if (!string.Equals(value, currentValue, StringComparison.OrdinalIgnoreCase) &&
                    _items.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, _itemName + " mevcut", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var index = _items.IndexOf(currentValue);
                if (index >= 0)
                {
                    _items[index] = value;
                    SortItems();
                }
            }
        }

        private void DeleteItem()
        {
            if (!TryGetSelected(out var currentValue))
            {
                return;
            }

            var result = MessageBox.Show(this,
                currentValue + " degerini silmek istiyor musunuz?",
                "Silme Onayi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                _items.Remove(currentValue);
            }
        }

        private bool TryGetSelected(out string value)
        {
            value = _listBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show(this, "Lutfen bir deger seciniz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        private static bool PromptValue(string title, string initial, out string value)
        {
            value = null;
            using (var dialog = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(340, 140)
            })
            {
                var lbl = new Label { Text = "Deger", Left = 12, Top = 20, Width = 80 };
                var txt = new TextBox { Left = 100, Top = 18, Width = 220, Text = initial ?? string.Empty };

                var btnOk = new Button { Text = "Tamam", Left = 160, Top = 70, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Vazgec", Left = 245, Top = 70, Width = 75, DialogResult = DialogResult.Cancel };

                btnOk.Click += (sender, _) =>
                {
                    if (string.IsNullOrWhiteSpace(txt.Text))
                    {
                        MessageBox.Show(dialog, "Deger bos olamaz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        dialog.DialogResult = DialogResult.None;
                    }
                };

                dialog.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }

                value = txt.Text.Trim();
                return true;
            }
        }

        private void SortItems()
        {
            var sorted = _items.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
            _items.RaiseListChangedEvents = false;
            _items.Clear();
            foreach (var item in sorted)
            {
                _items.Add(item);
            }
            _items.RaiseListChangedEvents = true;
            _items.ResetBindings();
        }
    }
}

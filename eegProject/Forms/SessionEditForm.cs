using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace eegProject.Forms
{
    internal sealed class SessionEditForm : Form
    {
        private sealed class UserItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        private readonly ComboBox _cmbUser;
        private readonly ComboBox _cmbExperimentType;
        private readonly ComboBox _cmbTimeLabel;
        private readonly ComboBox _cmbOturumTipi;
        private readonly DateTimePicker _dtStart;
        private readonly DateTimePicker _dtEnd;
        private readonly TextBox _txtNotes;
        private readonly Oturum _existing;
        private readonly BindingList<UserItem> _users;

        public int SelectedUserId => (_cmbUser.SelectedItem as UserItem)?.Id ?? 0;
        public string SelectedExperimentType => _cmbExperimentType.Text.Trim();
        public string SelectedTimeLabel => _cmbTimeLabel.Text.Trim();
        public string SelectedOturumTipi => string.IsNullOrWhiteSpace(_cmbOturumTipi.Text) ? null : _cmbOturumTipi.Text.Trim();
        public DateTime? SelectedStart => _dtStart.Checked ? (DateTime?)_dtStart.Value : null;
        public DateTime? SelectedEnd => _dtEnd.Checked ? (DateTime?)_dtEnd.Value : null;
        public string Notes => string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

        public SessionEditForm(string title, IEnumerable<Kullanici> users, IEnumerable<string> experimentTypes, IEnumerable<string> timeLabels, Oturum existing = null)
        {
            _existing = existing;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 460);

            _users = new BindingList<UserItem>(users?
                .Select(u => new UserItem { Id = u.KullaniciID, Name = string.IsNullOrWhiteSpace(u.AdSoyad) ? "(Isimsiz)" : u.AdSoyad })
                .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<UserItem>());

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(12)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            for (var i = 0; i < 6; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblUser = new Label { Text = "Kullanici", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbUser = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = _users,
                DisplayMember = nameof(UserItem.Name),
                ValueMember = nameof(UserItem.Id)
            };

            var lblExperiment = new Label { Text = "Deney Turu", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbExperimentType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            _cmbExperimentType.Items.AddRange((experimentTypes ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray());

            var lblTime = new Label { Text = "Zaman Etiketi", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbTimeLabel = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            _cmbTimeLabel.Items.AddRange((timeLabels ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray());

            var lblOturumTipi = new Label { Text = "Oturum Tipi", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbOturumTipi = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbOturumTipi.Items.AddRange(new object[] { "", "Bazal", "Gorev" });
            _cmbOturumTipi.SelectedIndex = 0;

            var lblStart = new Label { Text = "Baslangic", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _dtStart = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowCheckBox = true,
                Checked = true,
                Value = DateTime.Now
            };

            var lblEnd = new Label { Text = "Bitis", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _dtEnd = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowCheckBox = true,
                Checked = false,
                Value = DateTime.Now
            };

            var lblNotes = new Label { Text = "Notlar", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            layout.Controls.Add(lblUser, 0, 0);
            layout.Controls.Add(_cmbUser, 1, 0);
            layout.Controls.Add(lblExperiment, 0, 1);
            layout.Controls.Add(_cmbExperimentType, 1, 1);
            layout.Controls.Add(lblTime, 0, 2);
            layout.Controls.Add(_cmbTimeLabel, 1, 2);
            layout.Controls.Add(lblOturumTipi, 0, 3);
            layout.Controls.Add(_cmbOturumTipi, 1, 3);
            layout.Controls.Add(lblStart, 0, 4);
            layout.Controls.Add(_dtStart, 1, 4);
            layout.Controls.Add(lblEnd, 0, 5);
            layout.Controls.Add(_dtEnd, 1, 5);
            layout.Controls.Add(lblNotes, 0, 6);
            layout.Controls.Add(_txtNotes, 1, 6);

            var panelButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Padding = new Padding(12, 6, 12, 12)
            };

            var btnCancel = new Button { Text = "Vazgec", DialogResult = DialogResult.Cancel, Width = 100, Height = 36 };
            var btnOk = new Button { Text = "Kaydet", Width = 100, Height = 36 };
            btnOk.Click += (sender, _) =>
            {
                if (!ValidateInput())
                {
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            panelButtons.Controls.Add(btnCancel);
            panelButtons.Controls.Add(btnOk);

            Controls.Add(layout);
            Controls.Add(panelButtons);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (_users.Count > 0)
            {
                if (existing != null)
                {
                    var match = _users.FirstOrDefault(u => u.Id == existing.KullaniciID);
                    _cmbUser.SelectedItem = match ?? _users.FirstOrDefault();
                }
                else
                {
                    _cmbUser.SelectedItem = _users.FirstOrDefault();
                }
            }
            else
            {
                _cmbUser.SelectedItem = null;
            }

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.DeneyTuru))
                {
                    _cmbExperimentType.Text = existing.DeneyTuru;
                }

                if (!string.IsNullOrWhiteSpace(existing.ZamanEtiketi))
                {
                    _cmbTimeLabel.Text = existing.ZamanEtiketi;
                }

                if (existing.KayitBaslangic.HasValue)
                {
                    _dtStart.Value = existing.KayitBaslangic.Value;
                    _dtStart.Checked = true;
                }
                else
                {
                    _dtStart.Checked = false;
                }

                if (existing.KayitBitis.HasValue)
                {
                    _dtEnd.Value = existing.KayitBitis.Value;
                    _dtEnd.Checked = true;
                }
                else
                {
                    _dtEnd.Checked = false;
                }

                if (!string.IsNullOrWhiteSpace(existing.Notlar))
                {
                    _txtNotes.Text = existing.Notlar;
                }

                if (!string.IsNullOrWhiteSpace(existing.OturumTipi))
                {
                    _cmbOturumTipi.SelectedItem = existing.OturumTipi;
                }
            }
        }

        public Oturum BuildSessionModel()
        {
            return new Oturum
            {
                OturumID = _existing?.OturumID ?? 0,
                KullaniciID = SelectedUserId,
                ZamanEtiketi = string.IsNullOrWhiteSpace(SelectedTimeLabel) ? null : SelectedTimeLabel,
                DeneyTuru = string.IsNullOrWhiteSpace(SelectedExperimentType) ? null : SelectedExperimentType,
                OturumTipi = SelectedOturumTipi,
                KayitBaslangic = SelectedStart,
                KayitBitis = SelectedEnd,
                Notlar = Notes
            };
        }

        private bool ValidateInput()
        {
            if (SelectedUserId <= 0)
            {
                MessageBox.Show(this, "Kullanici seciniz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedExperimentType))
            {
                MessageBox.Show(this, "Deney turu giriniz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedTimeLabel))
            {
                MessageBox.Show(this, "Zaman etiketi giriniz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_dtStart.Checked && _dtEnd.Checked && _dtEnd.Value < _dtStart.Value)
            {
                MessageBox.Show(this, "Bitis tarihi baslangictan once olamaz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SessionEditForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "SessionEditForm";
            this.Load += new System.EventHandler(this.SessionEditForm_Load);
            this.ResumeLayout(false);

        }

        private void SessionEditForm_Load(object sender, EventArgs e)
        {

        }
    }
}



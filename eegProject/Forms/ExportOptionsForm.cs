using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using eegProject;

namespace eegProject.Forms
{
    internal sealed class ExportOptionsForm : Form
    {
        private sealed class UserOption
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        private sealed class SessionOption
        {
            public int SessionId { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display;
        }

        private sealed class ExperimentOption
        {
            public string Display { get; set; }
            public string Value { get; set; }
            public override string ToString() => Display;
        }

        private sealed class TimeLabelOption
        {
            public string Display { get; set; }
            public string Value { get; set; }
            public override string ToString() => Display;
        }

        private readonly ComboBox _cmbUser;
        private readonly ComboBox _cmbScope;
        private readonly ComboBox _cmbSession;
        private readonly ComboBox _cmbExperimentType;
        private readonly CheckedListBox _lstTimeLabels;
        private readonly CheckBox _chkAllLabels;
        private readonly Button _btnOk;

        private readonly Dictionary<int, List<SessionOption>> _sessionsByUser;
        private readonly List<TimeLabelOption> _timeLabelOptions;

        public int SelectedUserId => (_cmbUser.SelectedItem as UserOption)?.Id ?? 0;

        public string SelectedUserName => (_cmbUser.SelectedItem as UserOption)?.Name ?? string.Empty;

        public string SelectedExperimentType => (_cmbExperimentType.SelectedItem as ExperimentOption)?.Value;

        public int? SelectedSessionId
        {
            get
            {
                if (_cmbScope.SelectedIndex != 1)
                {
                    return null;
                }

                var option = _cmbSession.SelectedItem as SessionOption;
                if (option == null || option.SessionId <= 0)
                {
                    return null;
                }

                return option.SessionId;
            }
        }

        public IReadOnlyList<string> SelectedTimeLabels
        {
            get
            {
                if (_chkAllLabels.Checked)
                {
                    return null;
                }

                return _lstTimeLabels.CheckedItems
                    .OfType<TimeLabelOption>()
                    .Select(o => o.Value)
                    .ToList();
            }
        }

        public ExportOptionsForm(
            IEnumerable<Kullanici> users,
            IEnumerable<ExportSessionDescriptor> sessions,
            IEnumerable<string> experimentTypes,
            IEnumerable<string> timeLabels)
        {
            Text = "Excel Aktarim Secenekleri";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(540, 520);

            var userOptions = (users ?? Enumerable.Empty<Kullanici>())
                .Select(u => new UserOption
                {
                    Id = u.KullaniciID,
                    Name = string.IsNullOrWhiteSpace(u.AdSoyad) ? $"Kullanici #{u.KullaniciID}" : u.AdSoyad.Trim()
                })
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _sessionsByUser = (sessions ?? Enumerable.Empty<ExportSessionDescriptor>())
                .GroupBy(s => s.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderBy(s => s.SessionId)
                        .Select(s => new SessionOption
                        {
                            SessionId = s.SessionId,
                            Display = string.IsNullOrWhiteSpace(s.DisplayName) ? $"Oturum #{s.SessionId}" : s.DisplayName
                        })
                        .ToList());

            var experimentOptions = new List<ExperimentOption>
            {
                new ExperimentOption { Display = "Tum Deney Turleri", Value = null }
            };

            experimentOptions.AddRange((experimentTypes ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Select(v => new ExperimentOption { Display = v, Value = v }));

            _timeLabelOptions = (timeLabels ?? Enumerable.Empty<string>())
                .Select(label => string.IsNullOrWhiteSpace(label) ? null : label.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(label => label ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(label => new TimeLabelOption
                {
                    Display = label ?? "Etiketsiz",
                    Value = label
                })
                .ToList();

            if (_timeLabelOptions.Count == 0)
            {
                _timeLabelOptions.Add(new TimeLabelOption { Display = "Etiketsiz", Value = null });
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(12)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblUser = new Label { Text = "Kullanici", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbUser = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = userOptions
            };
            _cmbUser.SelectedIndexChanged += (_, __) => RefreshSessionOptions();

            var lblScope = new Label { Text = "Veri Kapsami", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbScope = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = new[] { "Tum Oturumlar", "Belirli Oturum" }
            };
            _cmbScope.SelectedIndexChanged += (_, __) => UpdateScopeState();

            var lblSession = new Label { Text = "Oturum", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbSession = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            var lblExperiment = new Label { Text = "Deney Turu", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbExperimentType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = experimentOptions
            };

            _chkAllLabels = new CheckBox
            {
                Text = "Tum zaman etiketleri",
                Dock = DockStyle.Left,
                Checked = true
            };
            _chkAllLabels.CheckedChanged += (_, __) => UpdateLabelState();

            _lstTimeLabels = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Enabled = false
            };

            foreach (var option in _timeLabelOptions)
            {
                var index = _lstTimeLabels.Items.Add(option, true);
                _lstTimeLabels.SetItemChecked(index, true);
            }

            layout.Controls.Add(lblUser, 0, 0);
            layout.Controls.Add(_cmbUser, 1, 0);
            layout.Controls.Add(lblScope, 0, 1);
            layout.Controls.Add(_cmbScope, 1, 1);
            layout.Controls.Add(lblSession, 0, 2);
            layout.Controls.Add(_cmbSession, 1, 2);
            layout.Controls.Add(lblExperiment, 0, 3);
            layout.Controls.Add(_cmbExperimentType, 1, 3);
            layout.Controls.Add(_chkAllLabels, 1, 4);
            layout.Controls.Add(_lstTimeLabels, 1, 5);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 6, 12, 12)
            };

            var btnCancel = new Button { Text = "Vazgec", DialogResult = DialogResult.Cancel, Width = 100, Height = 36 };
            _btnOk = new Button { Text = "Olustur", Width = 100, Height = 36 };
            _btnOk.Click += (sender, _) =>
            {
                if (!ValidateInput())
                {
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(_btnOk);

            Controls.Add(layout);
            Controls.Add(buttons);

            AcceptButton = _btnOk;
            CancelButton = btnCancel;

            if (userOptions.Count > 0)
            {
                _cmbUser.SelectedIndex = 0;
            }
            else
            {
                _cmbUser.SelectedIndex = -1;
                _btnOk.Enabled = false;
            }

            _cmbScope.SelectedIndex = 0;
            _cmbExperimentType.SelectedIndex = 0;
            RefreshSessionOptions();
            UpdateScopeState();
            UpdateLabelState();
        }

        private void RefreshSessionOptions()
        {
            var userId = SelectedUserId;

            _cmbSession.BeginUpdate();
            _cmbSession.Items.Clear();
            _cmbSession.Items.Add(new SessionOption { SessionId = 0, Display = "Tum Oturumlar" });

            if (userId > 0 && _sessionsByUser.TryGetValue(userId, out var sessions))
            {
                foreach (var option in sessions)
                {
                    _cmbSession.Items.Add(option);
                }
            }

            _cmbSession.EndUpdate();
            _cmbSession.SelectedIndex = _cmbSession.Items.Count > 0 ? 0 : -1;
        }

        private void UpdateScopeState()
        {
            var singleSession = _cmbScope.SelectedIndex == 1;
            _cmbSession.Enabled = singleSession;
        }

        private void UpdateLabelState()
        {
            _lstTimeLabels.Enabled = !_chkAllLabels.Checked;
        }

        private bool ValidateInput()
        {
            if (SelectedUserId <= 0)
            {
                MessageBox.Show(this, "Lutfen bir kullanici seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_cmbScope.SelectedIndex == 1 && (_cmbSession.SelectedItem as SessionOption)?.SessionId <= 0)
            {
                MessageBox.Show(this, "Lutfen bir oturum seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!_chkAllLabels.Checked && _lstTimeLabels.CheckedItems.Count == 0)
            {
                MessageBox.Show(this, "En az bir zaman etiketi seciniz ya da tum etiketler secenegini isaretleyiniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
    }
}

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace eegProject.Forms
{
    internal sealed class UserEditForm : Form
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtEmail;
        private readonly ComboBox _cmbRole;
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtPasswordConfirm;
        private readonly bool _requirePassword;

        public string UserName => _txtName.Text.Trim();
        public string Email => string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim();
        public string Role => string.IsNullOrWhiteSpace(_cmbRole.SelectedItem?.ToString()) ? "Kullanici" : _cmbRole.SelectedItem.ToString();
        public string Password => _txtPassword.Text;

        public UserEditForm(string title, bool requirePassword, string[] roleOptions = null, Kullanici existingUser = null)
        {
            _requirePassword = requirePassword;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 300);

            roleOptions = roleOptions?.Length > 0 ? roleOptions : new[] { "Kullanici", "Admin" };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(12),
                AutoSize = false
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            for (var i = 0; i < 5; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 4 ? 60 : 40));
            }

            var lblName = new Label { Text = "Ad Soyad", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtName = new TextBox { Dock = DockStyle.Fill };

            var lblEmail = new Label { Text = "Email", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtEmail = new TextBox { Dock = DockStyle.Fill };

            var lblRole = new Label { Text = "Rol", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _cmbRole = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbRole.Items.AddRange(roleOptions.Cast<object>().ToArray());

            var lblPassword = new Label { Text = requirePassword ? "Parola" : "Parola (opsiyonel)", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

            var lblPasswordConfirm = new Label { Text = requirePassword ? "Parola Tekrar" : "Parola Tekrar", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            _txtPasswordConfirm = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Enabled = requirePassword };

            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(_txtName, 1, 0);

            layout.Controls.Add(lblEmail, 0, 1);
            layout.Controls.Add(_txtEmail, 1, 1);

            layout.Controls.Add(lblRole, 0, 2);
            layout.Controls.Add(_cmbRole, 1, 2);

            layout.Controls.Add(lblPassword, 0, 3);
            layout.Controls.Add(_txtPassword, 1, 3);

            layout.Controls.Add(lblPasswordConfirm, 0, 4);
            layout.Controls.Add(_txtPasswordConfirm, 1, 4);

            var panelButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Padding = new Padding(12, 6, 12, 12),
                AutoSize = true
            };

            var btnCancel = new Button { Text = "Vazgec", DialogResult = DialogResult.Cancel, Width = 90, Height = 32 };
            var btnOk = new Button { Text = "Kaydet", Width = 90, Height = 32 };
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

            if (existingUser != null)
            {
                _txtName.Text = existingUser.AdSoyad;
                _txtEmail.Text = existingUser.Email;
                _cmbRole.SelectedItem = roleOptions.FirstOrDefault(r => string.Equals(r, existingUser.Rol, StringComparison.OrdinalIgnoreCase)) ?? roleOptions.First();
            }
            else
            {
                _cmbRole.SelectedIndex = 0;
            }

            if (!_requirePassword)
            {
                _txtPasswordConfirm.Enabled = false;
                _txtPassword.TextChanged += (sender, _) =>
                {
                    var enableConfirm = !string.IsNullOrWhiteSpace(_txtPassword.Text);
                    _txtPasswordConfirm.Enabled = enableConfirm;
                };
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show(this, "Ad Soyad zorunlu", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_cmbRole.SelectedItem == null)
            {
                MessageBox.Show(this, "Rol seciniz", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var passwordValue = _txtPassword.Text;
            var confirmValue = _txtPasswordConfirm.Text;

            if (_requirePassword || !string.IsNullOrWhiteSpace(passwordValue))
            {
                if (passwordValue.Length < 6)
                {
                    MessageBox.Show(this, "Parola en az 6 karakter olmali", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (_requirePassword || !string.IsNullOrWhiteSpace(confirmValue))
                {
                    if (!string.Equals(passwordValue, confirmValue, StringComparison.Ordinal))
                    {
                        MessageBox.Show(this, "Parolalar uyusmuyor", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // UserEditForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "UserEditForm";
            this.Load += new System.EventHandler(this.UserEditForm_Load);
            this.ResumeLayout(false);

        }

        private void UserEditForm_Load(object sender, EventArgs e)
        {

        }
    }
}

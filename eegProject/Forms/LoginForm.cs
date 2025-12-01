using System;
using System.Configuration;
using System.Data.Entity;
using System.Windows.Forms;
using eegProject.Security;
using eegProject.Services;

namespace eegProject.Forms
{
    public partial class LoginForm : Form
    {
        private readonly UserService _userService = new UserService();
        private readonly AuditLogService _auditLogService = new AuditLogService();
        
        public int? LoggedInUserId { get; private set; }
        public string LoggedInUserRole { get; private set; }
        public string LoggedInUserName { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var email = txtEmail.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show(this, "Lutfen email adresinizi giriniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(this, "Lutfen parolanizi giriniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                btnCancel.Enabled = false;
                Cursor = Cursors.WaitCursor;

                // Önce Kullanici tablosunda ara
                var user = await _userService.GetByEmailAsync(email);
                if (user != null && PasswordHasher.VerifyPassword(password, user.SifreHash))
                {
                    // Kullanıcı girişi başarılı
                    LoggedInUserId = user.KullaniciID;
                    LoggedInUserRole = user.Rol;
                    LoggedInUserName = user.AdSoyad;

                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                // Kullanıcı bulunamadı, Yönetici tablosunda ara
                using (var context = Services.DbContextFactory.Create())
                {
                    var yonetici = await System.Data.Entity.QueryableExtensions.FirstOrDefaultAsync(
                        context.Yonetici,
                        y => y.Email == email
                    );

                    if (yonetici != null && PasswordHasher.VerifyPassword(password, yonetici.SifreHash))
                    {
                        // Yönetici girişi başarılı
                        LoggedInUserId = yonetici.YoneticiID;
                        LoggedInUserRole = "Yonetici"; // Yönetici tablosundan geldiği için "Yonetici" rolü
                        LoggedInUserName = yonetici.KullaniciAdi;

                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }
                }

                // Her iki tabloda da bulunamadı veya parola yanlış
                await _auditLogService.LogAsync("GirisBasarisiz", $"Basarisiz giris denemesi: {email}", null, email, "Warning");
                MessageBox.Show(this, "Email veya parola hatali.", "Giris Hatasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "\n\nDetay: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        message += "\n\nAlt Detay: " + ex.InnerException.InnerException.Message;
                    }
                }

                await _auditLogService.LogAsync("GirisHatasi", $"Giris hatasi: {message}", null, email, "Error");
                MessageBox.Show(this, "Giris sirasinda hata olustu: " + message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnCancel.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            txtEmail.Focus();
        }

        private void panelMain_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}


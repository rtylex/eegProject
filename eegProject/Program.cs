using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using eegProject.Forms;

namespace eegProject
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Önce login formu göster
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // Başarılı giriş - ana formu aç
                    Application.Run(new Form1(
                        loginForm.LoggedInUserId.Value,
                        loginForm.LoggedInUserRole,
                        loginForm.LoggedInUserName
                    ));
                }
                // Giriş iptal edildi veya başarısız - uygulama kapanır
            }
        }
    }
}

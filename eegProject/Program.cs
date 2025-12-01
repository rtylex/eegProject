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

            bool keepRunning = true;

            while (keepRunning)
            {
                keepRunning = false; // Varsayılan olarak döngüden çık

                // Önce login formu göster
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        // Başarılı giriş - ana formu aç
                        Form1 mainForm = new Form1(
                            loginForm.LoggedInUserId.Value,
                            loginForm.LoggedInUserRole,
                            loginForm.LoggedInUserName
                        );

                        Application.Run(mainForm);

                        // Form kapandığında çıkış isteği var mı kontrol et
                        if (mainForm.UserRequestedLogout)
                        {
                            keepRunning = true; // Döngüye devam et (Login ekranına dön)
                        }
                    }
                    // Giriş iptal edildiyse loop zaten false, uygulama kapanır
                }
            }
        }
    }
}

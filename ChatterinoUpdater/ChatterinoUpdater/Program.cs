using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ChatterinoUpdater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(String[] args)
        {
#if !DEBUG
            try
#endif
            {
                var baseDir = AppContext.BaseDirectory;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (args.Length == 0)
                {
                    MessageBox.Show("The updater can not be ran manually.", "Chatterino Updater");
                    return;
                }

                Directory.SetCurrentDirectory(baseDir);

                var mainForm = new MainForm();

                if (args.Contains("restart"))
                {
                    mainForm.SuccessCallback = () =>
                    {
                        try
                        {
                            var parentDir = Directory.GetParent(baseDir)!.FullName;
                            var exePath = Path.Combine(parentDir, "chatterino.exe");

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    };
                }

                Application.Run(mainForm);
            }
#if !DEBUG
            catch (Exception exc)
            {
                HandleDankError(exc);
            }
#endif
        }

#if !DEBUG
        private static void HandleDankError(Exception exc)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

                MessageBox.Show("An unexpected error has occured. You might have to redownload the chatterino installer.\n\n" + exc.Message);
            }
            catch { }
        }
#endif
    }
}

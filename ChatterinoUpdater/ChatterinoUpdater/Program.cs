using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (args.Length == 0)
                {
                    MessageBox.Show("The updater can not be ran manually.", "Chatterino Updater");
                    return;
                }

                Directory.SetCurrentDirectory(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName);

                var mainForm = new MainForm();

                if (args.Contains("restart"))
                {
                    mainForm.SuccessCallback = () =>
                    {
                        try
                        {
                            var parentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.FullName;

                            Process.Start(Path.Combine(parentDir, "chatterino.exe"));
                        }
                        catch { }
                    };
                }

                Application.Run(mainForm);
            }
#if !DEBUG
            catch (Exception exc)
            {
                handleDankError(exc);
            }
#endif
        }

        private static void handleDankError(Exception exc)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

                MessageBox.Show("An unexpected error has occured. You might have to redownload the chatterino installer.\n\n" + exc.Message);
            }
            catch { }
        }
    }
}

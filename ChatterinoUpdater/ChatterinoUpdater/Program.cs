using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ChatterinoUpdater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
#if !DEBUG
            try
#endif
            {
                var baseDir = AppContext.BaseDirectory;

                if (args.Length == 0)
                {
                    Console.WriteLine("The updater can not be ran manually.");
                    return;
                }

                Directory.SetCurrentDirectory(baseDir);

                if (RunUpdater())
                {
                    if (args.Contains("restart"))
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
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

                    MessageBox.Show("An unexpected error has occured. You might have to redownload the chatterino installer.\n\n" + ex.Message);
                }
                catch { }
            }
#endif
        }

        private static bool RunUpdater()
        {
            var updater = new Updater();
            return updater.StartInstall();
        }
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace ChatterinoUpdater
{
    public partial class MainForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action? SuccessCallback { get; set; }

        private readonly string _ownDirectory;
        private int _fileCount;
        private int _currentFile = 1;
        private readonly ManualResetEvent _continueEvent = new(false);

        public MainForm()
        {
            InitializeComponent();


#if NET6_0_OR_GREATER
            string? exePath = Environment.ProcessPath;
#else
            string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
#endif
            try
            {
                if (!string.IsNullOrEmpty(exePath))
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch { }

            labelStatus.Text = string.Empty;
            buttonRetry.Visible = false;

            buttonRetry.Click += (s, e) => _continueEvent.Set();
            buttonCancel.Click += (s, e) => Close();

            _ownDirectory = AppContext.BaseDirectory;

            StartInstall();
        }

        private void StartInstall()
        {
            var baseDir = AppContext.BaseDirectory;
            var parentDir = Directory.GetParent(baseDir)!.FullName;
            var miscDir = Path.Combine(parentDir, "Misc");

            Task.Run(() =>
            {
                string zipPath = Path.Combine(miscDir, "update.zip");

                try
                {
                    using (var fileStream = File.OpenRead(zipPath))
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        _fileCount = zipArchive.Entries.Count(x => !string.IsNullOrEmpty(x.Name));

                        ProcessZipFile(zipArchive);
                    }

                    File.Delete(zipPath);
                }
                catch
                {
                    this.Invoke(() =>
                    {
                        labelStatus.Text = "Error";
                        rtbError.Text = "Update package not found.";
                        buttonCancel.Text = "Close";
                    });
                    return;
                }

                this.Invoke(() =>
                {
                    labelStatus.Text = "Success!";
                    buttonCancel.Text = "OK";

                    SuccessCallback?.Invoke();
                    Close();
                });
            });
        }

        private void ProcessZipFile(ZipArchive archive)
        {
            foreach (var entry in archive.Entries)
            {
                while (true)
                {
                    this.Invoke(() =>
                    {
                        rtbError.Text = "";
                        buttonRetry.Hide();
                    });

                    try
                    {
                        this.Invoke(() =>
                        {
                            labelStatus.Text = $@"Installing file {_currentFile} of {_fileCount}";
                        });

                        ProcessEntry(entry);

                        break;
                    }
                    catch (Exception exc)
                    {
                        this.Invoke(() =>
                        {
                            buttonRetry.Show();

                            var message = exc.Message;
                            message += "\n\nIf you have the browser extension enabled you might need to close chrome.";

                            rtbError.Text = message;
                        });

                        Console.WriteLine(exc);
                    }

                    _continueEvent.WaitOne();
                    _continueEvent.Reset();
                }
            }
        }

        private void ProcessEntry(ZipArchiveEntry entry)
        {
            // skip directories
            if (string.IsNullOrEmpty(entry.Name))
                return;

            // skip if same name as this directory
            var entryName = Regex.Replace(entry.FullName, "^Chatterino2/", "");

            if (entryName.StartsWith(_ownDirectory))
                return;

            // extract the file
            var outPath = Path.Combine("..", entryName);

            // create directory if needed
            var directoryName = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            // write the file
            using (var input = entry.Open())
            using (var output = File.Create(outPath))
            {
                input.CopyTo(output);
            }

            _currentFile++;
        }
    }
}

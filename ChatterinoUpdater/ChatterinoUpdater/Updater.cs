using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChatterinoUpdater
{
    public class Updater
    {
        public Action? SuccessCallback { get; set; }
        private readonly string _ownDirectory;
        private int _fileCount;
        private int _currentFile = 1;

        public Updater()
        {
            _ownDirectory = AppContext.BaseDirectory;
        }

        public bool StartInstall()
        {
            var baseDir = AppContext.BaseDirectory;
            var parentDir = Directory.GetParent(baseDir)!.FullName;
            var miscDir = Path.Combine(parentDir, "Misc");
            string zipPath = Path.Combine(miscDir, "update.zip");

            try
            {
                using (var fileStream = File.OpenRead(zipPath))
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    _fileCount = zipArchive.Entries.Count(x => !string.IsNullOrEmpty(x.Name));
                    var retry = true;
                    while (retry)
                    {
                        try
                        {
                            ProcessZipFile(zipArchive);
                        }
                        catch
                        {
                            Console.Write("Do you want to retry or close? (R/c): ");
                            var line = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line) && line.Trim().Equals("c", StringComparison.OrdinalIgnoreCase))
                            {
                                retry = false;
                            }
                        }
                    }
                }
                File.Delete(zipPath);
            }
            catch
            {
                Console.WriteLine("Error: Update package not found.\nPress any key to close.");
                Console.ReadKey();
                return false;
            }
            return true;
        }

        private void ProcessZipFile(ZipArchive archive)
        {
            foreach (var entry in archive.Entries)
            {
                Console.Write("\r");
                try
                {
                    Console.WriteLine($@"Installing file {_currentFile} of {_fileCount}");

                    ProcessEntry(entry);

                    break;
                }
                catch (Exception exc)
                {
                    var message = exc.Message;
                    message += "\n\nIf you have the browser extension enabled you might need to close chrome.";
                    Console.WriteLine(message);
                    Console.WriteLine(exc);
                    throw; // Pass down exception without changing line number
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

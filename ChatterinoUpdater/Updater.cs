using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChatterinoUpdater
{
    public class Updater
    {
        private readonly string _ownDirectory;

        public Updater()
        {
            _ownDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public bool StartInstall()
        {
            var parentDir = Directory.GetParent(_ownDirectory)!.FullName;
            var miscDir = Path.Combine(parentDir, "Misc");
            var zipPath = Path.Combine(miscDir, "update.zip");

            try
            {
                using (var fileStream = File.OpenRead(zipPath))
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    var retry = true;
                    while (retry)
                    {
                        try
                        {
                            ProcessZipFile(zipArchive);
                            retry = false;
                            Console.WriteLine();
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
            var entries = archive.Entries.Where(x => !string.IsNullOrEmpty(x.Name));
            var fileCount = entries.Count();
            var currentFile = 1;
            foreach (var entry in entries)
            {
                try
                {
                    Console.Write($"\rInstalling file {currentFile} of {fileCount}");
                    ProcessEntry(entry);
                    currentFile++;
                }
                catch (Exception exc)
                {
                    var message = exc.Message;
                    message += "\n\nIf you have the browser extension enabled you might need to close chrome.";
                    Console.WriteLine(message);
                    Console.WriteLine(exc);
                    throw; // Pass down exception without changing things like line number
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

            if (entry.Name.Equals("ChatterinoUpdater.exe", StringComparison.OrdinalIgnoreCase))
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
            using var input = entry.Open();
            using var output = File.Create(outPath);
            input.CopyTo(output);
        }
    }
}

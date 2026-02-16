using ChatterinoUpdater.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChatterinoUpdater;

public class Updater
{
    private readonly string _ownDirectory;

    public Updater(string ownDirectory)
    {
        _ownDirectory = ownDirectory;
    }

    public bool StartInstall(string zipPath)
    {
        if (!TryOpenZip(zipPath, out var zipArchive))
            return false;

        bool success;

        using (zipArchive)
        {
            success = InstallWithRetry(zipArchive);
        }

        if (success)
        {
            File.Delete(zipPath);
        }

        return true;
    }

    private static bool TryOpenZip(string path, [NotNullWhen(true)] out ZipArchive? zipArchive)
    {
        zipArchive = null;

        try
        {
            var stream = File.OpenRead(path);
            try
            {
                zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
            return true;
        }
        catch
        {
            NativeUI.ShowError("Update package not found", $"Could not find:\n{path}");
            return false;
        }
    }

    private bool InstallWithRetry(ZipArchive zipArchive)
    {
        var retry = true;

        while (retry)
        {
            try
            {
                ProcessZipFile(zipArchive);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                retry = NativeUI.ShowRetryCancel(
                    "An error occurred during the update",
                    $"{ex.Message}\n\nIf you have the browser extension enabled, you might need to close Chrome.");
            }
        }

        return false;
    }

    private void ProcessZipFile(ZipArchive archive)
    {
        var entries = archive.Entries.Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
        var fileCount = entries.Count;
        var cancelled = false;
        Exception? error = null;

        NativeUI.ShowProgressDialog(
            "Installing update...",
            fileCount,
            (reportProgress, isCancelled) =>
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    if (isCancelled())
                    {
                        cancelled = true;
                        return;
                    }

                    var entry = entries[i];
                    var current = i + 1;

                    reportProgress(current, $"Installing file {current} of {fileCount}: {entry.Name}");

                    try
                    {
                        ProcessEntry(entry);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        return;
                    }
                }
            });

        if (cancelled)
            throw new OperationCanceledException("Update cancelled by user.");

        if (error != null)
            throw error;
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

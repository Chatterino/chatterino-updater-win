using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ChatterinoUpdater.Interop;

namespace ChatterinoUpdater;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (!TryParseArgs(args, out var zipPath, out var restart))
        {
            NativeUI.ShowError("Zip package file wasn't provided", "The updater can not be ran manually.");
            return;
        }

        Run(zipPath, restart);
    }

    private static bool TryParseArgs(string[] args, [NotNullWhen(true)] out string? zipPath, out bool restart)
    {
        zipPath = null;
        restart = false;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "restart", StringComparison.OrdinalIgnoreCase))
            {
                restart = true;
            }
            else if (zipPath == null)
            {
                zipPath = arg;
            }

            if (restart && zipPath != null)
            {
                return true;
            }
        }

        return zipPath != null;
    }

    private static void Run(string zipPath, bool restart)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            Directory.SetCurrentDirectory(baseDir);

            if (new Updater(baseDir).StartInstall(zipPath) && restart)
            {
                try
                {
                    var parentDir = Directory.GetParent(baseDir)!.FullName;
                    var exePath = Path.Combine(parentDir, "chatterino.exe");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        WorkingDirectory = parentDir
                    });
                }
                catch { }
            }
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            try
            {
                NativeUI.ShowError("An unexpected error has occured", "You might have to redownload the chatterino installer.\n\n" + ex.Message);
            }
            catch { }
        }
    }
}

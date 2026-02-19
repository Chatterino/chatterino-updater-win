using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace ChatterinoUpdater.Interop;

internal static partial class NativeUI
{
    private const string Title = "Chatterino Updater";

    private const uint MB_RETRYCANCEL = 0x00000005;
    private const uint MB_ICONERROR = 0x00000010;
    private const int IDRETRY = 4;

    private const uint PROGDLG_AUTOTIME = 0x00000002;

    private const uint CLSCTX_INPROC_SERVER = 0x00000001;

    private static readonly Guid CLSID_ProgressDialog = new("F8383852-FCD3-11d1-A6B9-006097DF5BD4");
    private static readonly Guid IID_IProgressDialog = new(ComGuids.ProgressDialog);

    /// <summary>
    /// Shows a simple error dialog.
    /// </summary>
    public static void ShowError(string instruction, string content)
    {
        NativeMethods.MessageBox(nint.Zero, $"{instruction}\n\n{content}", Title, MB_ICONERROR);
    }

    /// <summary>
    /// Shows a Retry/Cancel dialog. Returns true if the user clicked Retry.
    /// </summary>
    public static bool ShowRetryCancel(string instruction, string content)
    {
        var result = NativeMethods.MessageBox(nint.Zero, $"{instruction}\n\n{content}", Title, MB_ICONERROR | MB_RETRYCANCEL);
        return result == IDRETRY;
    }

    /// <summary>
    /// Shows a progress dialog that runs work inline on the calling thread.
    /// The dialog runs its own message loop internally; the caller polls for cancellation.
    /// </summary>
    public static unsafe void ShowProgressDialog(string instruction, int totalSteps, Action<Action<int, string>, Func<bool>> doWork)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSteps);

        NativeMethods.CoCreateInstance(in CLSID_ProgressDialog, nint.Zero, CLSCTX_INPROC_SERVER, in IID_IProgressDialog, out var ppv);

        var dialog = ComInterfaceMarshaller<IProgressDialog>.ConvertToManaged((void*)ppv)!;
        dialog.SetTitle(Title);
        dialog.StartProgressDialog(nint.Zero, nint.Zero, PROGDLG_AUTOTIME, nint.Zero);
        dialog.SetLine(1, instruction, 0, nint.Zero);
        dialog.SetProgress(0, (uint)totalSteps);

        // The Shell creates the dialog window on a background STA thread and uses a timer
        // to delay ShowWindow, avoiding a flash for fast operations. Wait until the window
        // is actually visible before starting work; otherwise fast operations complete and
        // call StopProgressDialog before the window ever appears.
        var oleWindow = (IOleWindow)dialog;
        nint hwnd;
        do
        {
            Thread.Sleep(10);
            oleWindow.GetWindow(out hwnd);
        }
        while (hwnd == nint.Zero || NativeMethods.IsWindowVisible(hwnd) == 0);

        doWork(
            (step, text) =>
            {
                dialog.SetProgress((uint)step, (uint)totalSteps);
                dialog.SetLine(2, text, 0, nint.Zero);
            },
            () => dialog.HasUserCancelled() != 0
        );

        dialog.StopProgressDialog();
    }
}

using System;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ChatterinoUpdater.Interop;

internal static class NativeUI
{
    private const string Title = "Chatterino Updater";

    /// <summary>
    /// Shows a simple error dialog.
    /// </summary>
    public static unsafe void ShowError(string instruction, string content)
    {
        TaskDialogHelper.ShowSimple(
            Title,
            instruction,
            content,
            (IntPtr)(void*)PInvoke.TD_ERROR_ICON,
            TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CLOSE_BUTTON);
    }

    /// <summary>
    /// Shows a Retry/Cancel dialog. Returns true if the user clicked Retry.
    /// </summary>
    public static unsafe bool ShowRetryCancel(string instruction, string content)
    {
        var button = TaskDialogHelper.ShowSimple(
            Title,
            instruction,
            content,
            (IntPtr)(void*)PInvoke.TD_ERROR_ICON,
            TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_RETRY_BUTTON | TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON);

        return button == (int)MESSAGEBOX_RESULT.IDRETRY;
    }

    /// <summary>
    /// Shows a progress dialog that runs work on a background thread.
    /// The dialog closes automatically when the work completes or the user cancels.
    /// </summary>
    public static void ShowProgressDialog(string instruction, int totalSteps,
        Action<Action<int, string>, Func<bool>> doWork)
    {
        TaskDialogHelper.ShowProgress(Title, instruction, totalSteps, doWork);
    }
}

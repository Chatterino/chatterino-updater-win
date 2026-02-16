using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ChatterinoUpdater.Interop;

internal static class TaskDialogHelper
{
    public static unsafe int ShowSimple(string title, string instruction, string content,
        IntPtr iconResource, TASKDIALOG_COMMON_BUTTON_FLAGS commonButtons)
    {
        fixed (char* pTitle = title)
        fixed (char* pInstruction = instruction)
        fixed (char* pContent = content)
        {
            var config = new TASKDIALOGCONFIG
            {
                cbSize = (uint)sizeof(TASKDIALOGCONFIG),
                dwFlags = TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,
                dwCommonButtons = commonButtons,
                pszWindowTitle = pTitle,
                Anonymous1 = new TASKDIALOGCONFIG._Anonymous1_e__Union
                {
                    hMainIcon = (HICON)iconResource
                },
                pszMainInstruction = pInstruction,
                pszContent = pContent,
            };

            PInvoke.TaskDialogIndirect(in config, out var button, out _, out _).ThrowOnFailure();

            return button;
        }
    }

    public static unsafe void ShowProgress(
        string title,
        string instruction,
        int totalSteps,
        Action<Action<int, string>, Func<bool>> doWork)
    {
        var state = new ProgressState
        {
            TotalSteps = totalSteps,
            DoWork = doWork,
        };

        var handle = GCHandle.Alloc(state);

        try
        {
            fixed (char* pTitle = title)
            fixed (char* pInstruction = instruction)
            fixed (char* pLoading = "Preparing...")
            {
                var config = new TASKDIALOGCONFIG
                {
                    cbSize = (uint)sizeof(TASKDIALOGCONFIG),
                    hwndParent = HWND.Null,
                    dwFlags = TASKDIALOG_FLAGS.TDF_SHOW_PROGRESS_BAR |
                              TASKDIALOG_FLAGS.TDF_CALLBACK_TIMER |
                              TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,
                    dwCommonButtons = TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON,
                    pszWindowTitle = pTitle,
                    pszMainInstruction = pInstruction,
                    pszContent = pLoading,
                    pfCallback = &ProgressCallback,
                    lpCallbackData = GCHandle.ToIntPtr(handle)
                };

                PInvoke.TaskDialogIndirect(in config, out _, out _, out _).ThrowOnFailure();
            }
        }
        finally
        {
            if (handle.IsAllocated) handle.Free();
        }

        state.Worker?.Join();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static HRESULT ProgressCallback(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam, nint refData)
    {
        var handle = GCHandle.FromIntPtr(refData);
        if (!handle.IsAllocated || handle.Target is not ProgressState state)
        {
            return HRESULT.S_OK;
        }

        var notification = (TASKDIALOG_NOTIFICATIONS)msg;

        if (notification == TASKDIALOG_NOTIFICATIONS.TDN_CREATED)
        {
            state.DialogHwnd = hwnd;

            // Set Progress Range
            var range = (LPARAM)((state.TotalSteps & 0xFFFF) << 16);
            PInvoke.SendMessage(hwnd, (uint)TASKDIALOG_MESSAGES.TDM_SET_PROGRESS_BAR_RANGE, 0, range);

            var worker = new Thread(ProgressBarWorker) { IsBackground = true };

            worker.Start();
            state.Worker = worker;
        }
        else if (notification == TASKDIALOG_NOTIFICATIONS.TDN_BUTTON_CLICKED)
        {
            var buttonId = (int)wParam.Value;
            if (buttonId == (int)MESSAGEBOX_RESULT.IDCANCEL && !state.IsWorkDone)
            {
                state.Cancel();
                return HRESULT.S_FALSE; // Prevent close until worker finishes
            }
        }

        return HRESULT.S_OK;

        void ProgressBarWorker()
        {
            try
            {
                state.DoWork((step, text) =>
                {
                    var h = state.DialogHwnd;
                    if (h == HWND.Null) return;

                    unsafe
                    {
                        PInvoke.SendMessage(h, (uint)TASKDIALOG_MESSAGES.TDM_SET_PROGRESS_BAR_POS, (uint)step, 0);

                        fixed (char* pText = text)
                        {
                            PInvoke.SendMessage(h, (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT, (int)TASKDIALOG_ELEMENTS.TDE_CONTENT, (IntPtr)pText);
                        }
                    }
                }, () => state.IsCancelled);
            }
            finally
            {
                state.SetWorkDone();
                var h2 = state.DialogHwnd;
                if (h2 != HWND.Null)
                {
                    // Click `Cancel` (which handles the close)
                    PInvoke.SendMessage(h2, (uint)TASKDIALOG_MESSAGES.TDM_CLICK_BUTTON, (int)MESSAGEBOX_RESULT.IDCANCEL, 0);
                }
            }
        }
    }

    private sealed class ProgressState
    {
        private volatile bool _cancelled;
        private volatile bool _workDone;

        public required int TotalSteps { get; init; }
        public required Action<Action<int, string>, Func<bool>> DoWork { get; init; }

        public HWND DialogHwnd { get; set; }
        public Thread? Worker { get; set; }

        public void Cancel() => _cancelled = true;
        public bool IsCancelled => _cancelled;

        public void SetWorkDone() => _workDone = true;
        public bool IsWorkDone => _workDone;
    }
}

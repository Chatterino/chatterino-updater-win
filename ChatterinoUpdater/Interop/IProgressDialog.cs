using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ChatterinoUpdater.Interop;

[GeneratedComInterface]
[Guid(ComGuids.ProgressDialog)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IProgressDialog
{
    void StartProgressDialog(nint hwndParent, nint punkEnableModless, uint dwFlags, nint pvReserved);
    void StopProgressDialog();
    void SetTitle([MarshalUsing(typeof(Utf16StringMarshaller))] string pwzTitle);
    void SetAnimation(nint hInstAnimation, uint idAnimation);
    [PreserveSig] int HasUserCancelled();
    void SetProgress(uint dwCompleted, uint dwTotal);
    void SetProgress64(ulong ullCompleted, ulong ullTotal);
    void SetLine(uint dwLineNum, [MarshalUsing(typeof(Utf16StringMarshaller))] string pwzString, int fCompactPath, nint pvReserved);
    void SetCancelMsg([MarshalUsing(typeof(Utf16StringMarshaller))] string pwzCancelMsg, nint pvReserved);
    void Timer(uint dwTimerAction, nint pvReserved);
}
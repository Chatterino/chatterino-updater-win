using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ChatterinoUpdater.Interop;

[GeneratedComInterface]
[Guid(ComGuids.OleWindow)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IOleWindow
{
    void GetWindow(out nint phwnd);
    void ContextSensitiveHelp(int fEnterMode);
}
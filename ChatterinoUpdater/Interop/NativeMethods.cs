using System;
using System.Runtime.InteropServices;

namespace ChatterinoUpdater.Interop;

internal partial class NativeMethods
{
    private const string User32 = "user32.dll";
    private const string Ole32 = "ole32.dll";

    [LibraryImport(User32, EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(nint hwnd, string text, string caption, uint type);

    [LibraryImport(User32)]
    internal static partial int IsWindowVisible(nint hWnd);

    [LibraryImport(Ole32)]
    internal static partial int CoCreateInstance(in Guid rclsid, nint pUnkOuter, uint dwClsContext, in Guid riid, out nint ppv);
}

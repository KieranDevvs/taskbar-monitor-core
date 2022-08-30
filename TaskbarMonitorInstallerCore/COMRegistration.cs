using System;
using System.Runtime.InteropServices;

namespace TaskbarMonitorInstallerCore;

public class COMRegistration
{
    // All COM DLLs must export the DllRegisterServer()
    // and the DllUnregisterServer() APIs for self-registration/unregistration.
    // They both have the same signature and so only one
    // delegate is required.
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint DllRegUnRegAPI();

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string strLibraryName);

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int FreeLibrary(IntPtr hModule);

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

    public static void RegSvr32(string path, bool register = true)
    {
        // Load the DLL.
        var hModuleDLL = LoadLibrary(path); ;
        if (hModuleDLL == IntPtr.Zero)
        {
            Console.WriteLine("Unable to load DLL : {0:S}.", path);
            return;
        }

        // Obtain the required exported API.
        var procedureName = register ? "DllRegisterServer" : "DllUnregisterServer";
        var pExportedFunction = GetProcAddress(hModuleDLL, procedureName);

        if (pExportedFunction == IntPtr.Zero)
        {
            Console.WriteLine("Unable to get required API from DLL.");
            return;
        }

        // Obtain the delegate from the exported function, whether it be
        // DllRegisterServer() or DllUnregisterServer().
        if(Marshal.GetDelegateForFunctionPointer(pExportedFunction, typeof(DllRegUnRegAPI)) is not DllRegUnRegAPI pDelegateRegUnReg)
        {
            throw new Exception($"Could not get delegate for function pointer: {typeof(DllRegUnRegAPI).Name}");
        }

        // Invoke the delegate.
        uint hResult = pDelegateRegUnReg();

        if (hResult == 0)
        {
            var action = register ? "Registration" : "Unregistration";
            Console.WriteLine($"{action} Successful.");
        }
        else
        {
            Console.WriteLine($"Error occurred : {hResult:X}.");
        }

        if (FreeLibrary(hModuleDLL) == 0)
        {
            Console.WriteLine("Error freeing library.");
        }
    }
}

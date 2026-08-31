using RimTelligence.CustomEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RimTelligence.Memory
{

    public static class NativeDependencyLoader
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static readonly HashSet<string> Loaded =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        public static bool EnsureNativeLoaded(string dllName)
        {
            if (string.IsNullOrEmpty(dllName))
                return false;

            if (Loaded.Contains(dllName))
                return true;

            string dllPath;
            try
            {
                dllPath = ModPaths.Resolve(Path.Combine("Native", dllName));
            }
            catch (InvalidOperationException exception)
            {
                ModLogger.Error("NativeDependencyLoader: " + exception.Message);
                return false;
            }

            if (!File.Exists(dllPath))
            {
                ModLogger.Message("Native DLL NOT FOUND at: " + dllPath);
                return false;
            }

            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                ModLogger.Error("LoadLibrary failed for " + dllPath + ", Win32 error: " + error);
                return false;
            }

            Loaded.Add(dllName);
            ModLogger.Message(dllName + " loaded successfully from " + dllPath);
            return true;
        }
    }
}

using RimTelligence.CustomEvents;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Verse;

namespace RimTelligence.Memory
{
    public static class OnnxBootstrap
    {
        private static bool loaded = false;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public static void EnsureNativeLoaded()
        {
            if (loaded) return;

            string assembliesDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string modRoot = Path.GetFullPath(Path.Combine(assembliesDir, ".."));
            string dllPath = Path.Combine(modRoot, "Native", "onnxruntime.dll");

            if (!File.Exists(dllPath))
            {
                ModLogger.Message("onnxruntime.dll NOT FOUND at: " + dllPath);
                return;
            }

            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                ModLogger.Message("LoadLibrary failed for onnxruntime.dll, Win32 error: " + err);
            }
            else
            {
                loaded = true;
                ModLogger.Message("onnxruntime.dll loaded successfully.");
            }
        }
    }
}
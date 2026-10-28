using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RimTelligence.Source.Memory.Embedding
{
    /// <summary>
    /// Loads the onnxruntime native library for the current OS/architecture from the mod's
    /// Native folder and maps it for Mono's P/Invoke resolver.
    /// </summary>
    public static class OnnxBootstrap
    {
        enum Os { Windows, Linux, MacOS }

        sealed class Candidate
        {
            public readonly string Rid;
            public readonly string MainLib;
            public readonly string[] ExtraLibs;
            public Candidate(string rid, string mainLib, params string[] extraLibs)
            {
                Rid = rid;
                MainLib = mainLib;
                ExtraLibs = extraLibs ?? Array.Empty<string>();
            }
        }

        static readonly object Gate = new object();
        static bool _initialized;

        public static Action<string> Log = delegate { };

        public static void Init(string modRootDir)
        {
            lock (Gate)
            {
                if (_initialized) return;

                Os os = DetectOs();
                string nativeRoot = Path.Combine(modRootDir, "Native");
                var errors = new List<string>();
                bool loaded = false;

                foreach (Candidate c in Candidates(os))
                {
                    string ridDir = Path.Combine(nativeRoot, c.Rid);
                    string mainLibPath = Path.Combine(ridDir, c.MainLib);

                    if (!File.Exists(mainLibPath))
                    {
                        errors.Add("not found: " + mainLibPath);
                        continue;
                    }

                    // Preload dependencies first (e.g., providers_shared)
                    foreach (string extra in c.ExtraLibs)
                    {
                        string extraPath = Path.Combine(ridDir, extra);
                        if (File.Exists(extraPath))
                        {
                            string err;
                            Open(os, extraPath, out err);
                        }
                    }

                    // Load the main ONNX runtime library
                    string loadError;
                    IntPtr handle = Open(os, mainLibPath, out loadError);
                    if (handle == IntPtr.Zero)
                    {
                        errors.Add(mainLibPath + " -> " + loadError);
                        continue;
                    }

                    Log("ONNX native library loaded: " + mainLibPath);

                    // Register with Mono's P/Invoke subsystem on non-Windows
                    if (os != Os.Windows)
                    {
                        RegisterMonoDllMap("onnxruntime", mainLibPath);
                    }

                    loaded = true;
                    break;
                }

                if (!loaded)
                {
                    throw new InvalidOperationException(
                        "Could not load onnxruntime native libraries for " + os + ". Tried:\n  " +
                        string.Join("\n  ", errors.ToArray()));
                }

                _initialized = true;
            }
        }

        private static void RegisterMonoDllMap(string dll, string targetPath)
        {
            try
            {
                // mono_dllmap_insert(IntPtr.Zero, dll, null, targetPath, null) tells Mono's JIT/PInvoke
                // resolver globally to route [DllImport("onnxruntime")] directly to targetPath.
                MonoNative.mono_dllmap_insert(IntPtr.Zero, dll, null, targetPath, null);
                Log("Registered Mono dllmap: " + dll + " -> " + targetPath);
            }
            catch (Exception ex)
            {
                Log("Failed to call mono_dllmap_insert: " + ex.Message);
            }
        }

        static class MonoNative
        {
            [DllImport("__Internal", EntryPoint = "mono_dllmap_insert", CallingConvention = CallingConvention.Cdecl)]
            public static extern void mono_dllmap_insert(IntPtr assembly, string dll, string func, string target, string targetFunc);
        }

        static Os DetectOs()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return Os.Windows;
                case PlatformID.MacOSX:
                    return Os.MacOS;
                default:
                    return Directory.Exists("/System/Library/CoreServices") ? Os.MacOS : Os.Linux;
            }
        }

        static IEnumerable<Candidate> Candidates(Os os)
        {
            bool is64 = IntPtr.Size == 8;
            switch (os)
            {
                case Os.Windows:
                    if (is64)
                        yield return new Candidate("win-x64", "onnxruntime.dll", "onnxruntime_providers_shared.dll");
                    else
                        yield return new Candidate("win-arm64", "onnxruntime.dll", "onnxruntime_providers_shared.dll");
                    break;
                case Os.Linux:
                    if (is64)
                    {
                        yield return new Candidate("linux-x64", "libonnxruntime.so", "libonnxruntime_providers_shared.so");
                        yield return new Candidate("linux-arm64", "libonnxruntime.so", "libonnxruntime_providers_shared.so");
                    }
                    break;
                case Os.MacOS:
                    yield return new Candidate("osx-arm64", "libonnxruntime.dylib");
                    break;
            }
        }

        const int RTLD_NOW = 2;
        const int RTLD_GLOBAL_LINUX = 256;
        const int RTLD_GLOBAL_MACOS = 8;

        static IntPtr Open(Os os, string path, out string error)
        {
            error = null;
            try
            {
                switch (os)
                {
                    case Os.Windows:
                        {
                            IntPtr h = Win32.LoadLibraryW(path);
                            if (h == IntPtr.Zero)
                                error = "LoadLibrary failed, Win32 error " + Marshal.GetLastWin32Error();
                            return h;
                        }
                    case Os.Linux:
                        {
                            IntPtr h = LibDl.dlopen(path, RTLD_NOW | RTLD_GLOBAL_LINUX);
                            if (h == IntPtr.Zero)
                                error = Marshal.PtrToStringAnsi(LibDl.dlerror());
                            return h;
                        }
                    default:
                        {
                            IntPtr h = LibSystem.dlopen(path, RTLD_NOW | RTLD_GLOBAL_MACOS);
                            if (h == IntPtr.Zero)
                                error = Marshal.PtrToStringAnsi(LibSystem.dlerror());
                            return h;
                        }
                }
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return IntPtr.Zero;
            }
        }

        static class Win32
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryW(string path);
        }

        static class LibDl
        {
            [DllImport("libdl.so.2")] public static extern IntPtr dlopen(string path, int flags);
            [DllImport("libdl.so.2")] public static extern IntPtr dlerror();
        }

        static class LibSystem
        {
            [DllImport("/usr/lib/libSystem.B.dylib")] public static extern IntPtr dlopen(string path, int flags);
            [DllImport("/usr/lib/libSystem.B.dylib")] public static extern IntPtr dlerror();
        }
    }
}

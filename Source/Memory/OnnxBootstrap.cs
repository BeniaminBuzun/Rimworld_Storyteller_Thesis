using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RimTelligence.Memory
{
    /// <summary>
    /// Loads the right onnxruntime native library for the current OS/arch from the mod's
    /// Native folder and pre-loads it before Microsoft.ML.OnnxRuntime tries to find it.
    ///
    /// Expected layout inside the mod folder:
    ///   Native/win-x64/onnxruntime.dll
    ///   Native/win-x64/onnxruntime_providers_shared.dll
    ///   Native/win-arm64/onnxruntime.dll
    ///   Native/win-arm64/onnxruntime_providers_shared.dll
    ///   Native/linux-x64/libonnxruntime.so
    ///   Native/linux-x64/libonnxruntime_providers_shared.so
    ///   Native/linux-arm64/libonnxruntime.so
    ///   Native/linux-arm64/libonnxruntime_providers_shared.so
    ///   Native/osx-arm64/libonnxruntime.dylib
    ///
    /// Usage (call before constructing any InferenceSession):
    ///   OnnxBootstrap.Init(Content.RootDir);
    /// </summary>
    public static class OnnxBootstrap
    {
        enum Os { Windows, Linux, MacOS }

        sealed class Candidate
        {
            public readonly string Rid;
            public readonly string[] Files;
            public Candidate(string rid, params string[] files) { Rid = rid; Files = files; }
        }

        static readonly object Gate = new object();
        static bool _initialized;

        /// <summary>Optional logger; defaults to no-op.</summary>
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
                    // Every file in the candidate set must exist for this RID to be usable.
                    bool allExist = true;
                    foreach (string file in c.Files)
                    {
                        string path = Path.Combine(nativeRoot, c.Rid, file);
                        if (!File.Exists(path))
                        {
                            errors.Add("not found: " + path);
                            allExist = false;
                        }
                    }
                    if (!allExist) continue;

                    // Load each file; providers_shared must be loaded first so the main
                    // library can resolve symbols from it at link time.
                    bool allLoaded = true;
                    foreach (string file in c.Files)
                    {
                        string path = Path.Combine(nativeRoot, c.Rid, file);
                        string err;
                        IntPtr h = Open(os, path, out err);
                        if (h == IntPtr.Zero)
                        {
                            errors.Add(path + " -> " + err);
                            allLoaded = false;
                            break;
                        }
                        Log("ONNX native library loaded: " + path);
                    }

                    if (allLoaded) { loaded = true; break; }
                }

                if (!loaded)
                    throw new InvalidOperationException(
                        "Could not load onnxruntime native libraries for " + os + ". Tried:\n  " +
                        string.Join("\n  ", errors.ToArray()));

                _initialized = true;
            }
        }

        // ---- OS / arch detection ------------------------------------------------

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
                    // providers_shared must be listed first so it is loaded before onnxruntime.dll
                    if (is64)
                        yield return new Candidate("win-x64",
                            "onnxruntime_providers_shared.dll", "onnxruntime.dll");
                    else
                        yield return new Candidate("win-arm64",
                            "onnxruntime_providers_shared.dll", "onnxruntime.dll");
                    break;
                case Os.Linux:
                    if (is64)
                    {
                        yield return new Candidate("linux-x64",
                            "libonnxruntime_providers_shared.so", "libonnxruntime.so");
                        yield return new Candidate("linux-arm64",
                            "libonnxruntime_providers_shared.so", "libonnxruntime.so");
                    }
                    break;
                case Os.MacOS:
                    // osx-arm64 ships without a separate providers_shared
                    yield return new Candidate("osx-arm64", "libonnxruntime.dylib");
                    break;
            }
        }

        // ---- Native loading -----------------------------------------------------

        const int RTLD_NOW = 2;

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
                            IntPtr h = LibDl.dlopen(path, RTLD_NOW);
                            if (h == IntPtr.Zero)
                                error = Marshal.PtrToStringAnsi(LibDl.dlerror());
                            return h;
                        }
                    default:
                        {
                            IntPtr h = LibSystem.dlopen(path, RTLD_NOW);
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

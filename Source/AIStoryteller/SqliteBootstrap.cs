using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RimTelligence
{
    /// <summary>
    /// Loads the right e_sqlite3 native library for the current OS from the mod folder
    /// and registers it with SQLitePCLRaw. Call Init once, before opening any SqliteConnection.
    /// </summary>
    public static class SqliteBootstrap
    {
        enum Os { Windows, Linux, MacOS }

        sealed class Candidate
        {
            public readonly string Rid;
            public readonly string File;
            public Candidate(string rid, string file) { Rid = rid; File = file; }
        }

        static readonly object Gate = new object();
        static bool _initialized;

        /// <summary>Optional logger; defaults to no-op so this works outside RimWorld too.</summary>
        public static Action<string> Log = delegate { };

        public static void Init(string modRootDir)
        {
            lock (Gate)
            {
                if (_initialized) return;

                Os os = DetectOs();
                string nativeRoot = Path.Combine(modRootDir, "Native");
                var errors = new List<string>();
                IntPtr handle = IntPtr.Zero;

                foreach (Candidate c in Candidates(os))
                {
                    string path = Path.Combine(Path.Combine(nativeRoot, c.Rid), c.File);
                    if (!File.Exists(path))
                    {
                        errors.Add("not found: " + path);
                        continue;
                    }

                    string err;
                    handle = Open(os, path, out err);
                    if (handle != IntPtr.Zero)
                    {
                        Log("SQLite native library loaded: " + path);
                        break;
                    }
                    errors.Add(path + " -> " + err);
                }

                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        "Could not load a native SQLite library for " + os + ". Tried:\n  " +
                        string.Join("\n  ", errors.ToArray()));
                }

                // API names below are from SQLitePCLRaw 2.1.x; verify against your package version.
                SQLite3Provider_dynamic_cdecl.Setup("e_sqlite3", new Resolver(os, handle));
                raw.SetProvider(new SQLite3Provider_dynamic_cdecl());

                _initialized = true;
            }
        }

        // ---- OS / arch detection -------------------------------------------------

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
                    // Mono reports both Linux and macOS as Unix.
                    return Directory.Exists("/System/Library/CoreServices") ? Os.MacOS : Os.Linux;
            }
        }

        // Ordered by preference. On macOS a wrong-architecture dylib simply fails to load,
        // so we fall through to the next one instead of having to detect Rosetta/arm64.
        static IEnumerable<Candidate> Candidates(Os os)
        {
            bool is64 = IntPtr.Size == 8;
            switch (os)
            {
                case Os.Windows:
                    yield return new Candidate(is64 ? "win-x64" : "win-x86", "e_sqlite3.dll");
                    break;
                case Os.Linux:
                    if (is64)
                    {
                        yield return new Candidate("linux-x64", "libe_sqlite3.so");
                        yield return new Candidate("linux-arm64", "libe_sqlite3.so");
                    }
                    break;
                case Os.MacOS:
                    yield return new Candidate("osx-arm64", "libe_sqlite3.dylib");
                    yield return new Candidate("osx-x64", "libe_sqlite3.dylib");
                    break;
            }
        }

        // ---- Native loading --------------------------------------------------------

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

        const int RTLD_NOW = 2; // same value on Linux and macOS

        sealed class Resolver : IGetFunctionPointer
        {
            readonly Os _os;
            readonly IntPtr _handle;
            public Resolver(Os os, IntPtr handle) { _os = os; _handle = handle; }

            public IntPtr GetFunctionPointer(string name)
            {
                switch (_os)
                {
                    case Os.Windows: return Win32.GetProcAddress(_handle, name);
                    case Os.Linux: return LibDl.dlsym(_handle, name);
                    default: return LibSystem.dlsym(_handle, name);
                }
            }
        }

        static class Win32
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryW(string path);

            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
            public static extern IntPtr GetProcAddress(IntPtr module, string name);
        }

        static class LibDl
        {
            [DllImport("libdl.so.2")] public static extern IntPtr dlopen(string path, int flags);
            [DllImport("libdl.so.2")] public static extern IntPtr dlsym(IntPtr handle, string symbol);
            [DllImport("libdl.so.2")] public static extern IntPtr dlerror();
        }

        static class LibSystem
        {
            [DllImport("/usr/lib/libSystem.B.dylib")] public static extern IntPtr dlopen(string path, int flags);
            [DllImport("/usr/lib/libSystem.B.dylib")] public static extern IntPtr dlsym(IntPtr handle, string symbol);
            [DllImport("/usr/lib/libSystem.B.dylib")] public static extern IntPtr dlerror();
        }
    }
}
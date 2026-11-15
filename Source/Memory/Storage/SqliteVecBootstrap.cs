using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RimTelligence.Source.Memory.Storage
{
    /// <summary>
    /// Loads the sqlite-vec native extension (vec0.dll / vec0.so / vec0.dylib) for the
    /// current OS from the mod's Native folder. Must be called after SqliteBootstrap.Init
    /// so that the base SQLite library is already loaded.
    /// 
    /// The extension is loaded via SQL "SELECT load_extension(@path, 'sqlite3_vec_init')"
    /// on each new connection. This class pre-validates the file exists and stores the
    /// resolved path.
    /// </summary>
    public static class SqliteVecBootstrap
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
        static string _extensionPath;

        /// <summary>The resolved native path to the vec0 extension library.</summary>
        public static string ExtensionPath
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("SqliteVecBootstrap.Init has not been called.");
                return _extensionPath;
            }
        }

        public static void Init(string modRootDir)
        {
            lock (Gate)
            {
                if (_initialized) return;

                Os os = DetectOs();
                string nativeRoot = Path.Combine(modRootDir, "Native");
                var errors = new List<string>();

                foreach (Candidate c in Candidates(os))
                {
                    string path = Path.Combine(Path.Combine(nativeRoot, c.Rid), c.File);
                    if (!File.Exists(path))
                    {
                        errors.Add("not found: " + path);
                        continue;
                    }

                    _extensionPath = path;
                    ModLogger.Message("sqlite-vec extension located: " + path);
                    _initialized = true;
                    return;
                }

                throw new InvalidOperationException(
                    "Could not find sqlite-vec native extension for " + os + ". Tried:\n  " +
                    string.Join("\n  ", errors.ToArray()));
            }
        }

        /// <summary>
        /// Loads the sqlite-vec extension into an already-opened SqliteConnection.
        /// Call this immediately after conn.Open().
        /// </summary>
        public static void LoadInto(Microsoft.Data.Sqlite.SqliteConnection conn)
        {
            conn.LoadExtension(_extensionPath, "sqlite3_vec_init");
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
                    yield return new Candidate(is64 ? "win-x64" : "win-x86", "vec0.dll");
                    break;
                case Os.Linux:
                    if (is64)
                    {
                        yield return new Candidate("linux-x64", "vec0.so");
                        yield return new Candidate("linux-arm64", "vec0.so");
                    }
                    break;
                case Os.MacOS:
                    yield return new Candidate("osx-arm64", "vec0.dylib");
                    yield return new Candidate("osx-x64", "vec0.dylib");
                    break;
            }
        }
    }
}

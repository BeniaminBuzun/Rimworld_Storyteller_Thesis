using System;
using System.IO;

namespace RimTelligence
{

    public static class ModPaths
    {
        private static string _root;

        public static string Root => _root;

        public static bool IsInitialized => _root != null;

        public static void Initialize(string modRoot)
        {
            if (string.IsNullOrEmpty(modRoot))
            {
                ModLogger.Error("ModPaths.Initialize received an empty mod root.");
                return;
            }

            string normalized = Path.GetFullPath(modRoot);
            if (_root != null)
            {
                if (!string.Equals(_root, normalized, StringComparison.OrdinalIgnoreCase))
                    ModLogger.Error("ModPaths.Initialize called twice with different roots: '" +
                        _root + "' vs '" + normalized + "'.");
                return;
            }

            _root = normalized;
        }

        public static string Resolve(string relativePath)
        {
            if (_root == null)
                throw new InvalidOperationException(
                    "ModPaths is not initialized. The mod constructor must call ModPaths.Initialize(content.RootDir) before paths are resolved.");

            return Path.GetFullPath(Path.Combine(_root, relativePath));
        }
    }
}

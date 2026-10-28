using System;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace RimTelligence.Patches
{
    [HarmonyPatch(typeof(Root), nameof(Root.Update))]
    public static class Patch_Root_Update
    {
        public static void Postfix()
        {
            ModLogger.DrainQueue();
        }
    }

    [HarmonyPatch(typeof(EditWindow_Log), "DoMessagesListing")]
    public static class Patch_EditWindow_Log_DoMessagesListing
    {
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception is InvalidOperationException)
            {
                // Suppress transient collection modification exception caused by background logging
                return null;
            }
            return __exception;
        }
    }
}

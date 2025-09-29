using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_HE;


public static class CompHackableExtensions
{
    public static void ResetHackProgress(this CompHackable comp)
    {
        if (comp == null)
            return;

        try
        {
            Type type = typeof(CompHackable);

            var fProgress = AccessTools.Field(type, "progress");
            fProgress?.SetValue(comp, 0f);

            var fHacked = AccessTools.Field(type, "hacked");
            fHacked?.SetValue(comp, false);

            var fLastUserSpeed = AccessTools.Field(type, "lastUserSpeed");
            fLastUserSpeed?.SetValue(comp, 1f);

            var fLastHackTick = AccessTools.Field(type, "lastHackTick");
            fLastHackTick?.SetValue(comp, -1);

            var fLastUser = AccessTools.Field(type, "lastUser");
            fLastUser?.SetValue(comp, null);

            var fProgressLastLockout = AccessTools.Field(type, "progressLastLockout");
            fProgressLastLockout?.SetValue(comp, 0f);

            var fLockedOutUntilTick = AccessTools.Field(type, "lockedOutUntilTick");
            fLockedOutUntilTick?.SetValue(comp, -1);

            var fLockedOutPermanently = AccessTools.Field(type, "lockedOutPermanently");
            fLockedOutPermanently?.SetValue(comp, false);

            var fSentLetter = AccessTools.Field(type, "sentLetter");
            fSentLetter?.SetValue(comp, false);

            var fAutohack = AccessTools.Field(type, "autohack");
            fAutohack?.SetValue(comp, false);

            if (comp.parent != null && comp.parent.Spawned)
                comp.parent.DirtyMapMesh(comp.parent.Map);

        }
        catch (Exception ex)
        {
            Log.Error($"ResetHackProgress failed for CompHackable on {comp?.parent}: {ex}");
        }
    }
}

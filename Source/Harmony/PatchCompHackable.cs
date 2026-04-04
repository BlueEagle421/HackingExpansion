using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_HE;

[HarmonyPatch(typeof(CompHackable), nameof(CompHackable.Hack))]
public static class Patch_CompHackable_Hack
{
    private static readonly ConditionalWeakTable<CompHackable, CompDataSource> _cache = new();

    static void Postfix(CompHackable __instance, float amount, Pawn hacker = null)
    {
        if (__instance == null)
            return;

        if (!_cache.TryGetValue(__instance, out var compDataSource))
        {
            compDataSource = __instance.parent.GetComp<CompDataSource>();

            if (compDataSource == null)
                return;

            _cache.Add(__instance, compDataSource);
        }

        compDataSource.Hack(amount, hacker);
    }
}

[HarmonyPatch(typeof(CompHackable), "ProcessHacked")]
public static class Patch_CompHackable_ProcessHack
{
    static void Postfix(CompHackable __instance, Pawn hacker, bool suppressMessages)
    {
        if (__instance == null)
            return;

        if (!__instance.parent.TryGetComp(out CompDataSource compDataSource))
            return;

        compDataSource.ProcessHacked(hacker, suppressMessages);
    }
}

[HarmonyPatch(typeof(CompHackable), nameof(CompHackable.CanHackNow), [typeof(Pawn)])]
public static class Patch_CompHackable_CanHackNow
{
    static void Postfix(CompHackable __instance, ref AcceptanceReport __result, Pawn pawn)
    {
        if (HackValidationUtility.TryApplyCommonChecks(__instance, pawn, ref __result))
            return;

        if (__result.Reason != null &&
            __result.Reason == "NoPath".Translate().CapitalizeFirst() &&
            pawn.CanHackRemotely())
        {
            __result = true;
        }
    }
}

[HarmonyPatch]
public static class Patch_CompHackable_ValidateHacker
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CompHackable), "ValidateHacker", [typeof(LocalTargetInfo)]);
    }

    static void Postfix(CompHackable __instance, ref AcceptanceReport __result, LocalTargetInfo target)
    {
        if (target.Thing is not Pawn pawn)
            return;

        if (HackValidationUtility.TryApplyCommonChecks(__instance, pawn, ref __result))
            return;

        if (__result.Reason != null &&
            __result.Reason == "NoPath".Translate() &&
            pawn.CanHackRemotely())
        {
            __result = true;
        }
    }
}

public static class HackValidationUtility
{
    public static bool TryApplyCommonChecks(CompHackable comp, Pawn pawn, ref AcceptanceReport result)
    {
        if (comp?.parent?.Faction != null && pawn?.Faction != null &&
            comp.parent.Faction == pawn.Faction)
        {
            result = "USH_HE_BelongsToHacker".Translate();
            return true;
        }

        if ((comp is CompTurretHackable || comp is CompMechanoidHackable) && !pawn.IsHacker())
        {
            result = "USH_HE_MissingCyberlink".Translate();
            return true;
        }

        if (comp.IsHacked)
        {
            result = "USH_HE_AlreadyHacked".Translate();
            return true;
        }

        if (pawn.CanHackRemotely() && !JobDriver_RemoteHack.TryFindRemoteHackCell(pawn, comp.parent, out _))
        {
            result = "NoPath".Translate();
            return true;
        }

        return false;
    }
}
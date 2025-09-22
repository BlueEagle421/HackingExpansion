using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_HE
{
    [HarmonyPatch(typeof(CompHackable), nameof(CompHackable.Hack))]
    public static class Patch_CompHackable_Hack
    {
        private static readonly ConditionalWeakTable<CompHackable, CompDataSource> _cache = [];

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
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_HE;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
{
    [HarmonyPostfix]
    public static void Postfix(bool hotReload = false)
    {
        foreach (var entry in ThingDefGenerator_ExecData.ImpliedHediffDefs(hotReload))
            DefGenerator.AddImpliedDef(entry, hotReload);

        foreach (var entry in ThingDefGenerator_ExecData.ImpliedThingDefs(hotReload))
            DefGenerator.AddImpliedDef(entry, hotReload);
    }
}

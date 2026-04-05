using HarmonyLib;
using Verse;

namespace USH_HE;

[HarmonyPatch(typeof(Designation), nameof(Designation.DesignationDraw))]
public static class Patch_Designation_Draw
{
    public static bool Prefix(Designation __instance)
    {
        if (__instance.def == USH_DefOf.USH_RipData &&
            __instance.target.HasThing &&
            __instance.target.Thing.Fogged())
            return false;

        return true;
    }
}
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_HE;

[HarmonyPatch(typeof(DefOfHelper), nameof(DefOfHelper.RebindAllDefOfs))]
public static class Patch_DefOfHelper_RebindAllDefOfs
{
    private static readonly HashSet<string> _omittedDefNames = [];

    static void Postfix(bool earlyTryMode)
    {
        if (earlyTryMode)
            return;

        try
        {
            PatchAllDefs();
        }
        catch (Exception ex)
        {
            Log.Warning($"[Hacking Expansion] unexpected error in RebindAllDefOfs postfix. The overclock feature is disabled: {ex}");
        }

        if (!_omittedDefNames.NullOrEmpty())
            Log.Message("[Hacking Expansion] Thing defs omitted as data sources: " + string.Join(", ", _omittedDefNames));
    }

    private static void PatchAllDefs()
    {
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            try
            {
                if (ShouldBeDataSource(def))
                    (def.comps ??= []).Add(new CompProperties_DataSourceProtected());
            }
            catch
            {
                _omittedDefNames.Add(def.defName);
            }
        }
    }

    private static bool ShouldBeDataSource(ThingDef def)
    {
        if (!def.IsBuildingArtificial)
            return false;

        if (!def.HasComp<CompHackable>())
            return false;

        return true;
    }

}
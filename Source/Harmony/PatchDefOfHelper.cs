using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

[HarmonyPatch(typeof(DefOfHelper), nameof(DefOfHelper.RebindAllDefOfs))]
public static class Patch_DefOfHelper_RebindAllDefOfs
{
    private static readonly HashSet<string> _omittedDefNames = [];

    private const float TICKS_PER_WORK = 60;

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
            Log.Warning($"[Hacking Expansion] unexpected error in RebindAllDefOfs postfix. Hackables patch failed: {ex}");
        }

        if (!_omittedDefNames.NullOrEmpty())
            Log.Message("[Hacking Expansion] Defs omitted while patching hackables: " + string.Join(", ", _omittedDefNames));
    }

    private static void PatchAllDefs()
    {
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            try
            {
                if (ShouldBeDataSource(def))
                {
                    (def.comps ??= []).Add(new CompProperties_DataSourceProtected());
                }

                if (ShouldTurretBeHackable(def))
                {
                    (def.comps ??= []).Add(TurretPropsToAdd(def));
                    (def.comps ??= []).Add(new CompProperties_DataSourceProtected());
                }
            }
            catch
            {
                _omittedDefNames.Add(def.defName);
            }
        }

        foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            try
            {
                if (ShouldMechBeHackable(kindDef))
                {
                    (kindDef.race.comps ??= []).Add(MechPropsToAdd(kindDef));
                    (kindDef.race.comps ??= []).Add(new CompProperties_DataSourceProtected());
                }
            }
            catch
            {
                _omittedDefNames.Add(kindDef.defName);
            }
        }
    }


    private static bool ShouldBeDataSource(ThingDef def)
    {
        if (!def.HasComp<CompHackable>())
            return false;

        return true;
    }

    private static bool ShouldTurretBeHackable(ThingDef thingDef)
    {
        if (!HE_Mod.Settings.EnableTurretsHacking.Value)
            return false;

        if (thingDef.building?.turretGunDef == null)
            return false;

        if (thingDef.HasComp<CompMannable>())
            return false;

        if (thingDef.HasComp<CompInteractable>())
            return false;

        if (thingDef.HasComp<CompHackable>())
            return false;

        bool hasPower = thingDef.HasComp<CompPowerTrader>();

        var compStunnable = thingDef.GetCompProperties<CompProperties_Stunnable>();
        bool empStunnable = compStunnable?.affectedDamageDefs?.Any(d => d == DamageDefOf.EMP) == true;

        return hasPower && empStunnable;
    }

    private static bool ShouldMechBeHackable(PawnKindDef kindDef)
    {
        var raceProps = kindDef.race.race;

        if (!HE_Mod.Settings.EnableMechHacking.Value)
            return false;

        if (raceProps == null)
            return false;

        if (kindDef.race.HasComp<CompHackable>())
            return false;

        if (!raceProps.IsMechanoid && !raceProps.IsDrone)
            return false;

        return true;
    }

    private static CompProperties_TurretHackable TurretPropsToAdd(ThingDef thingDef)
        => new()
        {
            defence = GetTurretDefence(thingDef) * TICKS_PER_WORK,
            notHackedInspectString = "USH_HE_HackToDisable".Translate(),
            effectHacking = USH_DefOf.HackingTerminal
        };

    private static float GetTurretDefence(ThingDef thingDef)
    {
        float cost = thingDef.CostList.Sum(x => x.count * x.thingDef.BaseMarketValue);
        cost += thingDef.CostStuffCount * 2; //assuming cheap resource

        cost /= 4; //balancing

        //rounded for readability
        if (cost > 1000)
            return ((int)Mathf.Round(cost / 100.0f)) * 100;

        return ((int)Mathf.Round(cost / 10.0f)) * 10;
    }

    private static CompProperties_MechanoidHackable MechPropsToAdd(PawnKindDef kindDef)
        => new()
        {
            defence = GetMechDefence(kindDef) * TICKS_PER_WORK,
            notHackedInspectString = "USH_HE_HackToDisable".Translate(),
            effectHacking = USH_DefOf.HackingTerminal
        };

    private static float GetMechDefence(PawnKindDef kindDef)
        => kindDef.combatPower * kindDef.race.race.baseBodySize;
}
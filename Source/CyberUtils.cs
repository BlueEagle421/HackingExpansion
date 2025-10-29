using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

public static class CyberUtils
{
    private static List<HediffDef> _cachedCyberlinkHediffs;

    public static List<HediffDef> AllCyberlinkHediffDefs
    {
        get
        {
            _cachedCyberlinkHediffs ??= [.. DefDatabase<HediffDef>.AllDefsListForReading
                .Where(x => typeof(Hediff_Cyberlink).IsAssignableFrom(x.hediffClass))];

            return _cachedCyberlinkHediffs;
        }
    }
    private static List<ThingDef> _cachedCyberDatas;
    public static List<ThingDef> AllCyberDataDefs
    {
        get
        {
            _cachedCyberDatas ??= [.. DefDatabase<ThingDef>.AllDefsListForReading
                .Where(x => x.HasModExtension<RippableExtension>())];

            return _cachedCyberDatas;
        }
    }

    public static Color HackColor
        => Color.green;

    public static Color HackColorTransparent
        => HackColor.ToTransparent(0.35f);

    public static bool HasAnyHediffDef(this Pawn p, List<HediffDef> defs, out Hediff hediff)
    {
        hediff = null;
        if (defs == null || defs.Count == 0) return false;
        if (p?.health == null) return false;

        var hediffSet = p.health.hediffSet;
        if (hediffSet == null) return false;

        foreach (var def in defs)
            if (hediffSet.TryGetHediff(def, out hediff))
                return true;

        return false;
    }

    public static AcceptanceReport CanHack(this Pawn hacker, LocalTargetInfo t)
    {
        if (t.Thing is null)
            return false;

        return CanHack(hacker, t.Thing);
    }

    public static AcceptanceReport CanHack(this Pawn hacker, Thing t)
    {
        if (!t.TryGetComp(out CompHackable compHackable))
            return false;

        return CanHack(hacker, compHackable);
    }

    public static AcceptanceReport CanHack(this Pawn hacker, CompHackable compHackable)
    {
        if (compHackable == null)
            return false;

        if (compHackable.IsHacked)
            return "USH_HE_AlreadyHacked".Translate();

        var compReport = compHackable.CanHackNow(hacker);

        if (compReport.Reason != "NoPath".Translate().CapitalizeFirst())
            return compReport;

        return true;
    }

    public static HacksetDef GetHacksetDef(this CompHackable compHackable)
    {
        var defs = DefDatabase<HacksetDef>.AllDefsListForReading
            .Where(d => compHackable.Props.defence > d.minDefense);

        if (defs.Count() == 0)
            return null;

        return defs.RandomElementByWeight(def => def.weight);
    }

    public static bool IsHacker(this Pawn p)
        => p.HasAnyHediffDef(AllCyberlinkHediffDefs, out _);

    public static float GetRemoteHackRadius(this Pawn p)
        => p.GetStatValue(USH_DefOf.USH_RemoteHackingDistance);

    public static bool CanHackRemotely(this Pawn p)
        => !Mathf.Approximately(p.GetRemoteHackRadius(), 0f);

    public static float GetProgress(this CompHackable hackable)
        => hackable.ProgressPercent * hackable.defence;

    public static Building_Cyberpod GetHoldingCyberpod(this Pawn p)
    {
        if (p.holdingOwner.Owner is Building_Cyberpod pod)
            return pod;

        return null;
    }

    public static bool TryGetHoldingCyberpod(this Pawn p, out Building_Cyberpod pod)
    {
        pod = p?.holdingOwner?.Owner as Building_Cyberpod;
        return pod != null;
    }

    public static void MakeHackingOutcomeEffect(Thing t, string content)
    {
        USH_DefOf.USH_HackingOutcome.SpawnMaintained(t.Position, t.Map);

        float fadeTicks = 4f;
        MoteMaker.ThrowText(t.DrawPos, t.Map, content.Colorize(Color.red), fadeTicks);
    }

    public static IList<float> PercentChancesByWeight<T>(this IEnumerable<T> source, Func<T, float> weightSelector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));

        var list = source as IList<T> ?? [.. source];
        int count = list.Count;
        var chances = new float[count];

        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            float weight = weightSelector(list[i]);

            chances[i] = weight;
            total += weight;
        }

        if (total <= 0f)
        {
            for (int i = 0; i < count; i++) chances[i] = 0f;
            return chances;
        }

        for (int i = 0; i < count; i++)
            chances[i] = chances[i] / total;

        return chances;
    }

    public static bool IsUnlocked(this ResearchPrerequisitesExtension ext)
    {
        if (ext == null)
            return true;

        if (ext.researchPrerequisites == null)
            return true;

        if (!ext.researchPrerequisites.Any())
            return true;

        return ext.researchPrerequisites.All(x => x.IsFinished);
    }

}
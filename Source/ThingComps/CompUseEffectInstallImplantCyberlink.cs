using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_HE;

public class CompProperties_UseEffectInstallCyberlink : CompProperties_UseEffectInstallImplant
{
    public List<HediffDef> incompatibleHediffs = [];
    public List<HediffDef> replaceHediffs = [];

    public CompProperties_UseEffectInstallCyberlink()
        => compClass = typeof(CompUseEffect_InstallCyberlink);
}


public class CompUseEffect_InstallCyberlink : CompUseEffect_InstallImplant
{
    private new CompProperties_UseEffectInstallCyberlink Props
        => (CompProperties_UseEffectInstallCyberlink)props;

    public override void DoEffect(Pawn user)
    {
        base.DoEffect(user);

        RemoveHediffsOfDefs(user, Props.replaceHediffs);
    }

    private void RemoveHediffsOfDefs(Pawn p, List<HediffDef> defs)
    {
        List<Hediff> allHediffs = [];
        p.health.hediffSet.GetHediffs(ref allHediffs);
        foreach (Hediff hediff in allHediffs)
        {
            if (hediff.def == Props.hediffDef)
                continue;

            if (defs.Contains(hediff.def))
            {
                GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, p.Position, p.Map);
                p.health.RemoveHediff(hediff);
            }
        }
    }

    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        if (p.HasAnyHediffDef(Props.incompatibleHediffs, out Hediff incompatibleHediff))
            return "USH_HE_IncompatibleHediff".Translate(incompatibleHediff.Named("HEDIFF"));

        return base.CanBeUsedBy(p);
    }



    public override TaggedString ConfirmMessage(Pawn p)
    {
        if (p.WorkTagIsDisabled(WorkTags.Intellectual))
            return "USH_HE_ConfirmInstallCyberlinkIntellectual".Translate();

        return null;
    }
}
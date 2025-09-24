using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_HE;

public class CompProperties_MechanoidHackable : CompProperties_Hackable
{
    public CompProperties_MechanoidHackable()
    {
        compClass = typeof(CompMechanoidHackable);
    }
}

public class CompMechanoidHackable : CompHackable
{
    private new CompProperties_MechanoidHackable Props
        => (CompProperties_MechanoidHackable)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent is Corpse)
            yield break;

        if (parent.Faction == Faction.OfPlayer)
            yield break;

        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
    }

    public override string CompInspectStringExtra()
    {
        if (parent is Corpse)
            return "";

        if (parent.Faction == Faction.OfPlayer)
            return "";

        if (IsHacked)
            return "Disabled".Translate().Colorize(ColorLibrary.RedReadable);

        return base.CompInspectStringExtra();
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn.Faction == parent.Faction)
            yield break;

        foreach (var gizmo in base.CompFloatMenuOptions(selPawn))
            yield return gizmo;
    }

    protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
    {
        base.OnHacked(hacker, suppressMessages);

        if (parent is Corpse)
            return;

        CyberUtils.MakeHackingOutcomeEffect(parent, "Disabled".Translate());
        (parent as Pawn).health.AddHediff(USH_DefOf.USH_Disabled);
    }
}
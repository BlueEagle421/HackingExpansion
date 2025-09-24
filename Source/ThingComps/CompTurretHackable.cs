using System.Collections.Generic;
using RimWorld;
using Verse;

public class CompProperties_TurretHackable : CompProperties_Hackable
{
    public CompProperties_TurretHackable()
    {
        compClass = typeof(CompTurretHackable);
    }
}

public class CompTurretHackable : CompHackable
{
    private new CompProperties_TurretHackable Props
        => (CompProperties_TurretHackable)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction == Faction.OfPlayer)
            yield break;

        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
    }

    public override string CompInspectStringExtra()
    {
        if (parent.Faction == Faction.OfPlayer)
            return "";

        return base.CompInspectStringExtra();
    }
}
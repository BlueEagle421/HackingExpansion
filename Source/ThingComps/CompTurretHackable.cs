using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_HE;

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

        if (IsHacked)
            return "Disabled".Translate().Colorize(ColorLibrary.RedReadable);

        return base.CompInspectStringExtra();
    }

    protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
    {
        base.OnHacked(hacker, suppressMessages);

        if (hacker == null)
            return;

        parent.SetFactionDirect(hacker.Faction);
        this.ResetHackProgress();

        if (hacker.jobs.curJob.def == JobDefOf.Hack)
            hacker.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);

        if (parent.TryGetComp(out CompCanBeDormant canDormant))
            canDormant.WakeUp();

        CyberUtils.MakeHackingOutcomeEffect(parent, "USH_HE_Reconfigured".Translate());
    }

    public override bool CompPreventClaimingBy(Faction faction)
    {
        if (IsHacked)
            return true;

        return base.CompPreventClaimingBy(faction);
    }
}
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;


public class Ability_VirtualICEBreaker : Ability_Cyber
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (GlobalTargetInfo target in targets)
            if (target.Thing.TryGetComp(out CompHackable compHackable))
                InstallBreaker(compHackable);
    }

    private void InstallBreaker(CompHackable compHackable)
    {
        if (!compHackable.parent.TryGetComp(out CompDataSourceProtected compProtected))
            return;

        compProtected.AddICEBreaker(null);
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (!target.Thing.TryGetComp(out CompDataSourceProtected compProtected))
            return false;

        var report = compProtected.CanAcceptICEBreaker(null);
        if (!report)
        {
            if (showMessages)
            {
                Messages.Message(report.Reason,
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }
}
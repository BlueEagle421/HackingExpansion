using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class Ability_ForkBomb : Ability_Cyber
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (GlobalTargetInfo target in targets)
            if (target.Thing.TryGetComp(out CompHackable compHackable))
                AddHediff(compHackable);
    }

    private void AddHediff(CompHackable compHackable)
    {
        if (compHackable.parent is not Pawn p)
            return;

        p.health?.AddHediff(USH_DefOf.USH_ForkBomb);
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (!target.Thing.TryGetComp(out CompHackable compHackable))
            return false;

        if (compHackable.parent is not Pawn p)
            return false;

        if (p.health?.hediffSet?.HasHediff(USH_DefOf.USH_ForkBomb) == true)
        {
            if (showMessages)
            {
                Messages.Message("USH_HE_AlreadyForkBombed".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }
}
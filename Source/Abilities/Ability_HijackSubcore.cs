using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class Ability_HijackSubcore : Ability_Cyber
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (var target in targets)
            if (target.HasThing)
                Hijack(target.Pawn);
    }

    private void Hijack(Pawn mech)
    {
        mech.SetFaction(Faction.OfPlayer);

        HediffSet hediffSet = mech.health.hediffSet;

        if (hediffSet == null)
            return;

        if (hediffSet.TryGetHediff(USH_DefOf.USH_Disabled, out var toRemove))
            mech.health.RemoveHediff(toRemove);
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (!target.Thing.TryGetComp(out CompHackable _))
            return false;

        if (target.Thing is not Pawn p)
            return false;

        if (p.health?.hediffSet?.HasHediff(USH_DefOf.USH_Disabled) == false)
        {
            if (showMessages)
                Messages.Message("USH_HE_MustBeHacked".Translate(),
                    MessageTypeDefOf.RejectInput, false);

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }

    public override TargetingParameters targetParams => new()
    {
        canTargetPawns = true,
        canTargetSelf = false,
        canTargetBuildings = false,

        validator = HijackMechTargetValidator
    };

    private static bool HijackMechTargetValidator(TargetInfo targetInfo)
    {
        if (targetInfo.Thing is null)
            return false;

        if (!targetInfo.Thing.TryGetComp(out CompHackable _))
            return false;

        if (targetInfo.Thing is not Pawn p)
            return false;

        if (!p.RaceProps.IsMechanoid && !p.RaceProps.IsDrone)
            return false;

        return true;
    }
}
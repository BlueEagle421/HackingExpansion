using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class Ability_Zapcode : Ability_Cyber
{
    private const float HACK_PROGRESS_PERCENT = 0.25f;
    private const float MAX_PROGRESS_TO_CAST = 0.25f;

    private List<CompHackable> _affectedComps = [];

    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (GlobalTargetInfo target in targets)
            if (target.Thing.TryGetComp(out CompHackable compHackable))
                AddHackProgress(compHackable);

    }

    private void AddHackProgress(CompHackable compHackable)
    {
        compHackable.Hack(compHackable.defence * HACK_PROGRESS_PERCENT, pawn);

        _affectedComps.Add(compHackable);
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (!target.Thing.TryGetComp(out CompHackable compHackable))
            return false;

        if (_affectedComps.Contains(compHackable))
        {
            if (showMessages)
            {
                Messages.Message("USH_HE_AlreadyZapcoded".Translate(),
                    MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        if (compHackable.ProgressPercent > MAX_PROGRESS_TO_CAST)
        {
            if (showMessages)
            {
                Messages.Message("USH_HE_HackedTooMuch".Translate(
                    MAX_PROGRESS_TO_CAST.ToStringPercent()),
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }
}
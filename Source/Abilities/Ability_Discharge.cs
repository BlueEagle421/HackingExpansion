using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class Ability_Discharge : Ability_Cyber
{
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
        GenExplosion.DoExplosion(
            compHackable.parent.Position,
            compHackable.parent.Map,
            def.radius,
            DamageDefOf.EMP,
            null);

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
                Messages.Message("USH_HE_AlreadyDischarged".Translate(),
                    MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }
}
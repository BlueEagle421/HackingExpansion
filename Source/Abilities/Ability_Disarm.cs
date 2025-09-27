using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class Ability_Disarm : Ability_Cyber
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (var target in targets)
            if (target.HasThing)
                Disarm(target.Pawn);
    }

    private void Disarm(Pawn p)
    {
        if (p == null)
            return;

        Map map = p.Map;

        if (map == null)
            return;

        var eqTracker = p.equipment;
        if (eqTracker != null)
        {
            var primary = eqTracker.Primary;

            if (primary != null)
                eqTracker.TryDropEquipment(primary, out var _, p.Position, true);
        }
    }

    private bool TryGetPrimary(Pawn p, out ThingWithComps primary)
    {
        primary = null;

        if (p == null)
            return false;

        if (p.equipment is not Pawn_EquipmentTracker eqTracker)
            return false;

        primary = eqTracker.Primary;

        return primary != null;
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (target.Thing is not Pawn p)
            return false;

        if (!TryGetPrimary(p, out var primary))
            return false;

        if (primary.def.techLevel < TechLevel.Industrial)
        {
            if (showMessages)
                Messages.Message(
                    "USH_HE_NotIndustrial".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);

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

        if (targetInfo.Thing is not Pawn p)
            return false;

        return true;
    }
}
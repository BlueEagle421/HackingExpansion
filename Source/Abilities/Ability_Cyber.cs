using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public abstract class Ability_Cyber : VEF.Abilities.Ability
{
    public override TargetingParameters targetParams => new()
    {
        canTargetPawns = true,
        canTargetSelf = false,
        canTargetBuildings = true,

        validator = HackTargetValidator
    };

    private static bool HackTargetValidator(TargetInfo targetInfo)
    {
        if (targetInfo.Thing is null)
            return false;

        if (!targetInfo.Thing.TryGetComp(out CompHackable _))
            return false;

        return true;
    }

    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (GlobalTargetInfo target in targets)
            if (target.HasThing)
                CyberUtils.MakeHackingOutcomeEffect(target.Thing, def.LabelCap);
    }

    public override float GetRangeForPawn()
        => pawn.GetRemoteHackRadius();
}
using VEF.Abilities;
using Verse;

namespace USH_HE;

public abstract class Ability_Cyber : Ability
{
    public override RimWorld.TargetingParameters targetParams => new()
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

        if (!targetInfo.Thing.TryGetComp(out CompCyberTarget _))
            return false;

        return true;
    }
}
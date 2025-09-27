using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace USH_HE;

public class AbilityExtension_Cyber : DefModExtension
{
    public bool doHackEffect = true;
    public float marketValue;
    public float learningPoints;
}


public abstract class Ability_Cyber : VEF.Abilities.Ability
{
    private AbilityExtension_Cyber _ext;
    private AbilityExtension_Cyber Ext
    {
        get
        {
            _ext ??= def.GetModExtension<AbilityExtension_Cyber>();
            return _ext;
        }
    }

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

        if (Ext.doHackEffect)
            foreach (GlobalTargetInfo target in targets)
                if (target.HasThing)
                    CyberUtils.MakeHackingOutcomeEffect(target.Thing, def.LabelCap);
    }

    public override float GetRangeForPawn()
        => pawn.GetRemoteHackRadius();

    public override void WarmupToil(Toil toil)
    {
        base.WarmupToil(toil);
        toil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A);
    }
}
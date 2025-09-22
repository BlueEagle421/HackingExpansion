using Verse;

namespace USH_HE;

public class Verb_Disrupt : Verb
{
    private Effecter _effecter;
    public DisruptorExtension Ext
        => EquipmentSource.def.GetModExtension<DisruptorExtension>() ?? new();

    public override bool CanHitTarget(LocalTargetInfo targ)
    {
        if (targ.HasThing && targ.Thing.Map != caster.Map)
            return false;

        if (targ.Thing is not Pawn p)
            return false;

        if (!p.RaceProps.IsMechanoid)
            return false;

        return base.CanHitTarget(targ);
    }

    protected override bool TryCastShot()
    {
        _effecter?.ForceEnd();
        _effecter = Ext.effecterDef.SpawnMaintained(currentTarget.Thing, caster.Map);

        currentTarget.Pawn.health.AddHediff(Ext.hediffDef);

        return true;
    }
}

public class DisruptorExtension : DefModExtension
{
    public EffecterDef effecterDef;
    public HediffDef hediffDef;
    public int effectDurationTicks;
}

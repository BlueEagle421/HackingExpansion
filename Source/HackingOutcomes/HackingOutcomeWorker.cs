using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

public abstract class HackingOutcomeWorker(HackingOutcomeDef def)
{
    protected HackingOutcomeDef _def = def;
    public virtual void ApplyOutcome(Pawn hacker, Thing caster)
    {
        if (!CanApplyOnPawn(hacker))
            return;

        DoMessage(hacker, caster);
        DoVisuals(hacker);
    }

    protected void DoMessage(Pawn hacker, Thing caster)
    {
        if (!_def.doMessage)
            return;

        var hacksetArg = caster.Label.Named("HACKSET");
        var outcomeArg = _def.Named("OUTCOME");
        var pawnArg = hacker.Named("PAWN");

        var msg = "USH_HE_OutcomeMessage".Translate(hacksetArg, outcomeArg, pawnArg);

        Messages.Message(msg, new(hacker), MessageTypeDefOf.NegativeEvent);
    }

    protected void DoVisuals(Pawn hacker)
    {
        Thing source = hacker;

        var pod = hacker.GetHoldingCyberpod();
        if (pod != null)
            source = pod;

        _def.effecterDef.SpawnMaintained(source.Position, source.Map);

        float fadeTicks = 4f;
        MoteMaker.ThrowText(source.DrawPos, source.Map, _def.label.Colorize(Color.red), fadeTicks);
    }

    public virtual AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!_def.worksWithPod && hacker.TryGetHoldingCyberpod(out _))
            return false;

        return true;
    }

}

public abstract class HackingOutcomeWorker_GiveHediff(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    protected abstract HediffDef HediffDef { get; }
    protected abstract bool OnlyOnFlesh { get; }

    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (OnlyOnFlesh == true && !hacker.RaceProps.IsFlesh)
            return "USH_HE_FleshOnly";

        return true;
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        hacker.health?.AddHediff(HediffDef);
    }
}

public class HackingOutcomeWorker_GiveNausea(HackingOutcomeDef _def)
    : HackingOutcomeWorker_GiveHediff(_def)
{
    protected override bool OnlyOnFlesh => true;
    protected override HediffDef HediffDef
        => USH_DefOf.USH_CyberspaceNausea;
}

public class HackingOutcomeWorker_GiveLockOut(HackingOutcomeDef _def)
    : HackingOutcomeWorker_GiveHediff(_def)
{
    protected override bool OnlyOnFlesh => true;
    protected override HediffDef HediffDef
        => USH_DefOf.USH_CyberspaceLockOut;
}

public class HackingOutcomeWorker_GiveComa(HackingOutcomeDef _def)
    : HackingOutcomeWorker_GiveHediff(_def)
{
    protected override bool OnlyOnFlesh => true;
    protected override HediffDef HediffDef
        => USH_DefOf.USH_CyberspaceComa;
}

public class HackingOutcomeWorker_EMPPulse(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    private const float EMP_AMOUNT = 20f;

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        hacker.TakeDamage(new(DamageDefOf.EMP, EMP_AMOUNT));
    }
}

public class HackingOutcomeWorker_Berserk(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        var mental = hacker.mindState?.mentalStateHandler;
        mental?.TryStartMentalState(MentalStateDefOf.Berserk, "USH_HE_HackingBerserkReason".Translate(caster.Label));
    }
}

public class HackingOutcomeWorker_BrainFry(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    private const float BURN_AMOUNT = 1f;

    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (hacker.health.hediffSet.GetBrain() == null)
            return "USH_HE_NoBrain";

        return true;
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        var brainPart = hacker.health.hediffSet.GetBrain();

        var dinfo = new DamageInfo(DamageDefOf.Burn, BURN_AMOUNT,
                                   armorPenetration: 0, angle: 0,
                                   instigator: hacker, hitPart: brainPart);

        hacker.TakeDamage(dinfo);
    }
}

public class HackingOutcomeWorker_EMPExplosion(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!hacker.TryGetHoldingCyberpod(out _))
            return false;

        return base.CanApplyOnPawn(hacker);
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        hacker.TryGetHoldingCyberpod(out var pod);

        GenExplosion.DoExplosion(pod.Position, pod.Map, 5.9f, DamageDefOf.EMP, null);
    }
}

public class HackingOutcomeWorker_HullBreach(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    private FloatRange _damageRange = new(30f, 70f);

    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!hacker.TryGetHoldingCyberpod(out _))
            return false;

        return base.CanApplyOnPawn(hacker);
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return;

        pod.TakeDamage(new(DamageDefOf.Mining, _damageRange.RandomInRange));
    }
}

public class HackingOutcomeWorker_CyberspaceNecrosis(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        HealthUtility.DamageUntilDowned(hacker, false, DamageDefOf.Burn);
    }
}

public class HackingOutcomeWorker_LethalCyberspaceNecrosis(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        var pod = hacker.GetHoldingCyberpod();

        HealthUtility.DamageUntilDead(hacker, DamageDefOf.Burn);

        pod?.EjectContents();
    }
}

public class HackingOutcomeWorker_PowerDrainBug(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return false;

        if (pod.PowerComp?.PowerNet is not PowerNet powerNet)
            return false;

        if (powerNet.batteryComps?.Any() != true)
            return false;

        return base.CanApplyOnPawn(hacker);
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return;

        DrawPowerFromNet(pod.PowerComp?.PowerNet);
    }

    private void DrawPowerFromNet(PowerNet powerNet)
    {
        if (powerNet == null)
            return;

        foreach (CompPowerBattery battery in powerNet.batteryComps)
            battery.DrawPower(battery.StoredEnergy);
    }
}

public class HackingOutcomeWorker_EMIVirus(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    private IntRange _durationTicksRange = new(2500, 2500 * 2); //1, 2 hours 

    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return false;

        return base.CanApplyOnPawn(hacker);
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        caster.Map.GameConditionManager
            .RegisterCondition(GameConditionMaker
            .MakeCondition(GameConditionDefOf.EMIField, _durationTicksRange.RandomInRange));
    }
}

public class HackingOutcomeWorker_ShortCircuitExec(HackingOutcomeDef _def)
    : HackingOutcomeWorker(_def)
{
    public override AcceptanceReport CanApplyOnPawn(Pawn hacker)
    {
        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return false;

        return base.CanApplyOnPawn(hacker);
    }

    public override void ApplyOutcome(Pawn hacker, Thing caster)
    {
        base.ApplyOutcome(hacker, caster);

        if (!hacker.TryGetHoldingCyberpod(out var pod))
            return;

        ShortCircuitUtility.DoShortCircuit(pod);
    }
}
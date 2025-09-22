using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_HE;

public class JobDriver_UseEmptyActionUnit : JobDriver
{
    private Thing Target => job.GetTarget(TargetIndex.A).Thing;
    private Thing Item => job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(Target, job))
            return pawn.Reserve(Item, job);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);

        yield return Toils_Haul.StartCarryThing(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);

        Toil toil = Toils_General.Wait(240);

        toil.WithEffect(USH_DefOf.USH_RippingData, TargetIndex.A);
        toil.WithProgressBarToilDelay(TargetIndex.A);
        toil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
        toil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);
        toil.FailOnDespawnedOrNull(TargetIndex.A);
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

        yield return toil;

        yield return Toils_General.Do(RipRandomHackAction);
    }

    private void RipRandomHackAction()
    {
        if (!Target.TryGetComp(out CompDataSource compDataSource))
            return;

        // if (compDataSource.HacksetDef == null)
        //     return;

        // var action = compDataSource.HacksetDef.hackingOutcomes
        //     .RandomElementByWeight(d => d.weight);

        // float fadeTicks = 4f;
        // MoteMaker.ThrowText(compDataSource.parent.DrawPos, compDataSource.parent.Map, action.LabelCap, fadeTicks);

        Item.SplitOff(1).Destroy(DestroyMode.Vanish);
    }
}

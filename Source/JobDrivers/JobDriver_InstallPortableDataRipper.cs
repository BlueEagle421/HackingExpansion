using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_HE;

public class JobDriver_InstallVirtualDataRipper : JobDriver
{
    private Thing Target => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
        => pawn.Reserve(Target, job);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);

        Toil toil = Toils_General.Wait(240);

        toil.WithEffect(USH_DefOf.USH_RippingData, TargetIndex.A);
        toil.WithProgressBarToilDelay(TargetIndex.A);
        toil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
        toil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);
        toil.FailOnDespawnedOrNull(TargetIndex.A);
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

        yield return toil;

        yield return Toils_General.Do(InstallRipper);
    }

    private void InstallRipper()
    {
        if (!Target.TryGetComp(out CompDataSource compDataSource))
            return;

        SoundDefOf.Hacking_Completed.PlayOneShot(Target);
        var props = USH_DefOf.USH_DataRipper
            .GetCompProperties<CompProperties_DataRipper>();

        compDataSource.AddDataRipper(props.rippableThings);
    }
}

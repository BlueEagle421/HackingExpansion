
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_HE;


public class JobDriver_ApplyResearchGiver : JobDriver
{
    private const TargetIndex BENCH_INDEX = TargetIndex.A;
    private const TargetIndex THING_INDEX = TargetIndex.B;
    private const TargetIndex HAUL_CELL_INDEX = TargetIndex.C;
    private const int DURATION_PER_GIVER = 30;

    protected Building_ResearchBench ResearchBench => (Building_ResearchBench)job.GetTarget(BENCH_INDEX).Thing;
    protected Thing ResearchGiver => job.GetTarget(THING_INDEX).Thing;
    protected CompResearchGiver ResearchGiverComp => ResearchGiver.TryGetComp<CompResearchGiver>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(ResearchBench, job, 1, -1, null, errorOnFailed))
            return pawn.Reserve(ResearchGiver, job, 1, -1, null, errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(BENCH_INDEX);
        this.FailOnBurningImmobile(BENCH_INDEX);
        yield return Toils_General.DoAtomic(delegate
        {
            job.count = ResearchGiver.stackCount;
        });
        yield return Toils_Reserve.Reserve(THING_INDEX);

        yield return Toils_Goto.GotoThing(THING_INDEX, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(THING_INDEX)
            .FailOnSomeonePhysicallyInteracting(THING_INDEX);

        yield return Toils_Haul.StartCarryThing(THING_INDEX, false, true)
            .FailOnDestroyedNullOrForbidden(THING_INDEX);

        yield return Toils_Goto.GotoThing(BENCH_INDEX, PathEndMode.InteractionCell);
        yield return Toils_Haul.PlaceHauledThingInCell(HAUL_CELL_INDEX, null, storageMode: false);

        for (int i = 0; i < ResearchGiver.stackCount; i++)
        {
            yield return Toils_General.Wait(DURATION_PER_GIVER)
                .FailOnDestroyedNullOrForbidden(THING_INDEX)
                .FailOnDestroyedNullOrForbidden(BENCH_INDEX)
                .FailOnCannotTouch(BENCH_INDEX, PathEndMode.InteractionCell)
                .FailOn(() => Find.ResearchManager.GetProject() == null)
                .WithProgressBarToilDelay(BENCH_INDEX);

            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                Find.ResearchManager.AddProgress(Find.ResearchManager.GetProject(), ResearchGiverComp.Props.points);

                ResearchGiver.SplitOff(1).Destroy(DestroyMode.Vanish);
                SoundDefOf.TechprintApplied.PlayOneShotOnCamera();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }
    }
}

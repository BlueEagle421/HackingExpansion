using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_HE;

public class JobDriver_RemoteHack : JobDriver
{
    private Thing HackTarget => TargetThingA;
    private CompHackable CompHackable => HackTarget.TryGetComp<CompHackable>();

    private float _cachedRadiusSquared = -1;
    private float RadiusSquared
    {
        get
        {
            float radius = pawn.GetStatValue(USH_DefOf.USH_RemoteHackingDistance);

            if (_cachedRadiusSquared == -1)
                _cachedRadiusSquared = radius * radius;

            return _cachedRadiusSquared;
        }
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.CanHackRemotely())
            return true;

        return pawn.Reserve(HackTarget, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool remoteHacking = pawn.CanHackRemotely();

        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() =>
            CompHackable.Props.intellectualSkillPrerequisite > 0 &&
            pawn.skills.GetSkill(SkillDefOf.Intellectual).Level < CompHackable.Props.intellectualSkillPrerequisite);

        PathEndMode pathEndMode = TargetThingA.def.hasInteractionCell
            ? PathEndMode.InteractionCell
            : PathEndMode.ClosestTouch;

        var gotoToil = Toils_Goto.GotoThing(TargetIndex.A, pathEndMode);

        if (remoteHacking)
            gotoToil.tickAction = () =>
            {
                if (pawn.Position.DistanceToSquared(HackTarget.Position) <= RadiusSquared)
                {
                    pawn.pather.StopDead();
                    ReadyForNextToil();
                }
            };


        yield return gotoToil;

        var hackToil = ToilMaker.MakeToil("HackTarget");
        hackToil.handlingFacing = true;

        hackToil.tickAction = () =>
        {
            CompHackable.Hack(pawn.GetStatValue(StatDefOf.HackingSpeed), pawn);
            pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
            pawn.rotationTracker.FaceTarget(HackTarget);
        };

        hackToil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A);

        if (CompHackable.Props.effectHacking != null)
        {
            hackToil.WithEffect(
                () => CompHackable.Props.effectHacking,
                () => HackTarget.OccupiedRect().ClosestCellTo(pawn.Position));
        }

        hackToil.WithProgressBar(TargetIndex.A, () => CompHackable.ProgressPercent, false, -0.5f, true);
        hackToil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
        hackToil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);

        hackToil.AddFinishAction(() =>
        {
            if (CompHackable.IsHacked)
            {
                SoundDefOf.Hacking_Completed.PlayOneShot(HackTarget);
                CompHackable.Props.hackingCompletedSound?.PlayOneShot(HackTarget);
            }
            else
            {
                SoundDefOf.Hacking_Suspended.PlayOneShot(HackTarget);
            }
        });

        if (!remoteHacking)
            hackToil.FailOnCannotTouch(TargetIndex.A, pathEndMode);

        hackToil.FailOn(() => CompHackable.IsHacked || CompHackable.LockedOut);

        hackToil.defaultCompleteMode = ToilCompleteMode.Never;
        hackToil.activeSkill = () => SkillDefOf.Intellectual;

        yield return hackToil;
    }
}

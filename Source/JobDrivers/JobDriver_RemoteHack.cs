using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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

        Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        if (remoteHacking)
        {
            if (!IsTargetInRange())
            {
                gotoToil = Toils_Goto.GotoCell(FindNearestCellToTarget(), PathEndMode.OnCell);

                gotoToil.tickAction = () =>
                {
                    if (IsTargetInRange())
                    {
                        pawn.pather.StopDead();
                        ReadyForNextToil();
                    }
                };
            }
            else
            {
                gotoToil = ToilMaker.MakeToil("RemoteHack_Goto");
                gotoToil.initAction = () =>
                {
                    pawn.pather.StopDead();
                    ReadyForNextToil();
                };

                gotoToil.defaultCompleteMode = ToilCompleteMode.Instant;
            }
        }

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

        if (remoteHacking)
            hackToil.FailOn(() => !IsTargetInRange());

        hackToil.defaultCompleteMode = ToilCompleteMode.Never;
        hackToil.activeSkill = () => SkillDefOf.Intellectual;

        yield return hackToil;
    }

    private IntVec3 FindNearestCellToTarget()
    {
        IntVec3 bestCell = IntVec3.Invalid;
        float bestCost = float.MaxValue;

        int radius = Mathf.CeilToInt(pawn.GetStatValue(USH_DefOf.USH_RemoteHackingDistance));
        var map = pawn.Map;
        var traverseParms = TraverseParms.For(TraverseMode.PassDoors);

        foreach (IntVec3 cell in GenRadial.RadialCellsAround(HackTarget.Position, radius - 1, true))
        {
            if (!cell.InBounds(map) || !cell.Standable(map))
                continue;

            if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
                continue;

            var path = pawn.Map.pathFinder.FindPathNow(pawn.Position, cell, traverseParms);
            if (!path.Found)
                continue;

            float cost = path.TotalCost;
            path.ReleaseToPool();

            if (cost < bestCost)
            {
                bestCost = cost;
                bestCell = cell;
            }
        }

        if (!bestCell.IsValid)
            bestCell = CellFinder.RandomClosewalkCellNear(HackTarget.Position, map, 1);

        return bestCell;
    }

    private bool IsTargetInRange()
    {
        return pawn.Position.DistanceToSquared(HackTarget.Position) <= RadiusSquared;
    }
}

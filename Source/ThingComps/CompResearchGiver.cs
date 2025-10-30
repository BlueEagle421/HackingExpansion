using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_HE;


public class CompProperties_ResearchGiver : CompProperties
{
    public int points;

    public CompProperties_ResearchGiver()
        => compClass = typeof(CompResearchGiver);

}


public class CompResearchGiver : ThingComp
{
    public CompProperties_ResearchGiver Props => (CompProperties_ResearchGiver)props;

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        JobFailReason.Clear();

        if (IsResearchWorkDisabled(selPawn))
            JobFailReason.Is("WillNever".Translate("Research".TranslateSimple().UncapitalizeFirst()));
        else if (!selPawn.CanReach(parent, PathEndMode.ClosestTouch, Danger.Some))
            JobFailReason.Is("CannotReach".Translate());
        else if (Find.ResearchManager.GetProject() == null)
            JobFailReason.Is("USH_NoResearchProject".Translate());

        HaulAIUtility.PawnCanAutomaticallyHaul(selPawn, parent, forced: true);

        Thing researchBench = FindAvailableResearchBench(selPawn);

        Job applyJob = (researchBench != null) ? CreateApplyJob(researchBench) : null;

        if (JobFailReason.HaveReason)
        {
            yield return CreateFailureMenuOption(JobFailReason.Reason);
            JobFailReason.Clear();
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "USH_UtilizeCyberdataPoints".Translate(parent.Label, Props.points * parent.stackCount).CapitalizeFirst(),
                delegate
                {
                    if (applyJob == null)
                    {
                        Messages.Message("USH_MessageNoResearchBench".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        selPawn.jobs.TryTakeOrderedJob(applyJob, JobTag.Misc);
                    }
                }),
            selPawn,
            parent);
    }

    private bool IsResearchWorkDisabled(Pawn pawn)
        => pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research)
               || pawn.WorkTagIsDisabled(WorkTags.Intellectual);

    private Thing FindAvailableResearchBench(Pawn pawn)
        => GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForGroup(ThingRequestGroup.ResearchBench),
            PathEndMode.InteractionCell,
            TraverseParms.For(pawn, Danger.Some),
            maxDistance: 9999f,
            t =>
            {

                return (t is Building_ResearchBench) && !t.IsForbidden(pawn) && pawn.CanReserve(t);
            });


    private Job CreateApplyJob(Thing researchBench)
    {
        Job job = JobMaker.MakeJob(USH_DefOf.USH_ApplyResearchGiver);
        job.targetA = researchBench;
        job.targetB = parent;
        job.targetC = researchBench.Position;
        return job;
    }

    private FloatMenuOption CreateFailureMenuOption(string reason)
    {
        string label = "CannotGenericWorkCustom"
                        .Translate("USH_UtilizeCyberdata".Translate(parent.Label).ToLower())
                        + ": "
                        + reason.CapitalizeFirst();

        return new FloatMenuOption(label, null);
    }
}

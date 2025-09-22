using System;
using System.Collections.Generic;
using RimWorld;
using USH_HE;
using Verse;
using Verse.AI;

namespace USH_HE;

public class FloatMenuOptionProvider_InstallDataRipper : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!clickedThing.TryGetComp(out CompDataRipper compDataRipper))
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("USH_HE_InstallDataRipper".Translate(),
            () =>
            {
                BeginTargeting(context.FirstSelectedPawn, compDataRipper);
            }), context.FirstSelectedPawn, new LocalTargetInfo(clickedThing));
    }

    private static void BeginTargeting(Pawn p, CompDataRipper compDataRipper)
    {
        Find.Targeter.BeginTargeting(CyberLibrary.RipperTargetingParams, delegate (LocalTargetInfo target)
        {
            GiveJobToPawn(p, target.Thing.TryGetComp<CompDataSource>(), compDataRipper);
        }, null, null, null, null, null, playSoundOnAction: true,
        (target) =>
        {
            if (target.Thing.TryGetComp(out CompDataSource compDataSource))
            {
                var report = compDataSource.CanAcceptDataRipper(compDataRipper);
                if (!report.Accepted)
                {
                    var msg = $"{"USH_HE_CannotInstall".Translate()}: {report.Reason.CapitalizeFirst()}"
                              .Colorize(ColorLibrary.RedReadable);

                    Widgets.MouseAttachedLabel(msg);
                    return;
                }
            }

            Widgets.MouseAttachedLabel("USH_HE_CommandChooseDataSource".Translate());
        });
    }

    private static void GiveJobToPawn(Pawn p, CompDataSource compDataSource, CompDataRipper compDataRipper)
    {
        Job job = JobMaker.MakeJob(USH_DefOf.USH_InstallDataRipper, compDataSource.parent, compDataRipper.parent);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job);
    }

}
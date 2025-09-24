using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_HE;

public class FloatMenuOptionProvider_InstallICEBreaker : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!clickedThing.TryGetComp(out CompICEBreaker compICEBreaker))
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("USH_HE_InstallICEBreaker".Translate(),
            () =>
            {
                BeginTargeting(context.FirstSelectedPawn, compICEBreaker);
            }), context.FirstSelectedPawn, new LocalTargetInfo(clickedThing));
    }

    private static void BeginTargeting(Pawn p, CompICEBreaker compICEBreaker)
    {
        Find.Targeter.BeginTargeting(CyberLibrary.RipperTargetingParams, delegate (LocalTargetInfo target)
        {
            GiveJobToPawn(p, target.Thing, compICEBreaker.parent);
        }, null, null, null, null, null, playSoundOnAction: true,
        (target) =>
        {
            if (target.Thing.TryGetComp(out CompDataSourceProtected compDataSource))
            {
                var report = compDataSource.CanAcceptICEBreaker(compICEBreaker);
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

    private static void GiveJobToPawn(Pawn p, Thing building, Thing item)
    {
        Job job = JobMaker.MakeJob(USH_DefOf.USH_InstallICEBreaker, building, item);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job);
    }

}
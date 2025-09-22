using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_HE;

[StaticConstructorOnStartup]
public class Hediff_Cyberlink : HediffWithComps
{
    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield return HackGizmo();

        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
    }

    private Gizmo HackGizmo()
    {
        float radius = pawn.GetStatValue(USH_DefOf.USH_RemoteHackingDistance);

        Command_Action command_Action = new()
        {
            defaultLabel = "USH_HE_OrderHackTarget".Translate() + "...",
            defaultDesc = "USH_HE_OrderHackTargetDesc".Translate(),
            icon = CyberLibrary.TargetHackTex,
            groupable = false,
            shrinkable = true,
            onHover = () => DrawHackRadius(radius),
            action = () =>
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                DoTargeting(radius);
            },
        };

        return command_Action;
    }

    private void DoTargeting(float radius)
    {
        Find.Targeter.BeginTargeting(
            CyberLibrary.HackTargetingParams,
            (target) => pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Hack, target.Thing)),
            GenDraw.DrawTargetHighlight,
            (target) => pawn.CanHack(target).Accepted,
            pawn,
            null,
            CyberLibrary.TargetHackTex,
            playSoundOnAction: true,
            (target) =>
            {
                string mouseCommand = "USH_HE_CommandChooseToHack".Translate();

                if (target.Thing is null || !target.Thing.TryGetComp(out CompHackable compHackable))
                {
                    Widgets.MouseAttachedLabel(mouseCommand);
                    return;
                }

                AcceptanceReport hackReport = pawn.CanHack(target.Thing);
                if (!hackReport.Accepted)
                {
                    var reportMsg = hackReport.Reason.CapitalizeFirst().Colorize(ColorLibrary.RedReadable);
                    mouseCommand = "CannotChooseHacker".Translate() + ": " + reportMsg;
                    Widgets.MouseAttachedLabel(mouseCommand);
                    return;
                }

                Widgets.MouseAttachedLabel(mouseCommand, 0, 0, CyberUtils.HackColor);
            }, (target) => DrawHackRadius(radius));
    }

    private void DrawHackRadius(float radius)
    {
        if (Mathf.Approximately(radius, 0f))
            return;

        GenDraw.DrawRadiusRing(pawn.Position, radius, CyberUtils.HackColorTransparent);
    }
}

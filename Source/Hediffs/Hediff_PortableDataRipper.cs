using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_HE;

[StaticConstructorOnStartup]
public class Hediff_VirtualDataRipper : HediffWithComps
{
    private Texture2D _cachedRipperTex;
    public Texture2D RipperTex
    {
        get
        {
            _cachedRipperTex ??= ContentFinder<Texture2D>.Get(USH_DefOf.USH_DataRipper.graphic.path);
            return _cachedRipperTex;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield return RipperGizmo();

        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
    }

    private Gizmo RipperGizmo()
    {
        float radius = pawn.GetStatValue(USH_DefOf.USH_RemoteHackingDistance);

        Command_Action command_Action = new()
        {
            defaultLabel = "USH_HE_OrderHackTarget".Translate() + "...",
            defaultDesc = "USH_HE_OrderHackTargetDesc".Translate(),
            icon = RipperTex,
            groupable = false,
            shrinkable = true,
            onHover = null,
            action = () =>
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                BeginTargeting();
            },
        };

        return command_Action;
    }

    private void BeginTargeting()
    {
        Find.Targeter.BeginTargeting(CyberLibrary.RipperTargetingParams, delegate (LocalTargetInfo target)
                {
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(USH_DefOf.USH_InstallVirtualDataRipper, target.Thing));
                }, null, null, null, null, null, playSoundOnAction: true,
                (target) =>
                {
                    if (target.Thing.TryGetComp(out CompDataSource compDataSource))
                    {
                        var props = USH_DefOf.USH_DataRipper
                            .GetCompProperties<CompProperties_DataRipper>();

                        var report = compDataSource.CanAcceptDataRipper(props.rippableThings);
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
}

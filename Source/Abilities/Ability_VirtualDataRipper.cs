using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;


public class AbilityExtension_VirtualDataRipper : DefModExtension
{
    public List<ThingDef> rippableThings;
}


public class Ability_VirtualDataRipper : Ability_Cyber
{
    private AbilityExtension_VirtualDataRipper _ext;
    private AbilityExtension_VirtualDataRipper Ext
    {
        get
        {
            _ext ??= def.GetModExtension<AbilityExtension_VirtualDataRipper>();
            return _ext;
        }
    }

    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        foreach (GlobalTargetInfo target in targets)
            if (target.Thing.TryGetComp(out CompHackable compHackable))
                InstallRipper(compHackable);

    }

    private void InstallRipper(CompHackable compHackable)
    {
        if (!compHackable.parent.TryGetComp(out CompDataSource compDataSource))
            return;

        compDataSource.AddDataRipper(Ext.rippableThings);
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!target.HasThing)
            return false;

        if (!target.Thing.TryGetComp(out CompDataSource compDataSource))
            return false;


        var report = compDataSource.CanAcceptDataRipper(Ext.rippableThings);
        if (!report)
        {
            if (showMessages)
            {
                Messages.Message(report.Reason,
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return false;
        }

        return base.ValidateTarget(target, showMessages);
    }
}
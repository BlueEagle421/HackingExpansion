using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace USH_HE;

public class CompProperties_UsableExpanded : CompProperties_Usable
{
    public List<HediffDef> userMustHaveAnyHediff = [];

    public CompProperties_UsableExpanded()
        => compClass = typeof(CompUsableExpanded);
}


public class CompUsableExpanded : CompUsable
{
    private new CompProperties_UsableExpanded Props
        => (CompProperties_UsableExpanded)props;

    public override AcceptanceReport CanBeUsedBy(Pawn p, bool forced = false, bool ignoreReserveAndReachable = false)
    {
        var required = Props.userMustHaveAnyHediff;

        if (required.Count > 0 && !p.HasAnyHediffDef(required, out _))
        {
            var names = string.Join(", ", required.Select(d => d?.label));
            return "USH_HE_MustHaveAnyHediff".Translate(names.Named("HEDIFFS"));
        }

        return base.CanBeUsedBy(p, forced, ignoreReserveAndReachable);
    }
}
using System.Collections.Generic;
using Verse;

namespace USH_HE;

public class RippableExtension : DefModExtension
{
    public float hackWorkAmount;
    public IntRange maxPerDataSourceRange;
}

public class ResearchPrerequisitesExtension : DefModExtension
{
    public List<ResearchProjectDef> researchPrerequisites = [];

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);

        ResearchPrerequisitesResolver.AddDefToCheck(parentDef);
    }
}

public class CompProperties_DataRipper : CompProperties
{
    public List<ThingDef> rippableThings = [];
    public CompProperties_DataRipper()
        => compClass = typeof(CompDataRipper);
}

public class CompDataRipper : ThingComp, IDataRipper
{
    private CompProperties_DataRipper Props => (CompProperties_DataRipper)props;

    public IEnumerable<ThingDef> RippableThings
        => Props.rippableThings;
}

public interface IDataRipper
{
    public IEnumerable<ThingDef> RippableThings { get; }
}
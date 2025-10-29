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